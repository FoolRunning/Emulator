using SystemBase;

namespace System_NES.Mappers
{
    internal abstract class SimpleMapper : Mapper
    {
        protected uint prgSelectedBankLow;
        protected uint prgSelectedBankHigh;
        protected uint chrSelectedBank;

        protected SimpleMapper(ushort prgBankCount, byte chrBankCount, MirrorMode cartMirrorMode) : base(prgBankCount, chrBankCount, cartMirrorMode)
        {
        }

        #region Mapper implementation
        public override MirrorMode MirrorMode => cartMirrorMode;

        public override void Reset()
        {
            prgSelectedBankLow = 0;
            prgSelectedBankHigh = (uint)(prgBankCount - 1);
            chrSelectedBank = 0;
        }

        public override bool MapCPUAddressRead(ushort address, out uint newAddress, out byte data)
        {
            data = 0;
            if (address >= 0x8000 && address <= 0xBFFF)
            {
                newAddress = (uint)((address & 0x3FFF) + prgSelectedBankLow * Utils.Kilo16);
                return true;
            }

            if (address >= 0xC000 && address <= 0xFFFF)
            {
                newAddress = (uint)((address & 0x3FFF) + prgSelectedBankHigh * Utils.Kilo16);
                return true;
            }

            newAddress = 0;
            return false;
        }

        public override bool MapPPUAddressRead(ushort address, out uint newAddress)
        {
            if (address <= 0x1FFF)
            {
                newAddress = address + chrSelectedBank * Utils.Kilo8;
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
    }
}
