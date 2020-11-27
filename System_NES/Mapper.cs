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

        public abstract bool MapCPUAddressRead(ushort address, out uint newAddress);
        public abstract bool MapCPUAddressWrite(ushort address, out uint newAddress);
        
        public abstract bool MapPPUAddressRead(ushort address, out uint newAddress);
        public abstract bool MapPPUAddressWrite(ushort address, out uint newAddress);
    }
}
