using System;

namespace SystemBase
{
    public interface ICPU : IDisposable
    {
        void Start();

        /// <summary>
        /// Processor reset
        /// </summary>
        void Reset();

        /// <summary>
        /// Interrupt request
        /// </summary>
        void IRQ();

        /// <summary>
        /// Non-maskable interrupt request
        /// </summary>
        void NMI();

        void Pause();

        void Resume();
    }
}
