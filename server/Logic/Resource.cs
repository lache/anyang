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

    class Resource
    {
        private static int ResourceIssued = 9999999;

    }
}
