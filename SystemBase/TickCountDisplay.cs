using System;
using System.Drawing;
using System.Threading;

namespace SystemBase
{
    public sealed class TickCountDisplay : ITextDisplay
    {
        #region Member variables
        private readonly ITickProvider tickProvider;
        private readonly long expectedTicksPerSecond;
        private long prevTickCount;
        private long ticksInOneSecond;
        #endregion

        #region Constructor
        public TickCountDisplay(string title, ITickProvider tickProvider, SystemClock clock, long expectedTicksPerSecond)
        {
            this.tickProvider = tickProvider ?? throw new ArgumentNullException(nameof(tickProvider));
            this.expectedTicksPerSecond = expectedTicksPerSecond;
            Title = title;
            clock.OneSecondTick += Clock_OneSecondTick;
        }
        #endregion

        #region ITextDisplay implementation
        public event Action FrameFinished;
        
        public string Title { get; }
        
        public Size Size => new Size(100, 25);

        public string Text
        {
            get
            {
                long tios = Interlocked.Read(ref ticksInOneSecond);
                return $"{tios:###,###,###} ({tios * 100.0 / expectedTicksPerSecond:###.00}%)";
            }
        }

        public Color Color => Color.White;
        #endregion

        #region Event handlers
        private void Clock_OneSecondTick()
        {
            long newTickCount = tickProvider.TotalTickCount;
            Interlocked.Exchange(ref ticksInOneSecond, newTickCount - prevTickCount);
            prevTickCount = newTickCount;
            FrameFinished?.Invoke();
        }
        #endregion
    }

    public interface ITickProvider
    {
        long TotalTickCount { get; }
    }
}
