using Server.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Logic
{
    // 게임 월드에 대한 정보를 담당하고 중앙 AI를 담당한다.
    // 필요할 경우 Actor들은 World 객체를 통해 정보를 교환한다.
    class World
    {
        private readonly Coroutine _coro;

        public World(Coroutine coro)
        {
            _coro = coro;
        }

        public IEnumerable<int> CoroEntry()
        {
            // TODO: 월드의 중앙 AI를 구현한다.
            while (true)
            {
                yield return 1000;
            }
        }
    }
}
