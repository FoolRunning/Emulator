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

        public abstract void Reset();

        public abstract bool MapCPUAddressRead(uint address, out uint newAddress, out byte data);
        public abstract bool MapCPUAddressWrite(uint address, byte data, out uint newAddress);
        
        public abstract bool MapPPUAddressRead(ushort address, out uint newAddress);
        public abstract bool MapPPUAddressWrite(ushort address, byte data, out uint newAddress);
    }
}
