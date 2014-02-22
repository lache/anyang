﻿using Server.Core;
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

            Logger.Write("{0} is logged.", msg.Name);

            // WorldInfo의 Spawn 패킷에는 자기 자신에 대한 정보를 보내지 않는다.
            var infoMsg = new WorldInfoMsg { Id = _data.ObjectId, WorldId = _data.WorldId };
            infoMsg.SpawnList.AddRange(_world.GetActors<Player>(this).Select(e => e.ToSpawnMsg()));
            SendToNetwork(infoMsg);

            // 시야에 의한 Spawn 패킷을 전파할 때에는 자기 자신까지 포함해서 전달한다.
            var myMsg = ToSpawnMsg();
            foreach (var other in _world.GetActors<NetworkActor>())
                other.SendToNetwork(myMsg);
        }

        SpawnMsg ToSpawnMsg()
        {
            var msg = new SpawnMsg { Id = _data.ObjectId, Name = _data.Name };
            msg.CharacterResource.Id = _data.ObjectId;
            msg.CharacterResource.ResourceId = _data.ResourceId;
            msg.UpdatePosition.Id = _data.ObjectId;
            msg.UpdatePosition.X = _data.X;
            msg.UpdatePosition.Y = _data.Y;
            msg.UpdatePosition.Dir = _data.Dir;
            msg.UpdatePosition.Speed = _data.Speed;
            msg.UpdateHp.Id = _data.ObjectId;
            msg.UpdateHp.MaxHp = _data.MaxHp;
            msg.UpdateHp.Hp = _data.Hp;
            return msg;
        }

        void OnMove(MoveMsg msg)
        {
            _data.X = msg.X;
            _data.Y = msg.Y;
            _data.Dir = msg.Dir;
            _data.Speed = msg.Speed;

            _polator.AddSample(msg.Time, Realtime.Now, new Vector2 { X = msg.X, Y = msg.Y });

            var updatePos = new UpdatePositionMsg
                {
                    Id = msg.Id, X = msg.X, Y = msg.Y, Speed = msg.Speed, Dir = msg.Dir, Time = msg.Time
                };
            foreach (var actor in _world.Actors.OfType<NetworkActor>())
                actor.SendToNetwork(updatePos);
        }

        public override IEnumerable<int> CoroDispose()
        {
            yield return 1000;
            _world.Persist.Store(_data);
            _world.Actors.Remove(this);
            Logger.Write("{0} is logout.", _data.Name);

            var msg = new DespawnMsg { Id = _data.ObjectId };
            foreach (var actor in _world.Actors.OfType<NetworkActor>())
                actor.SendToNetwork(msg);
        }
    }
}
