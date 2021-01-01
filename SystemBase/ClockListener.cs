using System;
using System.Threading;

namespace SystemBase
{
    public abstract class ClockListener : ITickProvider, IDisposable
    {
        #region Member variables
        public static bool SynchronousClock = true;

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
        public long TotalTickCount => Interlocked.Read(ref totalTicks);

        public long ExpectedTicksPerSecond { get; }
        #endregion

        protected abstract void HandleSingleTick();
        
        #region Event handlers
        private void Clock_ClockTick()
        {
            processNextTick = true;
        }

        private void Clock_ClockTick_Synchronous()
        {
            while (processNextTick) // Wait until current tick is processed
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
