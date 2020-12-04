﻿using System;
using System.Diagnostics;
using System.Threading;

namespace SystemBase
{
    public sealed class SystemClock : IClock, ITickProvider
    {
        #region Events/Member variables
        public event Action OneSecondTick;

        private readonly Stopwatch timer;
        private readonly Thread clockThread;
        private readonly double ticksPerClock;
        private readonly long ticksPerOneSecond;

        private volatile bool run;
        private double ticksForNextClock;
        private long prevTicksOneSecond;
        private long totalTicks;
        #endregion

        #region Constructor
        public SystemClock(long frequency)
        {
            timer = new Stopwatch();

            ticksPerClock = Stopwatch.Frequency / (double)frequency;
            ticksPerOneSecond = Stopwatch.Frequency;

            run = true;
            clockThread = new Thread(ClockLoop);
            clockThread.Priority = ThreadPriority.AboveNormal;
            clockThread.IsBackground = true;
            clockThread.Name = "Clock";
        }
        #endregion

        #region IDisposable implementation
        public void Dispose()
        {
            run = false;
            if (clockThread.IsAlive)
                clockThread.Join();
        }
        #endregion

        #region IClock implementation
        public event Action ClockTick;
        #endregion

        #region ITickProvider implementation
        public long TotalTickCount => Interlocked.Read(ref totalTicks);
        #endregion

        #region Public methods
        public void Start()
        {
            clockThread.Start();
        }
        #endregion

        #region Main clock loop
        private void ClockLoop()
        {
            if (ClockTick == null)
                throw new InvalidOperationException("Can not start clock with no tick listeners");

            timer.Start();

            ticksForNextClock = timer.ElapsedTicks + ticksPerClock;
            prevTicksOneSecond = timer.ElapsedTicks + ticksPerOneSecond;

            while (run)
            {
                long currentTicks = timer.ElapsedTicks;
                if (currentTicks >= prevTicksOneSecond)
                {
                    prevTicksOneSecond += ticksPerOneSecond;
                    OneSecondTick?.Invoke();
                }

                if (currentTicks >= ticksForNextClock)
                {
                    Interlocked.Increment(ref totalTicks);
                    ticksForNextClock += ticksPerClock;
                    ClockTick.Invoke();
                }
            }

            timer.Stop();
        }
        #endregion
    }
}
