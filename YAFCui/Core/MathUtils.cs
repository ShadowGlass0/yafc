using System;

namespace YAFC.UI
{
    public static class MathUtils
    {
        public static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
        
        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        public static int Round(float value) => (int) MathF.Round(value);
        public static int Floor(float value) => (int) MathF.Floor(value);
        public static int Ceil(float value) => (int) MathF.Ceiling(value);

        public static byte FloatToByte(float f)
        {
            if (f <= 0)
                return 0;
            if (f >= 1)
                return 255;
            return (byte) MathF.Round(f * 255);
        }
        
        public static int HighestBitSet(ulong x)
        {
            var set = 0;
            if (x > 0xFFFFFFFF) { set += 32; x >>= 32; }
            if (x > 0xFFFF) { set += 16; x >>= 16; }
            if (x > 0xFF) { set += 8; x >>= 8; }
            if (x > 0xF) { set += 4; x >>= 4; }
            if (x > 0x3) { set += 2; x >>= 2; }
            if (x > 0x1) { set += 1; }
            return set;
        }
        
        public static float LogarithmicToLinear(float value, float logmin, float logmax)
        {
            if (value < 0f)
                value = 0f;
            var cur = MathF.Log(value);
            if (cur <= logmin)
                return 0f;
            if (cur >= logmax)
                return 1f;
            return (cur - logmin) / (logmax - logmin);
        }

        public static float LinearToLogarithmic(float value, float logmin, float logmax, float min, float max)
        {
            if (value <= 0f)
                return min;
            if (value >= 1f)
                return max;
            var logcur = logmin + (logmax - logmin) * value;
            return MathF.Exp(logcur);
        }
    }
}