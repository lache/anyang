using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Logic
{
    enum PathFinderMethod
    {
        PFM_ASTAR = 100,    // A*
        PFM_BFS = 200,      // BFS
        PFM_DFS = 300,      // DFS
        PFM_DIJKSTRA = 400, // Dijkstra
    }
    
    enum PathWay
    {
        STAY = 0,
        EAST = 100,
        WEST = 200,
        SOUTH = 300,
        NORTH = 400,
    }

    class Position
    {
        public int X = 0;
        public int Y = 0;
    }

    static class PathFinder
    {
        public static PathWay FindWay(this Actor actor, Position dest, PathFinderMethod method = PathFinderMethod.PFM_ASTAR)
        {
            return actor.FindWays(dest, method).First();
        }

        public static IEnumerable<PathWay> FindWays(this Actor actor, Position dest, PathFinderMethod method = PathFinderMethod.PFM_ASTAR)
        {
            yield return PathWay.STAY;
        }
    }
}
