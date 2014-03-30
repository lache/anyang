using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Logic
{
    // 게임 전체의 자원과 환경을 관리
    class MotherOfEarth : Npc
    {
        public MotherOfEarth(World world, NpcData data)
            : base(world, data)
        {
        }

        protected override IEnumerable<int> CoroMainEntry()
        {
            while(true)
            {
                yield return NextRandom(1000, 3000);
            }
        }
    }
}
