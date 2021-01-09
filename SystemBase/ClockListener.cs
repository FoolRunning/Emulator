using System;
using System.Threading;

namespace SystemBase
{
    public abstract class ClockListener : ITickProvider, IDisposable
    {
        #region Member variables
        public static bool SynchronousClock = false;

        private readonly Thread tickThread;
        private readonly IClock clock;
        private volatile bool run;
        private volatile bool enabled = true;
        private ulong requestedTickCount;
        private ulong totalTicks;
        #endregion

        #region Constructor
        protected ClockListener(IClock clock)
        {
            this.clock = clock;

            run = true;
            tickThread = new Thread(TickLoop);
            tickThread.IsBackground = true;
            tickThread.Name = GetType().Name;
            tickThread.Priority = ThreadPriority.AboveNormal;
            
            if (SynchronousClock)
                clock.ClockTick += Clock_ClockTick_Synchronous;
            else
                clock.ClockTick += Clock_ClockTick;
        }
        #endregion

        #region IDisposable implementation
        public void Dispose()
        {
            clock.ClockTick -= Clock_ClockTick;
            run = false;
            if (tickThread.IsAlive)
                tickThread.Join();
        }
        #endregion

        #region Public methods
        public void Start()
        {
            tickThread.Start();
        }

        public void Pause()
        {
            enabled = false;
        }

        public void Resume()
        {
            enabled = true;
        }
        #endregion
        
        #region ITickProvider implementation
        public ulong TotalTickCount => totalTicks;

        public ulong ExpectedTicksPerSecond => clock.ExpectedTicksPerSecond;
        #endregion

        protected abstract void HandleSingleTick();
        
        #region Event handlers
        private void Clock_ClockTick()
        {
            requestedTickCount++;
        }

        private void Clock_ClockTick_Synchronous()
        {
            while (requestedTickCount > totalTicks) // Wait until current tick is processed
            {
            }

            requestedTickCount++;
        }
        #endregion

        #region Main tick loop
        private void TickLoop()
        {
            while (run)
            {
                if (requestedTickCount <= totalTicks)
                    continue;

                totalTicks++;
                
                if (enabled)
                    HandleSingleTick();
            }
        }
        #endregion
    }
}
