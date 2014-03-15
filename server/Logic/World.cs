using Server.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Server.Logic
{
    // 게임 월드에 대한 정보를 담당하고 중앙 AI를 담당한다.
    // 필요할 경우 Actor들은 World 객체를 통해 정보를 교환한다.
    class World
    {
        private readonly Coroutine _coro;
        private readonly Persistence _persistence;
        private readonly TiledSharp.TmxMap _map;
        private readonly bool[,] _obstacles;

        public readonly List<Actor> Actors = new List<Actor>();

        public World(Coroutine coro, Persistence persistence)
        {
            _coro = coro;
            _persistence = persistence;
            _map = new TiledSharp.TmxMap(Path.Combine("Data", "default_1.tmx"));

            _obstacles = new bool[_map.Width, _map.Height];
            var obstacleGids = _map.Tilesets.First().Tiles.Where(e => e.Properties.ContainsKey("type") && (e.Properties["type"] == "wall" || e.Properties["type"] == "obstacle"))
                .Select(e => e.Id).ToList();
            var obstacles = _map.Layers.First().Tiles.Where(e => obstacleGids.Contains(e.Gid)).Select(e => Tuple.Create(e.X, e.Y));
            foreach (var tuple in obstacles)
            {
                _obstacles[tuple.Item1, tuple.Item2] = true;
            }
        }

        #region Actor getter

        public IEnumerable<T> GetActors<T>() where T : Actor
        {
            return Actors.OfType<T>();
        }

        public IEnumerable<T> GetActors<T>(Actor exceptActor) where T : Actor
        {
            return GetActors<T>().Where(e => e != exceptActor);
        }

        public IEnumerable<T> GetActors<T>(IEnumerable<T> exceptActors) where T : Actor
        {
            return GetActors<T>().Except(exceptActors);
        }

        #endregion

        public IEnumerable<int> CoroEntry()
        {
            // TODO: 월드의 중앙 AI를 구현한다.

            // 마을을 소환한다.

            // 마을 크기에 따라 NPC를 소환한다.

            // 마을 크기와 NPC의 개체수를 고려하여 자원을 소환한다.

            // 자원의 척박도: blue, yellow, red

            //foreach (var step in Enumerable.Range(1, 10))
            //{
            //    var x = step * 50;
            //    var y = step * 50;
            //    var npcData = new NpcData
            //    {
            //        Character = new CharacterData { Name = "John", MaxHp = 100, Hp = 100, ResourceId = 1 },
            //        Move = new MoveData { WorldId = 1, X = x, Y = y, Dir = 0, Speed = 0 }
            //    };
            //    var roamingPosList = new List<Position> {
            //        new Position{ X = x, Y = y },
            //        new Position{ X = x, Y = y + 50 },
            //        new Position{ X = x + 50, Y = y + 50 },
            //        new Position{ X = x + 50, Y = y },
            //    };

            //    var roamingNpc = new RoamingNpc(this, null, npcData, roamingPosList);
            //    _coro.AddEntry(roamingNpc.CoroEntry);
            //}

            while (true)
            {
                yield return 1000;
            }
        }

        public Coroutine Coro { get { return _coro; } }
        public Persistence Persist { get { return _persistence; } }

        public bool CanMove(int x, int y)
        {
            if (x < 0 || x >= _obstacles.GetLength(0)) return false;
            if (y < 0 || y >= _obstacles.GetLength(1)) return false;
            return !_obstacles[x, y];
        }
    }
}
