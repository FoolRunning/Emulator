using System;

namespace SystemBase
{
    /// <summary>
    /// Random access memory (RAM)
    /// </summary>
    public sealed class RAM : IBusComponent
    {
        #region Member variables
        private readonly byte[] bytes;
        private readonly uint addressMask;
        #endregion

        #region Constructor
        public RAM(uint size)
        {
            size = Utils.NearestPowerOf2(size);
            addressMask = size - 1;
            bytes = new byte[size];
        }
        #endregion

        #region IBusComponent implementation
        public void Dispose()
        {
        }

        public void Reset()
        {
            Array.Clear(bytes, 0, bytes.Length);
        }

        public void WriteDataFromBus(uint address, byte data)
        {
            bytes[address & addressMask] = data;
        }

        public byte ReadDataForBus(uint address)
        {
            return bytes[address & addressMask];
        }
        #endregion
    }
}
