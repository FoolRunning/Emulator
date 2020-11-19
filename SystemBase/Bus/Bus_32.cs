using System.Collections.Generic;

namespace SystemBase.Bus
{
    /// <summary>
    /// Represents a 32-bit bus
    /// </summary>
    public sealed class Bus_32 : IBus
    {
        #region Member variables
        private readonly List<BusAddressRange_32> componentRanges = new List<BusAddressRange_32>();
        private readonly List<IBusComponent_32> components = new List<IBusComponent_32>();
        #endregion

        #region IBus implementation
        public IEnumerable<IBusComponent> AllComponents => components;
        #endregion

        #region Public methods
        public void WriteData(uint address, byte data)
        {
            for (int i = 0; i < componentRanges.Count; i++)
            {
                if (componentRanges[i].InRange(address))
                    components[i].WriteDataFromBus(address, data);
            }
        }

        public byte ReadData(uint address)
        {
            for (int i = 0; i < componentRanges.Count; i++)
            {
                if (componentRanges[i].InRange(address))
                    return components[i].ReadDataForBus(address);
            }
            return 0;
        }

        public void AddComponent(IBusComponent_32 component, BusAddressRange_32 addressRange)
        {
            componentRanges.Add(addressRange);
            components.Add(component);
        }
        #endregion
    }

    #region BusAddressRange_32 structure
    public readonly struct BusAddressRange_32
    {
        public static readonly BusAddressRange_32 None = new BusAddressRange_32(uint.MaxValue, 0);

        private readonly uint start;
        private readonly uint end;

        public BusAddressRange_32(uint start, uint end)
        {
            this.start = start;
            this.end = end;
        }

        public bool InRange(uint val)
        {
            return start <= val && val <= end;
        }
    }
    #endregion
}
