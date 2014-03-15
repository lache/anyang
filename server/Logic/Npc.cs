using Server.Core;
using Server.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Logic
{
    class NpcData
    {
        public CharacterData Character { get; set; }
        public MoveData Move { get; set; }
        public NpcData()
        {
            Character = new CharacterData();
            Move = new MoveData();
        }
    }

    class Npc : AiActor
    {
        private static int NpcIssued = 9999;
        protected NpcData _data;
       
        public Npc(World world, Ai ai, NpcData data)
            : base(world, ai)
        {
            _data = data;
            Add<CharacterController>(_data.Character);
            Add<MoveController>(_data.Move);

            ObjectId = Interlocked.Increment(ref NpcIssued);
        }

        public override bool IsAlive()
        {
            return _data.Character.Hp > 0;
        }

        public override bool Equals(object obj)
        {
            var npc = obj as Npc;
            if (npc == null) return false;
            return npc.ObjectId == ObjectId;
        }

        public override int GetHashCode()
        {
            return ObjectId;
        }

        public override string ToString()
        {
            return string.Format("Npc Id: {0}, Type: {1}, IsAlive: {2}",
                ObjectId, GetType().Name, IsAlive());
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

        protected override IEnumerable<int> CoroMainEntry()
        {
            Broadcast(Get<CharacterController>().MakeSpawnMsg());

            while (IsAlive())
            {
                var moveCtrl = Get<MoveController>();

                // 이미 도달한 경우 다음 목적지로 이동한다
                var curPos = moveCtrl.Pos;
                if (curPos == _roamingPointList[_moveTo])
                {
                    _moveTo++;
                    _moveTo = _moveTo % _roamingPointList.Count;
                }

                // 길찾기를 해봅시다
                Location = moveCtrl.Pos;
                var nextPos = this.FindWay(_roamingPointList[_moveTo]);

                // 클라에 알려줍니다
                //const int magic = 24;
                const int magic = 1;
                moveCtrl.Move(nextPos.X * magic, nextPos.Y * magic, curPos.ToDirection(nextPos).ToClientDirection(), 1);
                yield return NextRandom(500, 800);
            }
        }
    }

}
