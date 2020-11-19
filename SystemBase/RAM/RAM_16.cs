namespace SystemBase.RAM
{
    /// <summary>
    /// Random access memory (RAM) for a 16-bit bus
    /// </summary>
    public sealed class RAM_16 : IRAM, IBusComponent_16
    {
        #region Member variables
        private readonly byte[] bytes;
        private readonly uint addressMask;
        #endregion

        #region Constructor
        public RAM_16(uint size)
        {
            size = Utils.NearestPowerOf2(size);
            addressMask = size - 1;
            bytes = new byte[size];
        }
        #endregion

        public uint Size => (uint)bytes.Length;

        #region IBusComponent_16 implementation
        public void Dispose()
        {
        }

        public void WriteDataFromBus(ushort address, byte data)
        {
            address = (ushort)(address & addressMask);
            bytes[address] = data;
        }

        public byte ReadDataForBus(ushort address)
        {
            address = (ushort)(address & addressMask);
            return bytes[address];
        }
        #endregion
    }
}
