using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Logic
{
    public enum ActionSet
    {
        AS_NONE = 1000, // do nothing
        AS_MOVE = 2000, // move to anywhere
        AS_CONSTRUCT = 3000, // construct something
        AS_TUNE_TAX_RATIO = 4000, // tune tax ratio
    }

    public interface ActionParam
    {
    }

    public class Command
    {
        public ActionSet Action;
        public ActionParam Params;
    }

    class Ai
    {
        protected readonly World _world;
        Actor _actor;

        Ai(World world, Actor actor)
        {
            _world = world;
            _actor = actor;
        }

        public Command GetAction()
        {
            return new Command { Action = ActionSet.AS_NONE };
        }

    }
}
