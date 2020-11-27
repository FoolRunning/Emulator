using System;
using System.Collections.Generic;
using System.Diagnostics;
using SystemBase;
using SystemBase.Bus;

namespace System_NES
{
    internal sealed class DMATransfer : IBusComponent_16
    {
        #region Member variables
        private readonly IClock clock;
        private readonly Bus_16 bus;
        private readonly ICPU cpu;
        private readonly PPU ppu;
        private volatile bool doTransfer;
        private IEnumerator<ClockTick> transferOp;
        private bool isEvenClock = true;
        private byte dmaAddress;
        private byte dmaPage;
        #endregion

        #region Constructor
        public DMATransfer(IClock clock, Bus_16 bus, ICPU cpu, PPU ppu)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.bus = bus ?? throw new ArgumentNullException(nameof(bus));
            this.cpu = cpu ?? throw new ArgumentNullException(nameof(cpu));
            this.ppu = ppu ?? throw new ArgumentNullException(nameof(ppu));
            clock.ClockTick += Clock_ClockTick;
        }
        #endregion

        #region IDisposable implementation
        public void Dispose()
        {
            clock.ClockTick -= Clock_ClockTick;
        }
        #endregion

        #region IBusComponent_16 implementation
        public void WriteDataFromBus(ushort address, byte data)
        {
            Debug.Assert(address == 0x4014);

            dmaPage = data;
            dmaAddress = 0;
            cpu.Pause();

            transferOp = DoTransfer().GetEnumerator();
            doTransfer = true;
        }

        public byte ReadDataForBus(ushort address)
        {
            throw new NotImplementedException("DMA transfer register can not be read");
        }
        #endregion

        #region Event handlers
        private void Clock_ClockTick()
        {
            isEvenClock = !isEvenClock;
            if (doTransfer)
                HandleDMATransferTick();
        }
        #endregion

        #region Transfer handling methods
        private void HandleDMATransferTick()
        {
            if (!transferOp.MoveNext())
            {
                doTransfer = false;
                cpu.Resume();
            }
        }

        private IEnumerable<ClockTick> DoTransfer()
        {
            yield return new ClockTick();

            if (isEvenClock)
                yield return new ClockTick();

            for (int i = 0; i <= 255; i++)
            {
                yield return new ClockTick();
                Debug.Assert(isEvenClock);
                byte data = bus.ReadData((ushort)((dmaPage << 8) | dmaAddress));

                yield return new ClockTick();
                ppu.WriteOAM(dmaAddress++, data);
            }
        }
        #endregion
    }
}
