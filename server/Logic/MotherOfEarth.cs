using Server.Message;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Logic
{
    class Food : Npc
    {
        private static int FoodCount = 0;

        private HashSet<Npc> _huntingSet = new HashSet<Npc>();

        public Food(World world, NpcData data)
            : base(world, data)
        {
        }

        protected override IEnumerable<int> CoroMainEntry()
        {
            Broadcast(Get<CharacterController>().MakeSpawnMsg());
            
            Interlocked.Increment(ref FoodCount);

            var charData = _data.Character;
            charData.Hp = Math.Max(10, 50 - (int)Math.Log10(FoodCount));
            while(IsAlive())
            {
                charData.Hp--;
                Broadcast(new UpdateHpMsg { Id = ObjectId, Hp = _data.Character.Hp, MaxHp = _data.Character.MaxHp, });
                
                yield return NextRandom(700, 1200);
            }

            // 누구를 도와줄지 결정해야 한다
            // _huntingSet.OrderBy(_huntingSet.OrderBy(e => e....)
            
            Broadcast(new DespawnMsg { Id = ObjectId });

            Interlocked.Decrement(ref FoodCount);
        }

        public void OnEaten(Npc npc)
        {
            _huntingSet.Add(npc);
        }
    }

    // 게임 전체의 자원과 환경을 관리
    class MotherOfEarth : Npc
    {
        private static int TownCount = 0;
        
        public MotherOfEarth(World world, NpcData data)
            : base(world, data)
        {
        }

        private void GenerateFood()
        {
            var npcData = new NpcData
            {
                Character = new CharacterData
                {
                    Hp = 100,
                    MaxHp = 100,
                    ResourceId = 1,
                    Color = Convert.ToInt32(Color.BlueViolet.ToArgb()),
                },
                Move = new MoveData
                {
                    X = NextRandom(10, 1000),
                    Y = NextRandom(10, 1000),
                    Dir = 0,
                    Speed = 0,
                },
            };
            var newFood = new Food(_world, npcData);
            npcData.Character.Name = "Food " + newFood.ObjectId;
            _world.Coro.AddEntry(newFood.CoroEntry);
        }

        private void GenerateTown()
        {
            var npcData = new NpcData
            {
                Character = new CharacterData
                {
                    Hp = 10000,
                    MaxHp = 10000,
                    ResourceId = 3,
                    Radius = 300,
                    Color = Convert.ToInt32(
                        Color.FromArgb(
                            NextRandom(0, 255),
                            NextRandom(0, 255),
                            NextRandom(0, 255),
                            NextRandom(0, 255))
                            .ToArgb()),
                },
                Move = new MoveData
                {
                    X = NextRandom(10, 1000),
                    Y = NextRandom(10, 1000),
                    Dir = 0,
                    Speed = 0,
                },
                GroupId = TownCount++,
            };
            var newTown = new Town(_world, npcData);
            npcData.Character.Name = "Town " + newTown.ObjectId;
            _world.Coro.AddEntry(newTown.CoroEntry);
        }

        protected override IEnumerable<int> CoroMainEntry()
        {
            while(true)
            {
                GenerateFood();
               
                if(TownCount < 10)
                {
                    Interlocked.Increment(ref TownCount);
                    GenerateTown();
                }

                yield return NextRandom(1000, 5000);
            }
        }
    }
}
