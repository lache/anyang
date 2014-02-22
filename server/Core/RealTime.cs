using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Server.Core
{
    public static class Realtime
    {
        [DllImport("kernel32.dll")]
        extern static short QueryPerformanceCounter(ref long x);
        [DllImport("kernel32.dll")]
        extern static short QueryPerformanceFrequency(ref long x);

        private static readonly double _tickMultiply;
        private static readonly long _maxDelta;

        private static long _baseReading;
        private static long _lastRead;

        static Realtime()
        {
          long tps = 0;
          QueryPerformanceFrequency(ref tps);
          _tickMultiply = 1.0 / (double)tps;
          _maxDelta = (long)(tps * 0.1);

          QueryPerformanceCounter(ref _baseReading);
          _lastRead = _baseReading;
        }

        private static double _now;
        public static void Update()
        {
            _now = Get();
        }

        public static double Now
        {
            get { return _now; }
        }

        private static double Get()
        {
          long now = 0;
          QueryPerformanceCounter(ref now);

          //  work around dual-core bug
          if (now < _lastRead) {
            now = _lastRead + 1;
          }
          if (now - _lastRead > _maxDelta) {
            //  don't advance time too much all at once
            _baseReading += now - _lastRead - _maxDelta;
          }
          _lastRead = now;
          return (now - _baseReading) * _tickMultiply;
        }
    }
}
