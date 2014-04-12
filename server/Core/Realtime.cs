using System.Runtime.InteropServices;

namespace Server.Core
{
    public static class Realtime
    {
        [DllImport("kernel32.dll")]
        extern static short QueryPerformanceCounter(ref long x);
        [DllImport("kernel32.dll")]
        extern static short QueryPerformanceFrequency(ref long x);

        private static readonly double TickMultiply;
        private static readonly long BaseReading;

        static Realtime()
        {
            long tps = 0;
            QueryPerformanceFrequency(ref tps);
            TickMultiply = 1.0 / tps;

            QueryPerformanceCounter(ref BaseReading);
        }

        public static void Update()
        {
            Now = Get();
        }

        public static double Now { get; private set; }

        private static double Get()
        {
            long now = 0;
            QueryPerformanceCounter(ref now);

            return (now - BaseReading) * TickMultiply;
        }
    }
}
