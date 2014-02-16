using Server.Core;
using Server.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Logic
{
    class Player : NetworkActor
    {
        public Player(World world, Session session)
            : base(world, session)
        {
        }

        void OnEnterWorld(EnterWorldMsg msg)
        {
            Logger.Write(msg.Name);
        }

    }
}
