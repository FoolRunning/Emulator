namespace System_NES.Mappers
{
    internal sealed class Mapper002 : SimpleMapper
    {
        public Mapper002(ushort prgBankCount, byte chrBankCount, MirrorMode cartMirrorMode) : base(prgBankCount, chrBankCount, cartMirrorMode)
        {
            prgSelectedBankLow = 0;
            prgSelectedBankHigh = (uint)(prgBankCount - 1);
        }

        #region SimpleMapper implementation
        public override bool MapCPUAddressWrite(uint address, byte data, out uint newAddress)
        {
            if (address >= 0x8000 && address <= 0xFFFF)
            {
                prgSelectedBankLow = (uint)(data & 0x0F);
                newAddress = MapperHandled;
                return true;
            }

            newAddress = 0;
            return false;
        }
        #endregion
    }
}
