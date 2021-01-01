using System;
using System.Diagnostics;
using System.IO;
using System_NES.Mappers;
using SystemBase;

namespace System_NES
{
    #region InfoFlags enumeration
    [Flags]
    internal enum InfoFlags
    {
        Mirroring = 1 << 0,
        Battery = 1 << 1,
        Trainer = 1 << 2,
        FourScreen = 1 << 3
    }
    #endregion

    #region Timing enumeration
    internal enum Timing
    {
        NTSC = 0,
        PAL = 1,
        Multi = 2,
        Dendy = 3
    }
    #endregion

    #region MirrorMode enumeration
    internal enum MirrorMode
    {
        OneScreenLo = 0,
        OneScreenHigh = 1,
        Vertical = 2,
        Horizontal = 3,
        Cartridge
    }
    #endregion

    internal sealed class Cartridge : IBusComponent_16
    {
        #region Member variables
        private readonly byte[] prgData;
        private readonly byte[] chrData;
        //private readonly byte[] trainerData;
        private readonly Mapper mapper;
        private readonly InfoFlags info;
        private readonly Timing timing;
        #endregion

        #region Constructor
        public Cartridge(string filePath, IBus bus)
        {
            using (BinaryReader reader = new BinaryReader(new FileStream(filePath, FileMode.Open)))
            {
                if (reader.BaseStream.Length < 16)
                    return;

                byte[] header = reader.ReadBytes(16);
                if (header[0] != 'N' || header[1] != 'E' || header[2] != 'S' || header[3] != 0x1A)
                    return;

                byte mapperId = (byte)((header[7] & 0xF0) | (header[6] >> 4));
                byte chrBankCount;
                ushort prgBankCount;
                if ((header[7] & 0x0C) != 0x08)
                {
                    // v1 format
                    prgBankCount = header[4];
                    chrBankCount = header[5];
                    info = (InfoFlags)(header[6] & 0x0F);
                    timing = (Timing)(header[9] & 0x01);
                }
                else
                {
                    // v2 format
                    prgBankCount = (ushort)(((header[9] & 0x0F) << 8) | header[4]);
                    chrBankCount = header[5];
                    info = (InfoFlags)(header[6] & 0x0F);
                    timing = (Timing)(header[12] & 0x03);
                }

                if (timing != Timing.NTSC)
                    Console.WriteLine("WARNING: Loaded non-NTSC ROM");

                //if (info.HasFlag(InfoFlags.Trainer))
                //{
                //    trainerData = new byte[512];
                //    reader.Read(trainerData, 0, trainerData.Length);
                //}

                int prgSize = prgBankCount * Utils.Kilo16;
                prgData = new byte[prgSize];
                int countRead = reader.Read(prgData, 0, prgSize);
                Debug.Assert(countRead == prgSize);

                int chrSize = chrBankCount * Utils.Kilo8;
                chrData = new byte[chrSize];
                countRead = reader.Read(chrData, 0, chrSize);
                Debug.Assert(countRead == chrSize);

                if (chrSize == 0)
                    chrData = new byte[Utils.Kilo8]; // Cartridge has RAM?

                MirrorMode cartMirrorMode = info.HasFlag(InfoFlags.Mirroring) ? MirrorMode.Vertical : MirrorMode.Horizontal;
                if (mapperId == 0)
                {
                    // Fix some simple incorrect mapping IDs
                    if (chrBankCount > 1)
                        mapperId = 3;
                    else if (prgBankCount > 2)
                        mapperId = 2;
                }
                
                switch (mapperId)
                {
                    case 0: mapper = new Mapper000(prgBankCount, chrBankCount, cartMirrorMode); break;
                    case 1: mapper = new Mapper001(prgBankCount, chrBankCount, cartMirrorMode, info); break;
                    case 2: mapper = new Mapper002(prgBankCount, chrBankCount, cartMirrorMode); break;
                    case 3: mapper = new Mapper003(prgBankCount, chrBankCount, cartMirrorMode); break;
                    case 4: mapper = new Mapper004(prgBankCount, chrBankCount, cartMirrorMode, bus); break;
                    default:
                        throw new NotImplementedException("Mapper " + mapperId + " is not implemented");
                }
            }
        }
        #endregion

        public MirrorMode Mirror => mapper.MirrorMode;

        #region IBusComponent_16 implementation
        public void Dispose()
        {
        }

        public void Reset()
        {
            mapper.Reset();
        }

        public void WriteDataFromBus(ushort address, byte data)
        {
            if (mapper.MapCPUAddressWrite(address, data, out uint newAddress) && newAddress != Mapper.MapperHandled)
                prgData[newAddress] = data;
        }

        public byte ReadDataForBus(ushort address)
        {
            if (mapper.MapCPUAddressRead(address, out uint newAddress, out byte data))
                return newAddress == Mapper.MapperHandled ? data : prgData[newAddress];

            //if (address >= 0x7000 && address <= 0x71FF)
            //    return trainerData?[address - 0x7000] ?? 0;
            //Debug.Assert(address < 0x7000 || address > 0x71FF);

            return 0;
        }
        #endregion

        public bool WritePPUData(ushort address, byte data)
        {
            if (mapper.MapPPUAddressWrite(address, data, out uint newAddress))
            {
                chrData[newAddress] = data;
                return true;
            }

            return false;
        }

        public bool ReadPPUData(ushort address, out byte data)
        {
            if (mapper.MapPPUAddressRead(address, out uint newAddress))
            {
                data = chrData[newAddress];
                return true;
            }

            data = 0;
            return false;
        }
    }
}
