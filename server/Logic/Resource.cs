using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Logic
{
    class ResourceData
    {
        public CharacterData Character { get; set; }
        public ResourceData()
        {
            Character = new CharacterData();
        }
    }

    class Food : Actor
    {
        private static int ResourceIssued = 9999999;
        protected ResourceData _data;

        public Food(World world, ResourceData data)
            : base(world)
        {
            _data = data;
            Add<CharacterController>(_data.Character);
        }
    }
}
