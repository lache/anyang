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
    [DataContract]
    class PlayerData : PersistenceData
    {
        [DataMember]
        public string Name { get; set; }
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
        [DataMember]
        public int ResourceId { get; set; }
        [DataMember]
        public int MaxHp { get; set; }
        [DataMember]
        public int Hp { get; set; }
    }

    class Player : NetworkActor
    {
        private readonly Extrapolator _polator = new Extrapolator();
        private PlayerData _data = new PlayerData { Name = "nonamed" };
        private bool _logged;

        public Player(World world, Session session)
            : base(world, session)
        {
        }

        void OnEnterWorld(EnterWorldMsg msg)
        {
            _data = _world.Persist.Find<PlayerData>(e => e.Name == msg.Name);
            if (_data == null)
            {
                _data = new PlayerData { Name = msg.Name };
                _world.Persist.Store(_data);
            }
            _world.Actors.Add(this);
            _polator.Reset(0, 0, new Vector2 { X = _data.X, Y = _data.Y });

            _logged = true;
            Logger.Write("{0} is logged.", msg.Name);

            // WorldInfo의 Spawn 패킷에는 자기 자신에 대한 정보를 보내지 않는다.
            var infoMsg = new WorldInfoMsg(_data.ObjectId, _data.WorldId, Realtime.Now,
                _world.GetActors<Player>(this).Select(e => e.ToSpawnMsg()).Concat(_world.GetActors<Npc>(this).Select(e => e.ToSpawnMsg())).ToList());
            SendToNetwork(infoMsg);

            // 시야에 의한 Spawn 패킷을 전파할 때에는 자기 자신까지 포함해서 전달한다.
            var myMsg = ToSpawnMsg();
            foreach (var other in _world.GetActors<NetworkActor>())
                other.SendToNetwork(myMsg);

            _world.Coro.AddEntry(CoroUpdatePos);
        }

        SpawnMsg ToSpawnMsg()
        {
            return new SpawnMsg(_data.ObjectId, _data.Name,
                new CharacterResourceMsg(_data.ObjectId, _data.ResourceId),
                new UpdatePositionMsg(_data.ObjectId, _data.X, _data.Y, _data.Dir, _data.Speed, Realtime.Now, false),
                new UpdateHpMsg(_data.ObjectId, _data.MaxHp, _data.Hp));
        }

        private void MovePosition(double x, double y, double dir, double speed)
        {
            _data.X = x;
            _data.Y = y;
            _data.Dir = dir;
            _data.Speed = speed;

            var posMsg = new UpdatePositionMsg(_data.ObjectId, x, y, speed, dir, Realtime.Now, false);
            foreach (var actor in _world.GetActors<NetworkActor>())
                actor.SendToNetwork(posMsg);
        }

        private double _prevX, _prevY;
        void OnMove(MoveMsg msg)
        {
            if (_prevX != msg.X || _prevY != msg.Y)
            {
                Logger.Write("C {0}, {1}", msg.X, msg.Y);
                _prevX = msg.X;
                _prevY = msg.Y;
            }
            _polator.AddSample(msg.Time, Realtime.Now, new Vector2 { X = msg.X, Y = msg.Y });
        }

        void OnChat(ChatMsg msg)
        {
            msg.Name = _data.Name;
            foreach (var actor in _world.GetActors<NetworkActor>())
                actor.SendToNetwork(msg);
        }

        public override IEnumerable<int> CoroDispose()
        {
            yield return 1000;
            _world.Persist.Store(_data);
            _world.Actors.Remove(this);
            _logged = false;
            Logger.Write("{0} is logout.", _data.Name);

            var msg = new DespawnMsg { Id = _data.ObjectId };
            foreach (var actor in _world.Actors.OfType<NetworkActor>())
                actor.SendToNetwork(msg);
        }

        private IEnumerable<int> CoroUpdatePos()
        {
            int count = 0;
            while (_logged)
            {
                Vector2 pos, velocity;
                var result = _polator.ReadPosition(Realtime.Now, out pos, out velocity);

                MovePosition(pos.X, pos.Y, _data.Dir, _data.Speed);

                const int sendCount = 20;
                yield return 1000 / sendCount;

                if (++count % sendCount == 0)
                {
                    Logger.Write("S {0}, {1} = {2}", pos.X, pos.Y, result);
                    // Logger.Write(Realtime.Now);
                }
            }
        }
    }
}
