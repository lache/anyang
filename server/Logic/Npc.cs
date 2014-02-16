using Server.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Logic
{
    class Npc : Actor
    {
        protected Ai _ai;

        public Npc(World world, Coroutine coro, Ai ai)
            : base(world, coro)
        {
            _ai = ai;
        }

        public override IEnumerable<int> CoroEntry()
        {
            return base.CoroEntry();
        }

        public override IEnumerable<int> CoroDispose()
        {
            return base.CoroDispose();
        }
    }
}
