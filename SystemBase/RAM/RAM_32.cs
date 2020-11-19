using SystemBase.Bus;

namespace SystemBase.RAM
{
    /// <summary>
    /// Random access memory (RAM) for a 32-bit bus
    /// </summary>
    public sealed class RAM_32 : IRAM, IBusComponent_32
    {
        #region Member variables
        private readonly byte[] bytes;
        private readonly uint addressMask;
        #endregion

        #region Constructor
        public RAM_32(uint size)
        {
            size = Utils.NearestPowerOf2(size);
            addressMask = size - 1;
            bytes = new byte[size];
        }
        #endregion

        public uint Size => (uint)bytes.Length;

        #region IBusComponent_32 implementation
        public void Dispose()
        {
        }

        public void WriteDataFromBus(uint address, byte data)
        {
            address &= addressMask;
            bytes[address] = data;
        }

        public byte ReadDataForBus(uint address)
        {
            address &= addressMask;
            return bytes[address];
        }

        public void Attached(Bus_32 bus)
        {
            // Nothing to do
        }
        #endregion
    }
}
