using Server.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Logic
{
    class Player : NetworkActor
    {
        public Player(World world, Coroutine coro, Session session)
            : base(world, coro, session)
        {
        }
    }
}
