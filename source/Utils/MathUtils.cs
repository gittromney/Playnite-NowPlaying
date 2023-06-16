using System;

namespace NowPlaying.Utils
{
    public static class MathUtils
    {
        public static double Log2(double x)
        {
            return Math.Log(x) / Math.Log(2);
        }

        public static int Clamp(int x, int bound1, int bound2)
        {
            var lowerBound = Math.Min(bound1, bound2);
            var upperBound = Math.Max(bound1, bound2);
            return x < lowerBound ? lowerBound : x > upperBound ? upperBound : x;
        }
    }
}
