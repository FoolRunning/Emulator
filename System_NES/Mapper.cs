namespace System_NES
{
    internal abstract class Mapper
    {
        public const uint MapperHandled = 0xFFFFFFFF;

        protected readonly ushort prgBankCount;
        protected readonly byte chrBankCount;
        protected readonly MirrorMode cartMirrorMode;

        protected Mapper(ushort prgBankCount, byte chrBankCount, MirrorMode cartMirrorMode)
        {
            this.prgBankCount = prgBankCount;
            this.chrBankCount = chrBankCount;
            this.cartMirrorMode = cartMirrorMode;
        }

        public abstract MirrorMode MirrorMode { get; }

        public abstract bool MapCPUAddressRead(ushort address, out uint newAddress);
        public abstract bool MapCPUAddressWrite(ushort address, byte data, out uint newAddress);
        
        public abstract bool MapPPUAddressRead(ushort address, out uint newAddress);
        public abstract bool MapPPUAddressWrite(ushort address, byte data, out uint newAddress);
    }
}
