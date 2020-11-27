namespace System_NES.Mappers
{
    internal sealed class Mapper000 : Mapper
    {
        public Mapper000(ushort prgBankCount, byte chrBankCount) : base(prgBankCount, chrBankCount)
        {
        }

        public override bool MapCPUAddressRead(ushort address, out uint newAddress)
        {
            if (address >= 0x8000 && address <= 0xFFFF)
            {
                newAddress = (uint)(address & (prgBankCount == 1 ? 0x3FFF : 0x7FFF));
                return true;
            }

            newAddress = 0;
            return false;
        }

        public override bool MapCPUAddressWrite(ushort address, out uint newAddress)
        {
            if (address >= 0x8000 && address <= 0xFFFF)
            {
                newAddress = (uint)(address & (prgBankCount == 1 ? 0x3FFF : 0x7FFF));
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

        public override bool MapPPUAddressWrite(ushort address, out uint newAddress)
        {
            if (address <= 0x1FFF && chrBankCount == 0)
            {
                newAddress = address; // Treat as RAM
                return true;
            }

            newAddress = 0;
            return false;
        }
    }
}
