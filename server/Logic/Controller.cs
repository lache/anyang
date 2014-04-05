using Server.Core;
using Server.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Server.Logic
{
    interface IController
    {
        bool Attached { get; set; }
    }

    class Controller<TData> : IController
    {
        protected readonly Actor _actor;
        protected readonly TData _data;

        protected Controller(Actor actor, TData data)
        {
            _actor = actor;
            _data = data;
        }

        public bool Attached { get; set; }
    }

    [DataContract]
    class CharacterData
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public int ResourceId { get; set; }
        [DataMember]
        public int MaxHp { get; set; }
        [DataMember]
        public int Hp { get; set; }

        public int Radius { get; set; }
    }

    class CharacterController : Controller<CharacterData>
    {
        public CharacterController(Actor actor, CharacterData data)
            : base(actor, data)
        {
        }

        public SpawnMsg MakeSpawnMsg()
        {
            return new SpawnMsg(_actor.ObjectId, _data.Name,
                new CharacterResourceMsg(_actor.ObjectId, _data.ResourceId, _data.Radius),
                _actor.Get<MoveController>().MakeMoveMsg(),
                new UpdateHpMsg(_actor.ObjectId, _data.MaxHp, _data.Hp));
        }
    }

    [DataContract]
    class MoveData
    {
        [DataMember]
        public int WorldId { get; set; }
        [DataMember]
        public double X { get; set; }
        [DataMember]
        public double Y { get; set; }
        [DataMember]
        public double Dir { get; set; }
        [DataMember]
        public double Speed { get; set; }
    }

    class MoveController : Controller<MoveData>
    {
        private readonly Extrapolator _polator = new Extrapolator();

        public MoveController(Actor actor, MoveData data)
            : base(actor, data)
        {
        }

        public Position Pos { get { return new Position { X = (int)_data.X, Y = (int)_data.Y }; } }

        public MoveMsg MakeMoveMsg(bool instanceMove = false)
        {
            return new MoveMsg(_actor.ObjectId, _data.X, _data.Y, _data.Dir, _data.Speed, Realtime.Now, instanceMove);
        }

        public void Reset()
        {
            _polator.Reset(0, 0, new Vector2 { X = _data.X, Y = _data.Y });
        }

        public bool ProcessPacket(MoveMsg msg)
        {
            if (msg.InstanceMove)
            {
                _polator.Reset(msg.Time, Realtime.Now, new Vector2 { X = msg.X, Y = msg.Y });
                return Move(msg.X, msg.Y, _data.Dir, _data.Speed, /* instanceMove = */ true);
            }
            else
            {
                _polator.AddSample(msg.Time, Realtime.Now, new Vector2 { X = msg.X, Y = msg.Y });
                return Update();
            }
        }

        public bool Update()
        {
            Vector2 pos;
            if (!_polator.ReadPosition(Realtime.Now, out pos))
                return false;

            return Move(pos.X, pos.Y, _data.Dir, _data.Speed);
        }

        public bool Move(double x, double y, double dir, double speed, bool instanceMove = false)
        {
            if (_data.X == x && _data.Y == y && _data.Dir == dir && _data.Speed == speed)
                return false;

            _data.X = x;
            _data.Y = y;
            _data.Dir = dir;
            _data.Speed = speed;
            _actor.Broadcast(MakeMoveMsg(instanceMove), true);
            return true;
        }

        public IEnumerable<int> CoroUpdatePos()
        {
            while (Attached)
            {
                Update();

                const int sendCount = 4 /* it's magic */;
                yield return 1000 / sendCount;
            }
        }
    }
}
