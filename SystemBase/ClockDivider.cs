using System;

namespace SystemBase
{
    public sealed class ClockDivider : IClock
    {
        private readonly uint clockDivisor;
        private uint parentTicksBeforeTick;

        public ClockDivider(IClock clock, uint clockDivisor)
        {
            if (clockDivisor == 0)
                throw new ArgumentException("clockDivisor not valid");

            this.clockDivisor = clockDivisor;
            parentTicksBeforeTick = clockDivisor;

            clock.ClockTick += Clock_ClockTick;
        }

        public void Dispose()
        {
        }

        public event Action ClockTick;

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
