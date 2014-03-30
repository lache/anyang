using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Logic
{
    class Food : Npc
    {
        public Food(World world, NpcData data)
            : base(world, data)
        {
        }

        protected override IEnumerable<int> CoroMainEntry()
        {
            while(true)
            {
                yield return NextRandom(500, 1000);
            }
        }
    }

    // 게임 전체의 자원과 환경을 관리
    class MotherOfEarth : Npc
    {
        public int nextx = 0;
        public int nexty = 0;
        public MotherOfEarth(World world, NpcData data)
            : base(world, data)
        {
        }

        protected override IEnumerable<int> CoroMainEntry()
        {
            while(true)
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
                        X = nextx,
                        Y = nexty,
                        Dir = 0,
                        Speed = 0,
                    },
                };
                nextx += 10;
                nexty += 10;
                var newFood = new Food(_world, npcData);
                npcData.Character.Name = "Food " + newFood.ObjectId;
                _world.Coro.AddEntry(newFood.CoroEntry);

                yield return NextRandom(100, 300);
            }
        }
    }
}
