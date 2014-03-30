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
        private readonly PlayerData _data;

        public Player(World world, Session session, PlayerData data)
            : base(world, session)
        {
            _data = data;
            Add<CharacterController>(_data.Character);
            Add<MoveController>(_data.Move);
            ObjectId = _data.ObjectId;
        }

        void OnChat(ChatMsg msg)
        {
            msg.Name = _data.Character.Name;

            // admin command 처리
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
                // 일반 채팅 메시지 처리
                Broadcast(msg);
            }
        }

        protected override IEnumerable<int> CoroMainEntry()
        {
            // 처음 시작할 때 로그인에 대한 처리를 해준다.
            _world.Actors.Add(this);
            Get<MoveController>().Reset();

            Logger.Write("{0} is logged.", _data.Character.Name);

            // WorldInfo의 Spawn 패킷에는 자기 자신에 대한 정보를 보내지 않는다.
            var infoMsg = new WorldInfoMsg(ObjectId, _data.Move.WorldId, Realtime.Now,
                _world.Actors.Get<CharacterController>().Select(e => e.MakeSpawnMsg()).ToList());
            SendToNetwork(infoMsg);

            // 시야에 의한 Spawn 패킷을 전파할 때에는 자기 자신까지 포함해서 전달한다.
            Broadcast(Get<CharacterController>().MakeSpawnMsg());

            _world.Coro.AddEntry(this, Get<MoveController>().CoroUpdatePos);

            // 현재는 Tick마다 할 일이 없다.
            while (Connected)
            {
                yield return 1000;
            }
        }

        protected override IEnumerable<int> CoroDispose()
        {
            yield return 1000;
            _world.Persist.Store(_data);
            _world.Actors.Remove(this);
            Logger.Write("{0} is logout.", _data.Character.Name);

            Broadcast(new DespawnMsg { Id = _data.ObjectId });
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
