using System.Collections.Generic;
using System.Diagnostics;
using SystemBase;
using SystemBase.CPUs;

namespace System_NES
{
    internal sealed class CPU_2A03 : CPU_6502
    {
        #region Member variables
        private readonly SystemBus bus;
        private volatile bool doTransfer;
        private IEnumerator<ClockTick> transferOp;
        private bool isEvenClock = true;
        private ushort dmaAddress;
        #endregion

        #region Constructor
        public CPU_2A03(IClock clock, SystemBus bus) : base(clock, bus)
        {
            this.bus = bus;
        }
        #endregion

        #region Overrides of CPU_6502
        public override void WriteDataFromBus(uint address, byte data)
        {
            Debug.Assert(address == 0x4014);
            dmaAddress = (ushort)(data << 8);
            doTransfer = true;
        }

        protected override void HandleSingleTick()
        {
            isEvenClock = !isEvenClock;
            if (!doTransfer)
                base.HandleSingleTick();
            else
            {
                if (transferOp == null)
                    transferOp = DoTransfer().GetEnumerator();

                if (!transferOp.MoveNext())
                {
                    doTransfer = false;
                    transferOp = null;
                }
            }
        }
        #endregion

        #region Transfer handling methods
        private IEnumerable<ClockTick> DoTransfer()
        {
            yield return new ClockTick();

            if (isEvenClock)
                yield return new ClockTick();

            for (int i = 0; i <= 255; i++)
            {
                yield return new ClockTick();
                Debug.Assert(isEvenClock);
                byte data = bus.ReadData(dmaAddress++);

                yield return new ClockTick();
                bus.WriteData(0x2004, data);
            }
        }
        #endregion
    }
}
