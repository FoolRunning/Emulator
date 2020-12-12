using System.Runtime.CompilerServices;

namespace SystemBase
{
    public static class Utils
    {
        public const ushort eightKilobytes = 8192;
        public const ushort sixteenKilobytes = eightKilobytes * 2;

        private static readonly byte[] reverseBitLookup = { 0x0, 0x8, 0x4, 0xc, 0x2, 0xa, 0x6, 0xe, 0x1, 0x9, 0x5, 0xd, 0x3, 0xb, 0x7, 0xf };

        public static uint NearestPowerOf2(uint val)
        {
            uint test = 1;
            while (val > test)
                test <<= 1;
            return test;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasFlag(this ref byte b, byte maskValue)
        {
            return (b & maskValue) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetOrClearFlag(this ref byte b, byte maskValue, bool set)
        {
            if (set)
                b |= maskValue;
            else
                b &= (byte)~maskValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetFlag(this ref byte b, byte maskValue)
        {
            b |= maskValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearFlag(this ref byte b, byte maskValue)
        {
            b &= (byte)~maskValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Taken from https://stackoverflow.com/a/2603254/4953232 </remarks>
        public static byte ReverseBits(this ref byte b)
        {
            // Reverse the top and bottom nibble then swap them.
            return (byte)((reverseBitLookup[b & 0x0F] << 4) | reverseBitLookup[b >> 4]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Taken from https://youtu.be/72dI7dB3ZvQ?t=2961 </remarks>
        public static float FastSine(float v)
        {
            float j = v * 0.15915f;
            j -= (int)j;
            return 20.785f * j * (j - 0.5f) * (j - 1.0f);
        }
    }
}
