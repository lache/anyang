using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Logic
{
    public struct Vector2
    {
        public double X;
        public double Y;

        public static Vector2 operator * (Vector2 vec, double scalar)
        {
            return new Vector2 { X = vec.X * scalar, Y = vec.Y * scalar };
        }

        public static Vector2 operator + (Vector2 vec1, Vector2 vec2)
        {
            return new Vector2 { X = vec1.X + vec2.X, Y = vec1.Y + vec2.Y };
        }

        public static Vector2 operator - (Vector2 vec1, Vector2 vec2)
        {
            return new Vector2 { X = vec1.X - vec2.X, Y = vec1.Y - vec2.Y };
        }
    }

    public class Extrapolator
    {
        private Vector2 _snapPos = new Vector2();
        private Vector2 _snapVel = new Vector2();
        private Vector2 _aimPos = new Vector2();
        private Vector2 _lastPacketPos = new Vector2();     //  only used when re-constituting velocity
        private double _snapTime;               //  related to snapPos_
        private double _aimTime;                //  related to aimPos_
        private double _lastPacketTime;         //  related to lastPacketPos_
        private double _latency;
        private double _updateTime;

        public Extrapolator()
        {
            Reset(0, 0, new Vector2());
        }

        public bool AddSample(double packetTime, double curTime, Vector2 pos)
        {
            var vel = new Vector2();
            if (Math.Abs(packetTime - _lastPacketTime) > 1e-4)
            {
                double dt = 1.0 / (packetTime - _lastPacketTime);
                vel = (pos - _lastPacketPos) * dt;
            }
            return AddSample(packetTime, curTime, pos, vel);
        }

        public bool AddSample(double packetTime, double curTime, Vector2 pos, Vector2 vel)
        {
            if (!Estimates(packetTime, curTime))
                return false;

            _lastPacketPos = pos;
            _lastPacketTime = packetTime;
            ReadPosition(curTime, out _snapPos);

            _aimTime = curTime + _updateTime;
            double dt = _aimTime - packetTime;
            _snapTime = curTime;
            _aimPos = pos + (vel * dt);

            if (Math.Abs(_aimTime - _snapTime) < 1e-4)
            {
                _snapVel = vel;
            }
            else
            {
                double dt2 = 1.0 / (_aimTime - _snapTime);
                _snapVel = (_aimPos - _snapPos) * dt2;
            }
            return true;
        }

        public void Reset(double packetTime, double curTime, Vector2 pos)
        {
            Reset(packetTime, curTime, pos, new Vector2());
        }

        public void Reset(double packetTime, double curTime, Vector2 pos, Vector2 vel)
        {
            Debug.Assert(packetTime <= curTime);
            _lastPacketTime = packetTime;

            _lastPacketPos = pos;
            _snapTime = curTime;
            _snapPos = pos;
            _updateTime = curTime - packetTime;
            _latency = _updateTime;
            _aimTime = curTime + _updateTime;
            _snapVel = vel;
            _aimPos = _snapPos + (_snapVel * _updateTime);
        }

        public bool ReadPosition(double forTime, out Vector2 oPos)
        {
            var vel = new Vector2();
            return ReadPosition(forTime, out oPos, out vel);
        }

        public bool ReadPosition(double forTime, out Vector2 oPos, out Vector2 oVel)
        {
            bool ok = true;

            //  asking for something before the allowable time?
            if (forTime < _snapTime)
            {
                forTime = _snapTime;
                ok = false;
            }

            //  asking for something very far in the future?
            double maxRange = _aimTime + _updateTime;
            if (forTime > maxRange)
            {
                forTime = maxRange;
                ok = false;
            }

            //  calculate the interpolated position
            oVel = _snapVel;
            oPos = _snapPos + oVel * (forTime - _snapTime);

            if (!ok)
            {
                oVel = new Vector2();
            }
            return ok;
        }

        public double EstimateLatency()
        {
            return _latency;
        }

        public double EstimateUpdateTime()
        {
            return _updateTime;
        }

        private bool Estimates(double packet, double cur)
        {
            if (packet <= _lastPacketTime)
            {
                return false;
            }

            //  The theory is that, if latency increases, quickly 
            //  compensate for it, but if latency decreases, be a 
            //  little more resilient; this is intended to compensate 
            //  for jittery delivery.
            double lat = cur - packet;
            if (lat < 0) lat = 0;
            if (lat > _latency)
            {
                _latency = (_latency + lat) * 0.5;
            }
            else
            {
                _latency = (_latency * 7 + lat) * 0.125;
            }

            //  Do the same running average for update time.
            //  Again, the theory is that a lossy connection wants 
            //  an average of a higher update time.
            double tick = packet - _lastPacketTime;
            if (tick > _updateTime)
            {
                _updateTime = (_updateTime + tick) * 0.5;
            }
            else
            {
                _updateTime = (_updateTime * 7 + tick) * 0.125;
            }

            return true;
        }
    }
}
