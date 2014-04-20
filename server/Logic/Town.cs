using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Logic
{
    class TownData
    {
        public CharacterData Character { get; set; }
        public TownData()
        {
            Character = new CharacterData();
        }
    }

    class Town : Actor
    {
        private List<Actor> nobody = new List<Actor>();

        public Town(World world)
            : base(world)
        {
        }

        protected override IEnumerable<int> CoroMainEntry()
        {
            return base.CoroMainEntry();
        }

        protected override IEnumerable<int> CoroDispose()
        {
            return base.CoroDispose();
        }
    }
}
