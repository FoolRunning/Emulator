using System;
using System.Drawing;
using System.Threading;

namespace SystemBase
{
    public sealed class TickCountDisplay : ITextDisplay
    {
        private readonly ITickProvider tickProvider;
        private long prevTickCount;
        private long ticksInOneSecond;

        public TickCountDisplay(string title, ITickProvider tickProvider, SystemClock clock)
        {
            this.tickProvider = tickProvider ?? throw new ArgumentNullException(nameof(tickProvider));
            Title = title;
            clock.OneSecondTick += Timer_Tick;
        }

        public event Action FrameFinished;
        
        public string Title { get; }
        
        public Size Size => new Size(150, 50);

        public string Text
        {
            get
            {
                long tios = Interlocked.Read(ref ticksInOneSecond);
                return tios.ToString("###,###,###");
            }
        }

        public Color Color => Color.White;

        private void Timer_Tick()
        {
            long newTickCount = tickProvider.TotalTickCount;
            Interlocked.Exchange(ref ticksInOneSecond, newTickCount - prevTickCount);
            prevTickCount = newTickCount;
            FrameFinished?.Invoke();
        }
    }

    public interface ITickProvider
    {
        long TotalTickCount { get; }
    }
}
