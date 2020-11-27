using System;
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
        Horizontal,
        Vertical,
        OneScreenLo,
        OneScreenHigh
    }
    #endregion

    internal sealed class Cartridge : IBusComponent_16
    {
        #region Member variables
        private readonly byte[] prgData;
        private readonly byte[] chrData;
        private readonly byte[] trainerData;
        private readonly Mapper mapper;
        private readonly InfoFlags info;
        private readonly Timing timing;
        #endregion

        #region Constructor
        public Cartridge(string filePath)
        {
            using (BinaryReader reader = new BinaryReader(new FileStream(filePath, FileMode.Open)))
            {
                if (reader.BaseStream.Length < 16)
                    return;

                byte[] header = reader.ReadBytes(16);
                if (header[0] != 'N' || header[1] != 'E' || header[2] != 'S' && header[3] != 0x1A)
                    return;

                int mapperId;
                byte chrBankCount;
                ushort prgBankCount;
                if ((header[7] & 0x0C) != 0x08)
                {
                    // v1 format
                    prgBankCount = header[4];
                    chrBankCount = header[5];
                    info = (InfoFlags)(header[6] & 0x0F);
                    mapperId = (header[7] & 0xF0) | ((header[6] & 0xF0) >> 4);
                    timing = (Timing)(header[9] & 0x01);
                }
                else
                {
                    // v2 format
                    prgBankCount = (ushort)(((header[9] & 0x0F) << 8) | header[4]);
                    chrBankCount = header[5];
                    info = (InfoFlags)(header[6] & 0x0F);
                    mapperId = ((header[8] & 0xF0) << 4) | (header[7] & 0xF0) | ((header[6] & 0xF0) >> 4);
                    timing = (Timing)(header[12] & 0x03);
                }

                if (timing != Timing.NTSC)
                    Console.WriteLine("WARNING: Loaded non-NTSC ROM");

                if (info.HasFlag(InfoFlags.Trainer))
                {
                    trainerData = new byte[512];
                    reader.Read(trainerData, 0, trainerData.Length);
                }

                int prgSize = prgBankCount * 16384;
                int chrSize = chrBankCount * 8192;
                if (chrSize == 0)
                    chrSize = 8192; // Cartridge has RAM?

                prgData = new byte[prgSize];
                reader.Read(prgData, 0, prgSize);
                chrData = new byte[chrSize];
                reader.Read(chrData, 0, chrSize);

                switch (mapperId)
                {
                    case 0: mapper = new Mapper000(prgBankCount, chrBankCount); break;
                    default:
                        throw new NotImplementedException("Mapper " + mapperId + " is not implemented");
                }
            }
        }
        #endregion

        public void Dispose()
        {
        }

        public MirrorMode Mirror =>
            info.HasFlag(InfoFlags.Mirroring) ? MirrorMode.Vertical : MirrorMode.Horizontal;

        #region IBusComponent_16 implementation
        public void WriteDataFromBus(ushort address, byte data)
        {
            //if (mapper.MapCPUAddressWrite(address, out uint newAddress))
            //    prgData[newAddress] = data;
        }

        public byte ReadDataForBus(ushort address)
        {
            if (mapper.MapCPUAddressRead(address, out uint newAddress))
                return prgData[newAddress];

            //if (address >= 0x7000 && address <= 0x71FF)
            //    return trainerData?[address - 0x7000] ?? 0;

            return 0;
        }
        #endregion

        public bool WritePPUData(ushort address, byte data)
        {
            if (mapper.MapPPUAddressWrite(address, out uint newAddress))
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
