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
        private int FoodCount = 0;
        private int LifeTime = 10;

        public Food(World world, NpcData data)
            : base(world, data)
        {
        }

        protected override IEnumerable<int> CoroMainEntry()
        {
            Interlocked.Increment(ref FoodCount);

            //LifeTime = 50 - (int)Math.Log10(FoodCount);
            //if (LifeTime <= 0) LifeTime = 2;
            while(IsAlive())
            {
                //LifeTime--;
                //if (LifeTime == 0) break;

                yield return NextRandom(1000, 2000);
            }
            Broadcast(new DespawnMsg { Id = ObjectId });

            Interlocked.Decrement(ref FoodCount);
        }

        public void OnEaten(Npc npc)
        {
            _data.Character.Hp = 0;
        }
    }

    // 게임 전체의 자원과 환경을 관리
    class MotherOfEarth : Npc
    {
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
                    Hp = 1,
                    MaxHp = 1,
                    ResourceId = Convert.ToInt32(Color.BlueViolet.ToArgb()),
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

        private void GenerateHungryNpc()
        {
            //if (NextRandom(0, 2) == 0) return;

            var npcData = new NpcData
            {
                Character = new CharacterData
                {
                    Hp = 30,
                    MaxHp = 100,
                    ResourceId = Convert.ToInt32(Color.AliceBlue.ToArgb()),
                },
                Move = new MoveData
                {
                    X = NextRandom(100, 1000),
                    Y = NextRandom(100, 1000),
                    Dir = 0,
                    Speed = 0,
                },
            };
            var hungryNpc = new HungryNpc(_world, npcData);
            npcData.Character.Name = "Hungry Npc" + hungryNpc.ObjectId;
            _world.Coro.AddEntry(hungryNpc.CoroEntry);
        }

        protected override IEnumerable<int> CoroMainEntry()
        {
            while(true)
            {
                GenerateFood();
                GenerateHungryNpc();

                yield return NextRandom(1000, 5000);
            }
        }
    }
}
