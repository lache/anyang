﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Logic
{
    // 게임 내부의 모든 경제적 요소에 대한 컨트롤
    class InvisibleHands : Npc
    {
        public InvisibleHands(World world, NpcData data)
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
