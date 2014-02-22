using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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

    class Position : IComparable<Position>, IEquatable<Position>
    {
        public int X = 0;
        public int Y = 0;

        public int CompareTo(Position pos)
        {
            return X != pos.X ? X - pos.X : Y - pos.Y;
        }

        public override int GetHashCode()
        {
            return (int)(Math.Sqrt(int.MaxValue) * X + Y);
        }

        public Position Clone()
        {
            return new Position {X = this.X, Y = this.Y};
        }

        public static bool operator == (Position pos1, Position pos2)
        {
            return pos1.X == pos2.X && pos1.Y == pos2.Y;
        }
        
        public static bool operator != (Position pos1, Position pos2)
        {
            return !(pos1 == pos2);
        }

        public bool Equals(Position pos)
        {
            return X == pos.X && Y == pos.Y;
        }

        public Position MoveToWay(PathWay way)
        {
            var newLoc = Clone();
            switch (way)
            {
                case PathWay.EAST:
                    newLoc.X++;
                    break;
                case PathWay.WEST:
                    newLoc.X--;
                    break;
                case PathWay.SOUTH:
                    newLoc.Y--;
                    break;
                case PathWay.NORTH:
                    newLoc.Y++;
                    break;
            }
            return newLoc;
        }

        public IEnumerable<Position> PossibleMoves(bool exceptStay = false)
        {
            foreach (var way in (PathWay[]) Enum.GetValues(typeof (PathWay)))
            {
                if(exceptStay && way == PathWay.STAY)
                    continue;

                yield return MoveToWay(way);
            }
        }

        public override string ToString()
        {
            return string.Format("X: {0}, Y: {1}", X, Y);
        }
    }

    static class PathHelper
    {
        public static int ManhattanDistance(this Position here, Position there)
        {
            return Math.Abs(here.X - there.X) + Math.Abs(here.Y - there.Y);
        }

        public static PathWay ToDirection(this Position here, Position there)
        {
            var dx = here.X - there.X;
            var dy = here.Y - there.Y;

            if (dx == 1 && dy == 0)
                return PathWay.EAST;
            if (dx == -1 && dy == 0)
                return PathWay.WEST;
            if (dx == 0 && dy == -1)
                return PathWay.SOUTH;
            if (dx == 0 && dy == 1)
                return PathWay.NORTH;

            return PathWay.STAY;
        }

        public static double ToClientDirection(this PathWay way)
        {
            switch (way)
            {
                case PathWay.NORTH:
                    return 0.0;
                case PathWay.EAST:
                    return 90.0;
                case PathWay.SOUTH:
                    return 180.0;
                case PathWay.WEST:
                    return 270.0;
            }
            return 0.0;
        }
    }

    static class PathFinder
    {
        public static Position FindWay(this Actor actor, Position dest, PathFinderMethod method = PathFinderMethod.PFM_ASTAR)
        {
            return actor.FindWays(dest, method).First();
        }

        public static IEnumerable<Position> FindWays(this Actor actor, Position dest, PathFinderMethod method = PathFinderMethod.PFM_ASTAR)
        {
            // currently, just A* is implemented
            switch(method)
            {
                case PathFinderMethod.PFM_ASTAR:

                    var closedSet = new HashSet<Position>();
                    var openSet = new HashSet<Position>();
                    var navigated = new Dictionary<Position, Position>();

                    var pathScore = new Dictionary<Position, int>();
                    var heuriScore = new SortedDictionary<Position, int>();

                    var actorLoc = actor.Location.Clone();
                    openSet.Add(actorLoc);
                    pathScore.Add(actorLoc, 0);
                    heuriScore.Add(actorLoc, dest.ManhattanDistance(actorLoc));
                    while (openSet.Count > 0)
                    {
                        var current = heuriScore.First();
                        if (current.Key == dest)
                        {
                            // construct way to dest
                            var wayStack = new Stack<Position>();
                            var curPos = dest;
                            while (navigated.ContainsKey(curPos))
                            {
                                wayStack.Push(curPos);
                                curPos = navigated[curPos];
                            }

                            while (wayStack.Count > 0)
                                yield return wayStack.Pop();
                            yield break;
                        }

                        openSet.Remove(current.Key);
                        heuriScore.Remove(current.Key);
                        closedSet.Add(current.Key);
                        foreach (var newLoc in current.Key.PossibleMoves(true))
                        {
                            if (closedSet.Contains(newLoc) || (!actor.CanMove(newLoc)))
                                continue;

                            var trueScore = pathScore[current.Key] + 1;
                            if ((!openSet.Contains(newLoc)) || (trueScore < heuriScore[newLoc]))
                            {
                                navigated.Remove(newLoc);
                                navigated.Add(newLoc, current.Key);

                                pathScore.Remove(newLoc);
                                pathScore.Add(newLoc, trueScore);

                                heuriScore.Remove(newLoc);
                                heuriScore.Add(newLoc, pathScore[newLoc] + dest.ManhattanDistance(newLoc));

                                openSet.Add(newLoc);
                            }
                        }
                    }

                    break;

                case PathFinderMethod.PFM_BFS:
                case PathFinderMethod.PFM_DFS:
                case PathFinderMethod.PFM_DIJKSTRA:

                    break;
            }
            yield return actor.Location.Clone();
        }
    }
}
