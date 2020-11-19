namespace System_NES
{
    internal abstract class Mapper
    {
        protected readonly ushort prgBankCount;
        protected readonly byte chrBankCount;

        public Mapper(ushort prgBankCount, byte chrBankCount)
        {
            this.prgBankCount = prgBankCount;
            this.chrBankCount = chrBankCount;
        }

        public abstract bool MapCPUAddress(ushort address, out ushort newAddress);
        
        public abstract bool MapPPUAddress(ushort address, out ushort newAddress);
    }
}
