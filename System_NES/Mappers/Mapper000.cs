namespace System_NES.Mappers
{
    internal sealed class Mapper000 : Mapper
    {
        public Mapper000(ushort prgBankCount, byte chrBankCount) : base(prgBankCount, chrBankCount)
        {
        }

        public override bool MapCPUAddress(ushort address, out ushort newAddress)
        {
            if (address >= 0x8000 && address <= 0xFFFF)
            {
                newAddress = (ushort)(address & (prgBankCount == 1 ? 0x3FFF : 0x7FFF));
                return true;
            }

            newAddress = 0;
            return false;
        }

        public override bool MapPPUAddress(ushort address, out ushort newAddress)
        {
            if (address <= 0x1FFF)
            {
                newAddress = address;
                return true;
            }

            newAddress = 0;
            return false;
        }
    }
}
