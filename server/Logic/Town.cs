using Server.Message;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Logic
{
    class Town : Npc
    {
        protected NpcData _npcData;
        protected HashSet<Npc> _people = new HashSet<Npc>();

        public Town(World world, NpcData data)
           : base(world, data)
        {
        }

        protected override IEnumerable<int> CoroMainEntry()
        {
            Broadcast(Get<CharacterController>().MakeSpawnMsg());
            
            while (true)
            {
                GenerateHungryNpc();

                yield return NextRandom(4000, 5000);
            }
            
            Broadcast(new DespawnMsg { Id = ObjectId });
        }

        protected override IEnumerable<int> CoroDispose()
        {
            return base.CoroDispose();
        }

        private void GenerateHungryNpc()
        {
            var npcData = new NpcData
            {
                Character = new CharacterData
                {
                    Hp = 30,
                    MaxHp = 100,
                    ResourceId = 3,
                    Color = Convert.ToInt32(Color.AliceBlue.ToArgb()),
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
    }
}
