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
        Random _random = new Random(DateTime.Now.Millisecond);

        Ai(World world, Actor actor)
        {
            _world = world;
            _actor = actor;
        }

        private Command GetAction()
        {
            return new Command { Action = ActionSet.AS_NONE };
        }

        public IEnumerable<int> CoroAiEntry()
        {
            while (_actor.IsAlive())
            {
                // 정보를 모으고 - 현재 어떤 정보가 필요한지 확인 불가

                // 판단을 한 다음 - QLearning을 한번 적용해볼까?

                // 액션을 취한다 - 여튼 명령을 내려보자
                _actor.DoItNow(GetAction());

                yield return _random.Next(1000, 5000);
            }

            yield break;
        }
    }
}
