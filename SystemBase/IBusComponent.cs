using System;

namespace SystemBase
{
    public interface IBusComponent : IDisposable
    {
    }

    /// <summary>
    /// A component that can be attached to a 16-bit bus
    /// </summary>
    public interface IBusComponent_16 : IBusComponent
    {
        void WriteDataFromBus(ushort address, byte data);
        byte ReadDataForBus(ushort address);
    }

    /// <summary>
    /// A component that can be attached to a 32-bit bus
    /// </summary>
    public interface IBusComponent_32 : IBusComponent
    {
        void WriteDataFromBus(uint address, byte data);
        byte ReadDataForBus(uint address);
    }
}
