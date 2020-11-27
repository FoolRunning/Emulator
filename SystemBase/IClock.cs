using System;

namespace SystemBase
{
    public interface IClock : IDisposable
    {
        event Action ClockTick;
    }
}
