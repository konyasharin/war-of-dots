using System.Collections.Generic;
using UnityEngine;

namespace DotWars.Map
{
    public static class Pathfinding
    {
        private static readonly Vector2Int[] Directions =
        {
            new(0, 1), new(0, -1), new(1, 0), new(-1, 0),
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
        };

        public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, bool isInfantry, bool isShip = false)
        {
            var map = MapManager.Instance;
            if (map == null) return null;

            if (!CanTraverse(map, end, isInfantry, isShip))
                return null;

            var openSet = new SortedSet<Node>(new NodeComparer());
            var closedSet = new HashSet<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float>();

            gScore[start] = 0;
            openSet.Add(new Node(start, 0, Heuristic(start, end)));

            while (openSet.Count > 0)
            {
                var current = openSet.Min;
                openSet.Remove(current);

                if (current.Position == end)
                    return ReconstructPath(cameFrom, end);

                closedSet.Add(current.Position);

                foreach (var dir in Directions)
                {
                    var neighbor = current.Position + dir;

                    if (!map.InBounds(neighbor) || closedSet.Contains(neighbor))
                        continue;

                    if (!CanTraverse(map, neighbor, isInfantry, isShip))
                        continue;

                    var terrain = map.GetTerrainAt(neighbor);
                    float moveCost = dir.x != 0 && dir.y != 0 ? 1.414f : 1f;

                    if (!isShip)
                    {
                        float speedMod = terrain != null ? terrain.GetSpeedModifier(isInfantry) : 1f;
                        if (speedMod <= 0f) continue;
                        moveCost /= speedMod;
                    }

                    float tentativeG = gScore[current.Position] + moveCost;

                    if (gScore.TryGetValue(neighbor, out float existingG) && tentativeG >= existingG)
                        continue;

                    cameFrom[neighbor] = current.Position;
                    gScore[neighbor] = tentativeG;

                    openSet.Add(new Node(neighbor, tentativeG, tentativeG + Heuristic(neighbor, end)));
                }
            }

            return null;
        }

        private static bool CanTraverse(MapManager map, Vector2Int pos, bool isInfantry, bool isShip)
        {
            var terrain = map.GetTerrainAt(pos);
            if (terrain == null) return false;

            if (isShip)
            {
                if (terrain.terrainType == TerrainType.Water || terrain.terrainType == TerrainType.Port)
                    return true;
                // Allow coastal land tiles for beaching
                if (terrain.isPassable && HasAdjacentWater(map, pos))
                    return true;
                return false;
            }
            else
            {
                return terrain.isPassable;
            }
        }

        private static bool HasAdjacentWater(MapManager map, Vector2Int pos)
        {
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var d in dirs)
            {
                var neighbor = pos + d;
                var t = map.GetTerrainAt(neighbor);
                if (t != null && t.terrainType == TerrainType.Water)
                    return true;
            }
            return false;
        }

        private static float Heuristic(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            return Mathf.Max(dx, dy) + (1.414f - 1f) * Mathf.Min(dx, dy);
        }

        private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Add(current);
            }
            path.Reverse();
            return path;
        }

        private readonly struct Node
        {
            public readonly Vector2Int Position;
            public readonly float GScore;
            public readonly float FScore;

            public Node(Vector2Int position, float gScore, float fScore)
            {
                Position = position;
                GScore = gScore;
                FScore = fScore;
            }
        }

        private class NodeComparer : IComparer<Node>
        {
            public int Compare(Node a, Node b)
            {
                int result = a.FScore.CompareTo(b.FScore);
                if (result == 0) result = a.GScore.CompareTo(b.GScore);
                if (result == 0) result = a.Position.x.CompareTo(b.Position.x);
                if (result == 0) result = a.Position.y.CompareTo(b.Position.y);
                return result;
            }
        }
    }
}
