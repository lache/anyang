using System;
using System.Collections.Generic;
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
    }
}
