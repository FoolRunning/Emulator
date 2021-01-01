namespace System_NES.Mappers
{
    internal sealed class Mapper000 : Mapper
    {
        private readonly uint cpuAddressMask;

        public Mapper000(ushort prgBankCount, byte chrBankCount, MirrorMode cartMirrorMode) : base(prgBankCount, chrBankCount, cartMirrorMode)
        {
            cpuAddressMask = prgBankCount == 1 ? (uint)0x3FFF : (uint)0x7FFF;
        }

        #region Mapper implementation
        public override MirrorMode MirrorMode => cartMirrorMode;

        public override void Reset()
        {
            // Nothing to do
        }

        public override bool MapCPUAddressRead(ushort address, out uint newAddress, out byte data)
        {
            data = 0;
            if (address >= 0x8000 && address <= 0xFFFF)
            {
                newAddress = address & cpuAddressMask;
                return true;
            }

            newAddress = 0;
            return false;
        }

        public override bool MapCPUAddressWrite(ushort address, byte data, out uint newAddress)
        {
            if (address >= 0x8000 && address <= 0xFFFF)
            {
                newAddress = address & cpuAddressMask;
                return true;
            }

            newAddress = 0;
            return false;
        }

        public override bool MapPPUAddressRead(ushort address, out uint newAddress)
        {
            if (address <= 0x1FFF)
            {
                newAddress = address;
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
