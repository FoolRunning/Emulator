using System;
using System.Collections.Generic;
using System.Linq;

namespace SystemBase
{
    /// <summary>
    /// Represents a system data bus with 32-bit address and 8-bit data capabilities
    /// </summary>
    public sealed class SystemBus
    {
        #region Member variables
        private readonly List<BusAddressRange> componentRanges = new List<BusAddressRange>();
        private readonly List<IBusComponent> components = new List<IBusComponent>();
        private readonly uint maxAddress;
        #endregion

        public SystemBus(uint maxAddress)
        {
            this.maxAddress = maxAddress;
        }

        #region Properties
        public IEnumerable<IBusComponent> AllComponents => components;
        #endregion

        #region Public methods
        public void Reset()
        {
            components.ForEach(comp => comp.Reset());
        }

        public void IRQ<T>() where T : IInterruptRequestHandler
        {
            IInterruptRequestHandler handler = components.OfType<T>().FirstOrDefault();
            handler?.IRQ();
        }

        public void NMI<T>() where T : IInterruptRequestHandler
        {
            IInterruptRequestHandler handler = components.OfType<T>().FirstOrDefault();
            handler?.NMI();
        }

        public void WriteData(ushort address, byte data)
        {
            if (address > maxAddress)
                throw new ArgumentOutOfRangeException(nameof(address));

            for (int i = 0; i < componentRanges.Count; i++)
            {
                if (componentRanges[i].InRange(address))
                    components[i].WriteDataFromBus(address, data);
            }
        }

        public byte ReadData(ushort address)
        {
            if (address > maxAddress)
                throw new ArgumentOutOfRangeException(nameof(address));

            for (int i = 0; i < componentRanges.Count; i++)
            {
                if (componentRanges[i].InRange(address))
                    return components[i].ReadDataForBus(address);
            }

            return 0;
        }

        public void AddComponent(IBusComponent component, BusAddressRange addressRange)
        {
            componentRanges.Add(addressRange);
            components.Add(component);
        }

        public void RemoveComponent(IBusComponent component)
        {
            int index = components.IndexOf(component);
            if (index < 0) 
                return;

            componentRanges.RemoveAt(index);
            components.RemoveAt(index);
        }
        #endregion
    }

    #region BusAddressRange structure
    public sealed class BusAddressRange
    {
        private readonly uint start;
        private readonly uint end;

        public BusAddressRange(uint start, uint end)
        {
            if (start > end)
                throw new ArgumentException("start must be less than or equal to end");

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
