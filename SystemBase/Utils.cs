using System.Runtime.CompilerServices;

namespace SystemBase
{
    public static class Utils
    {
        public static uint NearestPowerOf2(uint val)
        {
            uint test = 1;
            while (val > test)
                test <<= 1;
            return test;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasFlag(this byte b, byte maskValue)
        {
            return (b & maskValue) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetFlag(this ref byte b, byte maskValue, bool set)
        {
            if (set)
                b |= maskValue;
            else
                b &= (byte)~maskValue;
        }
    }
}
