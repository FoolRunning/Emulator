using System;

namespace SystemBase
{
    public interface ICPU : IInterruptRequestHandler, IDisposable
    {
        void Start();
        
        void Pause();

        void Resume();
    }
}
