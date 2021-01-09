using System;
using System.Drawing;
using System.Text;

namespace SystemBase
{
    public sealed class TickCountDisplay : ITextDisplay
    {
        #region Member variables
        private readonly ITickProvider[] tickProviders;
        private readonly string[] titles;
        private readonly ulong[] prevTickCount;
        private readonly ulong[] ticksInOneSecond;
        #endregion

        #region Constructor
        public TickCountDisplay(SystemClock clock, string[] titles, ITickProvider[] tickProviders)
        {
            this.tickProviders = tickProviders ?? throw new ArgumentNullException(nameof(tickProviders));
            this.titles = titles;

            prevTickCount = new ulong[tickProviders.Length];
            ticksInOneSecond = new ulong[tickProviders.Length];

            clock.OneSecondTick += Clock_OneSecondTick;
        }
        #endregion

        #region ITextDisplay implementation
        public event Action FrameFinished;

        public string Title => "Clock Frequency";
        
        public Size Size => new Size(150, 12 * tickProviders.Length);

        public string Text
        {
            get
            {
                StringBuilder bldr = new StringBuilder();
                for (int i = 0; i < tickProviders.Length; i++)
                {
                    ulong tios = ticksInOneSecond[i];
                    bldr.AppendLine($"{titles[i]}: {tios:###,###,###} ({tios * 100.0 / tickProviders[i].ExpectedTicksPerSecond:###.00}%)");
                }
                return bldr.ToString();
            }
        }

        public Color Color => Color.White;
        #endregion

        #region Event handlers
        private void Clock_OneSecondTick()
        {
            for (int i = 0; i < tickProviders.Length; i++)
            {
                ulong newTickCount = tickProviders[i].TotalTickCount;
                ticksInOneSecond[i] = newTickCount - prevTickCount[i];
                prevTickCount[i] = newTickCount;
            }
            FrameFinished?.Invoke();
        }
        #endregion
    }

    public interface ITickProvider
    {
        ulong TotalTickCount { get; }

        ulong ExpectedTicksPerSecond { get; }
    }
}
