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
        public CharacterData Character { get; set; }
        [DataMember]
        public MoveData Move { get; set; }

        public PlayerData()
        {
            Character = new CharacterData();
            Move = new MoveData();
        }

        public PlayerData(string name)
            : this()
        {
            Character.Name = name;
        }
    }

    partial class Player : NetworkActor
    {
        private PlayerData _data = new PlayerData("nonamed");

        public Player(World world, Session session)
            : base(world, session)
        {
        }

        void Initialize()
        {
            Add<CharacterController>(_data.Character);
            Add<MoveController>(_data.Move);
            ObjectId = _data.ObjectId;
        }

        void OnEnterWorld(EnterWorldMsg msg)
        {
            _data = _world.Persist.Find<PlayerData>(e => e.Character.Name == msg.Name);
            if (_data == null)
            {
                _data = new PlayerData(msg.Name);
                _world.Persist.Store(_data);
            }
            Initialize();

            _world.Actors.Add(this);
            Get<MoveController>().Reset();

            Logger.Write("{0} is logged.", msg.Name);

            // WorldInfo의 Spawn 패킷에는 자기 자신에 대한 정보를 보내지 않는다.
            var infoMsg = new WorldInfoMsg(ObjectId, _data.Move.WorldId, Realtime.Now,
                _world.Actors.Get<CharacterController>().Select(e => e.MakeSpawnMsg()).ToList());
            SendToNetwork(infoMsg);

            // 시야에 의한 Spawn 패킷을 전파할 때에는 자기 자신까지 포함해서 전달한다.
            var myMsg = Get<CharacterController>().MakeSpawnMsg();
            foreach (var other in _world.GetActors<NetworkActor>())
                other.SendToNetwork(myMsg);

            _world.Coro.AddEntry(this, Get<MoveController>().CoroUpdatePos);
        }

        void OnChat(ChatMsg msg)
        {
            msg.Name = _data.Character.Name;

            bool success;
            if (Commands.Instance.IsAdminCommand(msg.Message) &&
                Commands.Instance.Dispatch(this, msg.Message, out success))
            {
                var report = msg.Message + " -> " + (success ? "ok." : "fail.");
                msg.Message = report;

                Logger.Write(report);
                Broadcast(msg);
            }
            else
            {
                Broadcast(msg);
            }
        }

        protected override IEnumerable<int> CoroDispose()
        {
            yield return 1000;
            _world.Persist.Store(_data);
            _world.Actors.Remove(this);
            Logger.Write("{0} is logout.", _data.Character.Name);

            var msg = new DespawnMsg { Id = _data.ObjectId };
            foreach (var actor in _world.Actors.OfType<NetworkActor>())
                actor.SendToNetwork(msg);
        }

        void OnMove(MoveMsg msg)
        {
            Get<MoveController>().ProcessPacket(msg);
        }
    }

    #region Admin Commands

    partial class Player
    {
        [CommandHandler("help", "명령어를 확인합니다")]
        internal bool SendHelpOfCommands()
        {
            var message = new StringBuilder();
            foreach (var info in Commands.Instance.HandlerInfos)
            {
                message.AppendLine(string.Format(".{0} -> {1}", info.Command, info.Description));

                // 첫 번째 인자로 들어가는 admin은 생략한다.
                for (var paramIndex = 1; paramIndex < info.Parameters.Length; ++paramIndex)
                {
                    var param = info.Parameters[paramIndex];
                    message.AppendLine(string.Format("  [{0}] {1} ({2})", paramIndex, param.Description, param.ActualType.Name));
                }
            }

            Broadcast(new ChatMsg(ObjectId, _data.Character.Name, message.ToString()));
            return true;
        }
    }

    #endregion
}
