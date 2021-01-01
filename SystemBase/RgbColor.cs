namespace SystemBase
{
    /// <summary>
    /// Simple color structure in 24-bit RGB format
    /// </summary>
    public struct RgbColor
    {
        private readonly int value;

        public RgbColor(byte red, byte green, byte blue)
        {
            value = ((red & 0xFF) << 16) | ((green & 0xFF) << 8) | (blue & 0xFF);
        }

        public RgbColor(int rgb)
        {
            value = rgb & 0xFFFFFF;
        }

        public int RGB => value;

        public byte R => (byte)((value >> 16) & 0xFF);

        public byte G => (byte)((value >> 8) & 0xFF);

        public byte B => (byte)(value & 0xFF);

        public override string ToString()
        {
            return $"RGB({R},{G},{B})";
        }
    }
}
