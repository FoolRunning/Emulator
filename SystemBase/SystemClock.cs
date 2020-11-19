using System;
using System.Diagnostics;
using System.Threading;

namespace SystemBase
{
    public sealed class SystemClock : IDisposable
    {
        #region Events/Member variables
        public event Action ClockTick;

        private readonly Stopwatch timer;
        private readonly Thread clockThread;
        private readonly double ticksPerClock;

        private volatile bool enabled;
        private volatile bool run;
        private double neededTicksForNextClock;
        private long prevTicks;
        #endregion

        #region Constructor
        public SystemClock(long frequency)
        {
            timer = new Stopwatch();

            ticksPerClock = Stopwatch.Frequency / (double)frequency;

            run = true;
            enabled = true;
            clockThread = new Thread(ClockLoop);
            clockThread.Priority = ThreadPriority.AboveNormal;
            clockThread.IsBackground = true;
            clockThread.Name = "Clock";
        }
        #endregion

        #region Implementation of IDisposable
        public void Dispose()
        {
            run = false;
            if (clockThread.IsAlive)
                clockThread.Join();
        }
        #endregion

        #region Public methods
        public void Start()
        {
            clockThread.Start();
        }

        public void Pause()
        {
            enabled = false;
        }

        public void Resume()
        {
            enabled = true;
        }

        public void SingleStep()
        {
            if (enabled)
                throw new InvalidOperationException("Can not single-step while clock is enabled");
            
            ClockTick?.Invoke();
        }
        #endregion

        #region Main clock loop
        private void ClockLoop()
        {
            timer.Start();

            prevTicks = timer.ElapsedTicks;
            neededTicksForNextClock = ticksPerClock;
            while (run)
            {
                long currentTicks = timer.ElapsedTicks;
                double tickDelta = currentTicks - prevTicks;
                if (tickDelta <= neededTicksForNextClock) 
                    continue;

                prevTicks = currentTicks;

                if (enabled)
                    ClockTick?.Invoke();

                neededTicksForNextClock = neededTicksForNextClock + ticksPerClock - tickDelta; // Determine next delta needed while preserving precision
                if (neededTicksForNextClock < 0)
                    neededTicksForNextClock = 0;
            }

            timer.Stop();
        }
        #endregion
    }
}
