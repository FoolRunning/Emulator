using SystemBase;

namespace System_NES.Mappers
{
    internal sealed class Mapper004 : Mapper
    {
        private readonly IBus bus;

        public Mapper004(ushort prgBankCount, byte chrBankCount, MirrorMode cartMirrorMode, IBus bus) : 
            base(prgBankCount, chrBankCount, cartMirrorMode)
        {
            this.bus = bus;
        }

        public override MirrorMode MirrorMode { get; }

        public override void Reset()
        {
        }

        public override bool MapCPUAddressRead(ushort address, out uint newAddress, out byte data)
        {
            newAddress = 0;
            data = 0;
            return false;
        }

        public override bool MapCPUAddressWrite(ushort address, byte data, out uint newAddress)
        {
            newAddress = 0;
            return false;
        }

        public override bool MapPPUAddressRead(ushort address, out uint newAddress)
        {
            newAddress = 0;
            return false;
        }

        public override bool MapPPUAddressWrite(ushort address, byte data, out uint newAddress)
        {
            newAddress = 0;
            return false;
        }
    }
}
