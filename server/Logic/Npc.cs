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

    class Npc : Actor
    {
        private static int NpcIssued = 9999;
        protected NpcData _data;
        protected Random _random = new Random(DateTime.Now.Millisecond);
       
        public Npc(World world, NpcData data)
            : base(world)
        {
            _data = data;
            Add<CharacterController>(_data.Character);
            Add<MoveController>(_data.Move);

            Location = _data.Move.ToPosition();

            ObjectId = Interlocked.Increment(ref NpcIssued);

            _world.Actors.Add(this);
            Broadcast(Get<CharacterController>().MakeSpawnMsg());
        }

        public bool IsAlive()
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

        public int NextRandom(int min, int max)
        {
            return _random.Next(min, max);
        }
    }

    class RoamingNpc : Npc
    {
        private readonly List<Position> _roamingPointList;
        private int _moveTo = 0;
        public RoamingNpc(World world, NpcData data, List<Position> roamingPoints)
            : base(world, data)
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
                moveCtrl.Move(nextPos.X, nextPos.Y, curPos.ToDirection(nextPos).ToClientDirection(), 1);
                yield return NextRandom(500, 800);
            }
            
            Broadcast(new DespawnMsg { Id = ObjectId });
        }
    }

    class HungryNpc : Npc
    {
        public HungryNpc(World world, NpcData data)
            : base(world, data)
        {
        }

        protected override IEnumerable<int> CoroMainEntry()
        {
            Broadcast(Get<CharacterController>().MakeSpawnMsg());

            while (IsAlive())
            {
                // 살아있으므로 HP를 깍고
                _data.Character.Hp--;
                Broadcast(new UpdateHpMsg { Id = ObjectId, Hp = _data.Character.Hp, MaxHp = _data.Character.MaxHp, });

                var foods = _world.GetActors<Food>().Where(e => e.IsAlive()).ToList();
                
                // 음식이 없으면 한턴 쉬고
                if (foods.Count() == 0)
                {
                    yield return NextRandom(500, 800);
                    continue;
                }

                var food = foods.OrderBy(e => e.Location.ManhattanDistance(this.Location)).First();

                // 가까운 음식이 거리가 0일 경우 먹는당 'ㅅ'
                // 그게 아니라면 가장 가까운 음식을 향해 이동 ㄱㄱ
                var dist = food.Location.ManhattanDistance(this.Location);
                if (dist <= 15)
                {
                    _data.Character.Hp += 10;
                    food.OnEaten(this);
                }
                else
                {
                    var moveCtrl = Get<MoveController>();
                    var pos = this.FindWay(food.Location);
                    if (moveCtrl.Move(pos.X, pos.Y, Location.ToDirection(pos).ToClientDirection(), 1))
                        Location = pos;
                }
 
                yield return NextRandom(500, 800);
            }
            
            Broadcast(new DespawnMsg { Id = ObjectId });
        }
    }

}
