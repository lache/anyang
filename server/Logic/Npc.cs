using Server.Core;
using Server.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Logic
{
    class NpcData
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public int WorldId { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Dir { get; set; }
        public double Speed { get; set; }
        public int ResourceId { get; set; }
        public int MaxHp { get; set; }
        public int Hp { get; set; }
    }

    class Npc : AiActor
    {
        private static object IssueLock = new object();
        private static int NpcIssued = 9999;
        protected NpcData _data;
       
        public Npc(World world, Ai ai, NpcData data)
            : base(world, ai)
        {
            _data = data;
            lock (IssueLock)
            {
                _data.Id = NpcIssued;
                NpcIssued++;
            }
        }

        public override bool IsAlive()
        {
            return _data.Hp > 0;
        }

        public override IEnumerable<int> CoroEntry()
        {
            return base.CoroAiEntry();
        }

        public override IEnumerable<int> CoroDispose()
        {
            return base.CoroDispose();
        }

        public override bool Equals(object obj)
        {
            var npc = obj as Npc;
            if (npc == null) return false;
            return npc._data.Id == _data.Id;
        }

        public override int GetHashCode()
        {
            return _data.Id;
        }

        public override string ToString()
        {
            return string.Format("Npc Id: {0}, Type: {1}, IsAlive: {2}",
                _data.Id, GetType().Name, IsAlive());
        }

        public SpawnMsg ToSpawnMsg()
        {
            return new SpawnMsg(_data.Id, _data.Name,
                new CharacterResourceMsg(_data.Id, _data.ResourceId),
                new UpdatePositionMsg(_data.Id, _data.X, _data.Y, _data.Dir, _data.Speed, Realtime.Now, false),
                new UpdateHpMsg(_data.Id, _data.MaxHp, _data.Hp));
        }

        protected void MovePosition(int x, int y, double dir, double speed)
        {
            _data.X = x;
            _data.Y = y;
            _data.Dir = dir;
            _data.Speed = speed;
            BroadcastToNetworkActors(new UpdatePositionMsg(_data.Id, x, y, dir, speed, Realtime.Now, false));
        }

        protected void BroadcastToNetworkActors<T>(T msg) where T : IMessage
        {
            foreach (var actor in _world.Actors.OfType<NetworkActor>())
                actor.SendToNetwork(msg);
        }
    }

    class RoamingNpc : Npc
    {
        private readonly List<Position> _roamingPointList;
        private int _moveTo = 0;
        public RoamingNpc(World world, Ai ai, NpcData data, List<Position> roamingPoints)
            : base(world, ai, data)
        {
            _roamingPointList = roamingPoints;
        }

        public override IEnumerable<int> CoroEntry()
        {
            BroadcastToNetworkActors(ToSpawnMsg());

            while (true)
            {
                // 이미 도달한 경우 다음 목적지로 이동한다
                var curPos = new Position { X = (int)_data.X, Y = (int)_data.Y };
                if (curPos == _roamingPointList[_moveTo])
                {
                    _moveTo++;
                    _moveTo = _moveTo % _roamingPointList.Count;
                }

                // 길찾기를 해봅시다
                Location.X = (int)_data.X; Location.Y = (int)_data.Y;
                var nextPos = this.FindWay(_roamingPointList[_moveTo]);

                // 클라에 알려줍니다
                MovePosition(nextPos.X, nextPos.Y, curPos.ToDirection(nextPos).ToClientDirection(), 1);
                yield return NextRandom(1000, 2000);
            }
        }
    }

}
