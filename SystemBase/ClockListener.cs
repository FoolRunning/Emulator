using System;
using System.Threading;

namespace SystemBase
{
    public abstract class ClockListener : ITickProvider, IDisposable
    {
        #region Member variables
        private readonly Thread tickThread;
        private readonly IClock clock;
        private volatile int ticksToRun;
        private volatile bool run;
        private volatile bool enabled = true;
        private long totalTicks;
        #endregion

        #region Constructor
        protected ClockListener(IClock clock, string listenerDescription)
        {
            this.clock = clock;

            run = true;
            tickThread = new Thread(TickLoop);
            tickThread.IsBackground = true;
            tickThread.Name = listenerDescription;

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
        public long TotalTickCount => Interlocked.Read(ref totalTicks);
        #endregion

        protected abstract void HandleSingleTick();

        #region Event handlers
        private void Clock_ClockTick()
        {
            Interlocked.Increment(ref ticksToRun);
#if DEBUG
            while (ticksToRun > 2) // Prefer slowdown versus getting overwhelmed with ticks
            {
            }
#endif
        }
        #endregion

        #region Main tick loop
        private void TickLoop()
        {
            ticksToRun = 0;

            while (run)
            {
                if (ticksToRun <= 0) 
                    continue;

                Interlocked.Decrement(ref ticksToRun);
                Interlocked.Increment(ref totalTicks);
                
                if (enabled)
                    HandleSingleTick();
            }
        }
        #endregion
    }
}
