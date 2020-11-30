using SystemBase;

namespace System_NES
{
    internal sealed class APU : IBusComponent_16, ISoundGenerator
    {
        public void Dispose()
        {
            
        }

        public void WriteDataFromBus(ushort address, byte data)
        {
            
        }

        public byte ReadDataForBus(ushort address)
        {
            return 0;
        }
    }
}
