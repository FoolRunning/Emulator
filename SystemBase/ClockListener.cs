using System;
using System.Threading;

namespace SystemBase
{
    public abstract class ClockListener : ITickProvider, IDisposable
    {
        #region Member variables
        private readonly Thread tickThread;
        private readonly IClock clock;
        private volatile bool processNextTick;
        private volatile bool run;
        private volatile bool enabled = true;
        private long totalTicks;
        #endregion

        #region Constructor
        protected ClockListener(IClock clock, long expectedTicksPerSecond, string listenerDescription)
        {
            this.clock = clock;
            ExpectedTicksPerSecond = expectedTicksPerSecond;

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

        public long ExpectedTicksPerSecond { get; }
        #endregion

        protected abstract void HandleSingleTick();

        #region Event handlers
        private void Clock_ClockTick()
        {
            while (processNextTick) // Prefer slowdown versus getting overwhelmed with ticks
            {
            }

            processNextTick = true;
        }
        #endregion

        #region Main tick loop
        private void TickLoop()
        {
            processNextTick = false;
            while (run)
            {
                if (!processNextTick) 
                    continue;

                processNextTick = false;
                Interlocked.Increment(ref totalTicks);
                
                if (enabled)
                    HandleSingleTick();
            }
        }
        #endregion
    }
}
