using System;

namespace SystemBase
{
    public sealed class ClockDivider : IClock
    {
        private readonly IClock parentClock;
        private readonly uint clockDivisor;
        private uint parentTicksBeforeTick;

        public ClockDivider(IClock parentClock, uint clockDivisor)
        {
            if (clockDivisor == 0)
                throw new ArgumentException("clockDivisor not valid");

            this.parentClock = parentClock;
            this.clockDivisor = clockDivisor;
            parentTicksBeforeTick = clockDivisor;

            parentClock.ClockTick += Clock_ClockTick;
        }

        public void Dispose()
        {
        }

        public event Action ClockTick;

        public ulong TotalTickCount => parentClock.TotalTickCount / clockDivisor;
        
        public ulong ExpectedTicksPerSecond => parentClock.ExpectedTicksPerSecond / clockDivisor;

        private void Clock_ClockTick()
        {
            parentTicksBeforeTick--;
            if (parentTicksBeforeTick > 0) 
                return;

            parentTicksBeforeTick = clockDivisor;
            ClockTick?.Invoke();
        }
    }
}
