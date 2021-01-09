using SystemBase;

namespace System_NES.Mappers
{
    internal sealed class Mapper001 : Mapper
    {
        #region PRGBankMode enumeration
        private enum PRGBankMode
        {
            Single1,
            Single2,
            FixedLow,
            FixedHigh
        }
        #endregion

        #region CHRBankMode enumeration
        private enum CHRBankMode
        {
            Single,
            Double
        }
        #endregion

        #region Member variables
        private readonly byte[] cartridgeRAM;

        private byte prgBankSelectedLo;
        private byte prgBankSelectedHi;

        private byte chrBankSelectedLo;
        private byte chrBankSelectedHi;

        private byte loadRegister;
        private byte loadRegisterCount;
        
        private PRGBankMode prgBankMode;
        private CHRBankMode chrBankMode;
        private MirrorMode mirrorMode;
        #endregion

        #region Constructor
        public Mapper001(ushort prgBankCount, byte chrBankCount, MirrorMode cartMirrorMode, InfoFlags info) : 
            base(prgBankCount, chrBankCount, cartMirrorMode)
        {
            //if (info.HasFlag(InfoFlags.Battery))
            cartridgeRAM = new byte[Utils.Kilo8]; // TODO: Persist
        }
        #endregion

        #region Mapper implementation
        public override MirrorMode MirrorMode => mirrorMode;

        public override void Reset()
        {
            loadRegister = 0;
            loadRegisterCount = 0;
            SetControlRegister(0x1C);

            mirrorMode = MirrorMode.Horizontal;

            prgBankSelectedLo = 0;
            prgBankSelectedHi = (byte)(prgBankCount - 1);

            chrBankSelectedLo = 0;
            chrBankSelectedHi = 0;
        }

        public override bool MapCPUAddressRead(uint address, out uint newAddress, out byte data)
        {
            if (address >= 0x6000 && address <= 0x7FFF)
            {
                newAddress = MapperHandled;
                data = cartridgeRAM[address & 0x1FFF];
                return true;
            }

            data = 0;
            if (address >= 0x8000)
            {
                if (prgBankMode == PRGBankMode.FixedLow || prgBankMode == PRGBankMode.FixedHigh)
                {
                    if (address <= 0xBFFF)
                        newAddress = (uint)(prgBankSelectedLo * Utils.Kilo16 + (address & 0x3FFF));
                    else
                        newAddress = (uint)(prgBankSelectedHi * Utils.Kilo16 + (address & 0x3FFF));
                    return true;
                }

                newAddress = (uint)(prgBankSelectedLo * Utils.Kilo32 + (address & 0x7FFF));
                return true;
            }

            newAddress = 0;
            return false;
        }

        public override bool MapCPUAddressWrite(uint address, byte data, out uint newAddress)
        {
            if (address >= 0x6000 && address <= 0x7FFF)
            {
                newAddress = MapperHandled;
                cartridgeRAM[address & 0x1FFF] = data;
                return true;
            }

            if (address >= 0x8000)
            {
                if ((data & 0x80) != 0)
                {
                    // Reset load register
                    loadRegister = 0x00;
                    loadRegisterCount = 0;
                    prgBankMode = PRGBankMode.FixedHigh;
                    newAddress = 0;
                    return false;
                }

                loadRegister >>= 1;
                loadRegister |= (byte)((data & 0x01) << 4);
                loadRegisterCount++;

                if (loadRegisterCount == 5)
                {
                    if (address <= 0x9FFF)
                        SetControlRegister((byte)(loadRegister & 0x1F));
                    else if (address <= 0xBFFF)
                        chrBankSelectedLo = (byte)(loadRegister & 0x1F);
                    else if (address <= 0xDFFF)
                        chrBankSelectedHi = (byte)(loadRegister & 0x1F);
                    else
                    {
                        switch (prgBankMode)
                        {
                            case PRGBankMode.Single1:
                            case PRGBankMode.Single2:
                                prgBankSelectedLo = (byte)((loadRegister & 0x0E) >> 1);
                                break;
                            case PRGBankMode.FixedLow:
                                prgBankSelectedLo = 0;
                                prgBankSelectedHi = (byte)(loadRegister & 0x0F);
                                break;
                            case PRGBankMode.FixedHigh:
                                prgBankSelectedLo = (byte)(loadRegister & 0x0F);
                                prgBankSelectedHi = (byte)(prgBankCount - 1);
                                break;
                        }
                    }

                    loadRegister = 0x00;
                    loadRegisterCount = 0;
                }
            }

            newAddress = 0;
            return false;
        }

        public override bool MapPPUAddressRead(ushort address, out uint newAddress)
        {
            if (address < 0x2000)
            {
                if (chrBankCount == 0)
                {
                    newAddress = address;
                    return true;
                }

                if (chrBankMode == CHRBankMode.Double)
                {
                    if (address <= 0x0FFF)
                        newAddress = (uint)(chrBankSelectedLo * Utils.Kilo4 + (address & 0x0FFF));
                    else
                        newAddress = (uint)(chrBankSelectedHi * Utils.Kilo4 + (address & 0x0FFF));
                    return true;
                }

                newAddress = (uint)(chrBankSelectedLo * Utils.Kilo8 + (address & 0x1FFF));
                return true;
            }

            newAddress = 0;
            return false;
        }

        public override bool MapPPUAddressWrite(ushort address, byte data, out uint newAddress)
        {
            if (address <= 0x1FFF && chrBankCount == 0)
            {
                newAddress = address; // Treat as RAM
                return true;
            }

            newAddress = 0;
            return false;
        }
        #endregion

        #region Private helper methods
        private void SetControlRegister(byte data)
        {
            mirrorMode = (MirrorMode)(data & 0x03);
            prgBankMode = (PRGBankMode)((data >> 2) & 0x03);
            chrBankMode = (CHRBankMode)((data >> 4) & 0x01);
        }
        #endregion
    }
}
