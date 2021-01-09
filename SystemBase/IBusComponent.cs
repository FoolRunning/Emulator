using System;

namespace SystemBase
{
    /// <summary>
    /// A component that can be attached to a system bus
    /// </summary>
    public interface IBusComponent : IDisposable
    {
        void Reset();

        void WriteDataFromBus(uint address, byte data);
        byte ReadDataForBus(uint address);
    }
}
