namespace System_NES.Mappers
{
    internal sealed class Mapper003 : SimpleMapper
    {
        public Mapper003(ushort prgBankCount, byte chrBankCount, MirrorMode cartMirrorMode) : base(prgBankCount, chrBankCount, cartMirrorMode)
        {
        }

        #region SimpleMapper implementation
        public override bool MapCPUAddressWrite(ushort address, byte data, out uint newAddress)
        {
            if (address >= 0x8000 && address <= 0xFFFF)
            {
                chrSelectedBank = (uint)(data & 0x03);
                newAddress = MapperHandled;
                return true;
            }

            newAddress = uint.MaxValue;
            return false;
        }
        #endregion
    }
}
