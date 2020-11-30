using System.Collections.Generic;

namespace SystemBase.Bus
{
    /// <summary>
    /// Represents a 16-bit bus
    /// </summary>
    public sealed class Bus_16 : IBus
    {
        #region Member variables
        private readonly List<BusAddressRange_16> componentRanges = new List<BusAddressRange_16>();
        private readonly List<IBusComponent_16> components = new List<IBusComponent_16>();
        #endregion

        #region IBus implementation
        public IEnumerable<IBusComponent> AllComponents => components;
        #endregion

        #region Public methods
        public void WriteData(ushort address, byte data)
        {
            for (int i = 0; i < componentRanges.Count; i++)
            {
                if (componentRanges[i].InRange(address))
                    components[i].WriteDataFromBus(address, data);
            }
        }

        public byte ReadData(ushort address)
        {
            for (int i = 0; i < componentRanges.Count; i++)
            {
                if (componentRanges[i].InRange(address))
                    return components[i].ReadDataForBus(address);
            }

            return 0;
        }

        public void AddComponent(IBusComponent_16 component, BusAddressRange_16 addressRange)
        {
            componentRanges.Add(addressRange);
            components.Add(component);
        }

        public void RemoveComponent(IBusComponent_16 component)
        {
            int index = components.IndexOf(component);
            if (index < 0) 
                return;

            componentRanges.RemoveAt(index);
            components.RemoveAt(index);
        }
        #endregion
    }

    #region BusAddressRange_16 structure
    public readonly struct BusAddressRange_16
    {
        private readonly ushort start;
        private readonly ushort end;

        public BusAddressRange_16(ushort start, ushort end)
        {
            this.start = start;
            this.end = end;
        }

        public bool InRange(ushort val)
        {
            return start <= val && val <= end;
        }
    }
    #endregion
}
