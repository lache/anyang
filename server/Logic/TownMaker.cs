using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Logic
{
    class TownMaker : Npc
    {
        public TownMaker(World world, NpcData data)
            : base(world, data)
        {
        }

        protected override IEnumerable<int> CoroMainEntry()
        {
            while (true)
            {
                yield return NextRandom(1000, 3000);
            }
        }
    }
}
