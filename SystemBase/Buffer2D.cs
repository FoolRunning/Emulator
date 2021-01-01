using System.Diagnostics;

namespace SystemBase
{
    public sealed class Buffer2D
    {
        private readonly ushort width;
        private readonly ushort height;
        private readonly RgbColor[] data;

        public Buffer2D(ushort width, ushort height)
        {
            this.width = width;
            this.height = height;
            data = new RgbColor[width * height];
        }

        public RgbColor[] InternalBuffer => data;

        public RgbColor this[ushort x, ushort y]
        {
            get => data[CalculateOffset(x, y)];
            set => data[CalculateOffset(x, y)] = value;
        }

        private int CalculateOffset(ushort x, ushort y)
        {
            Debug.Assert(x < width && y < height);
            return x + y * width;
        }
    }
}
