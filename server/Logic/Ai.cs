using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Logic
{
    enum ActionSet
    {
        AS_NONE = 1000, // do nothing
        AS_MOVE = 2000, // move to anywhere
        AS_CONSTRUCT = 3000, // construct something
        AS_TUNE_TAX_RATIO = 4000, // tune tax ratio
    }

    interface ActionParam
    {
    }

    class ActionMoveParam : ActionParam
    {
        public PathWay Direction;
        public Position Destination;
    }

    class Command
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
            // 아래의 코드는 테스트 코드
            var dest = new Position { X = 0, Y = 0 };
            return new Command {
                Action = ActionSet.AS_MOVE,
                Params = new ActionMoveParam {
                    Direction = _actor.FindWay(dest),
                    Destination = dest,
                },
            };
        }

    }
}
