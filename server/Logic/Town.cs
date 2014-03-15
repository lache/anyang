using Server.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Logic
{
    class Town
    {
        private readonly Coroutine _coro;

        protected readonly Position _position;
        protected readonly int _radius;
        protected readonly int _npcCapacity;
        protected readonly int _resourceCapacity;

        int _spawnedNpcCount;
        int _remainedResourceCount;

        private Random _random = new Random(Guid.NewGuid().GetHashCode());

        Town(Coroutine coro, Position center, int radius, int npcCapacity, int resourceCapacity)
        {
            _coro = coro;
            _position = center;
            _radius = radius;
            _npcCapacity = npcCapacity;
            _resourceCapacity = resourceCapacity;
        }

        void GenerateNpc(int count = 0)
        {
            _spawnedNpcCount = count;
            if (count == 0)
                _spawnedNpcCount = _random.Next(_npcCapacity / 2, _npcCapacity);

            // spawn npc
            foreach(var npc in Enumerable.Range(0, _spawnedNpcCount))
            {
            }
        }

        void GenerateResource(int count = 0)
        {
            _remainedResourceCount = count;
            if (count == 0)
                _remainedResourceCount = _random.Next(_resourceCapacity / 4, _resourceCapacity / 2);

            // spawn resource
            foreach(var res in Enumerable.Range(0, _remainedResourceCount))
            {
            }
        }
    }
}
