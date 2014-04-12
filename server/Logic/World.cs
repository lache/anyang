using Server.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TiledSharp;

namespace Server.Logic
{
    // 다중 월드를 관리하는 객체
    class Worlds
    {
        private static readonly Dictionary<int, World> WorldMap = new Dictionary<int, World>();

        public static void Initialize(Persistence persistence)
        {
            // 각 대륙에 대한 World을 만들어준다.
            foreach (var world in Directory.GetFiles("Data", "*.tmx").Select(e => new World(persistence, e)))
            {
                WorldMap.Add(world.Id, world);
            }

            foreach (var world in WorldMap.Values)
                world.Start();
        }

        public static World Get(int worldId)
        {
            World world;
            return WorldMap.TryGetValue(worldId, out world) ? world : null;
        }
    }

    // 게임 월드에 대한 정보를 담당하고 중앙 AI를 담당한다.
    // 필요할 경우 Actor들은 World 객체를 통해 정보를 교환한다.
    class World
    {
        private readonly Coroutine _coro;
        private readonly Persistence _persistence;
        private readonly bool[,] _obstacles;

        public readonly List<Actor> Actors = new List<Actor>();

        public World(Persistence persistence, string mapFile)
        {
            _coro = new Coroutine();
            _coro.AddEntry(CoroEntry);

            _persistence = persistence;
            var map = new TmxMap(mapFile);
            Id = int.Parse(map.Properties["world-id"]);

            _obstacles = new bool[map.Width, map.Height];
            var obstacleGids = map.Tilesets.First().Tiles.Where(e => e.Properties.ContainsKey("type") && (e.Properties["type"] == "wall" || e.Properties["type"] == "obstacle"))
                .Select(e => e.Id).ToList();
            var obstacles = map.Layers.First().Tiles.Where(e => obstacleGids.Contains(e.Gid)).Select(e => Tuple.Create(e.X, e.Y));
            foreach (var tuple in obstacles)
            {
                _obstacles[tuple.Item1, tuple.Item2] = true;
            }
        }

        public int Id { get; private set; }

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

        public void Start()
        {
            _coro.Start();
        }

        private IEnumerable<int> CoroEntry()
        {
            // TODO: MotherOfEarth, InvisibleHands, 및 TownMaker 를 소환!

            // MotherOfEarth 소환
            var moe = new MotherOfEarth(this, new NpcData { Character = new CharacterData { Name = "MotherOfEarth" }, });
            Actors.Add(moe);
            _coro.AddEntry(moe.CoroEntry);

            // InvisibleHands 소환
            var invHands = new InvisibleHands(this, new NpcData { Character = new CharacterData { Name = "InvisibleHands" }, });
            Actors.Add(invHands);
            _coro.AddEntry(invHands.CoroEntry);

            // TownMaker 소환
            var townMaker = new TownMaker(this, new NpcData { Character = new CharacterData { Name = "TownMaker" }, });
            Actors.Add(townMaker);
            _coro.AddEntry(townMaker.CoroEntry);

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
