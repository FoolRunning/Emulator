using System;

namespace SystemBase
{
    public interface IClock : ITickProvider, IDisposable
    {
        event Action ClockTick;
    }
}
