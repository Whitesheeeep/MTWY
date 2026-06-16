using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameData;
using UnityEngine;
using WS_Modules.DataStructure;

namespace Pathfinding
{
    /// <summary>
    /// A* pathfinding service for the currently loaded map grid.
    /// </summary>
    public static class MapPathfindingService
    {
        private const int StraightCost = 10;
        private const int DiagonalCost = 14;

        private static readonly Vector2Int[] NeighborOffsets =
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 1),
            new Vector2Int(1, 0),
            new Vector2Int(1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(-1, 1)
        };

        /// <summary>
        /// Finds an eight-direction path between two grid cells on the current map.
        /// </summary>
        public static bool TryFindPath(Vector3Int startCell, Vector3Int targetCell, List<Vector3Int> pathCells)
        {
            if (!TryGetCurrentMapGrid(out MapGridManager mapGrid, out string mapId))
            {
                if (pathCells == null)
                {
                    throw new ArgumentNullException(nameof(pathCells));
                }

                pathCells.Clear();
                return false;
            }

            return TryFindLoadedPath(mapId, startCell, targetCell, pathCells);
        }

        /// <summary>
        /// Ensures a map is cached, then finds an eight-direction path between two grid cells on that map.
        /// </summary>
        public static async UniTask<bool> TryFindPathAsync(
            string mapId,
            Vector3Int startCell,
            Vector3Int targetCell,
            List<Vector3Int> pathCells)
        {
            if (pathCells == null)
            {
                throw new ArgumentNullException(nameof(pathCells));
            }

            pathCells.Clear();

            MapGridManager mapGrid = MapGridManager.Instance;
            if (!await mapGrid.EnsureLoadedAsync(mapId))
            {
                return false;
            }

            return TryFindLoadedPath(mapId, startCell, targetCell, pathCells);
        }

        /// <summary>
        /// Finds an eight-direction path on a map that is already loaded in MapGridDatabase.
        /// </summary>
        public static bool TryFindLoadedPath(
            string mapId,
            Vector3Int startCell,
            Vector3Int targetCell,
            List<Vector3Int> pathCells)
        {
            if (pathCells == null)
            {
                throw new ArgumentNullException(nameof(pathCells));
            }

            pathCells.Clear();

            if (string.IsNullOrWhiteSpace(mapId))
            {
                return false;
            }

            MapGridManager mapGrid = MapGridManager.Instance;
            if (!mapGrid.IsLoaded(mapId) ||
                !mapGrid.IsWalkable(mapId, startCell) ||
                !mapGrid.IsWalkable(mapId, targetCell))
            {
                return false;
            }

            if (startCell == targetCell)
            {
                pathCells.Add(startCell);
                return true;
            }

            BinaryHeap<PathfindingHeapNode> openSet = new BinaryHeap<PathfindingHeapNode>(PathfindingHeapNodeComparer.Instance);
            HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();
            Dictionary<Vector3Int, int> gScore = new Dictionary<Vector3Int, int>();
            Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();

            int sequence = 0;
            int startHeuristic = GetHeuristicCost(startCell, targetCell);
            gScore[startCell] = 0;
            openSet.Push(new PathfindingHeapNode(startCell, startHeuristic, startHeuristic, sequence++));

            while (openSet.Count > 0)
            {
                Vector3Int current = openSet.Pop().Cell;
                if (closedSet.Contains(current))
                {
                    continue;
                }

                if (current == targetCell)
                {
                    BuildPath(cameFrom, current, pathCells);
                    return true;
                }

                closedSet.Add(current);
                int currentGScore = gScore[current];

                foreach (Vector3Int neighbor in GetWalkableNeighbors(mapGrid, mapId, current))
                {
                    if (closedSet.Contains(neighbor))
                    {
                        continue;
                    }

                    int tentativeGScore = currentGScore + GetMoveCost(current, neighbor);
                    if (gScore.TryGetValue(neighbor, out int knownGScore) && tentativeGScore >= knownGScore)
                    {
                        continue;
                    }

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;

                    int hCost = GetHeuristicCost(neighbor, targetCell);
                    int fCost = tentativeGScore + hCost;
                    openSet.Push(new PathfindingHeapNode(neighbor, fCost, hCost, sequence++));
                }
            }

            pathCells.Clear();
            return false;
        }

        /// <summary>
        /// Finds an eight-direction path between two world positions on the current map.
        /// </summary>
        public static bool TryFindWorldPath(Vector3 startWorld, Vector3 targetWorld, List<Vector3> worldPath)
        {
            if (worldPath == null)
            {
                throw new ArgumentNullException(nameof(worldPath));
            }

            worldPath.Clear();

            if (!TryGetMapGrid(out MapGridManager mapGrid))
            {
                return false;
            }

            List<Vector3Int> pathCells = new List<Vector3Int>();
            Vector3Int startCell = mapGrid.WorldToCell(startWorld);
            Vector3Int targetCell = mapGrid.WorldToCell(targetWorld);
            if (!TryFindPath(startCell, targetCell, pathCells))
            {
                return false;
            }

            foreach (Vector3Int cell in pathCells)
            {
                worldPath.Add(mapGrid.GetCellCenterWorld(cell));
            }

            return true;
        }

        private static bool TryGetCurrentMapGrid(out MapGridManager mapGrid, out string mapId)
        {
            mapGrid = MapGridManager.Instance;
            mapId = mapGrid.CurrentMapId;
            return mapGrid.CurrentMapData != null && !string.IsNullOrWhiteSpace(mapId);
        }

        private static bool TryGetMapGrid(out MapGridManager mapGrid)
        {
            mapGrid = MapGridManager.Instance;
            return mapGrid.CurrentMapData != null && mapGrid.HasCurrentGrid;
        }

        private static IEnumerable<Vector3Int> GetWalkableNeighbors(MapGridManager mapGrid, string mapId, Vector3Int current)
        {
            foreach (Vector2Int offset in NeighborOffsets)
            {
                Vector3Int neighbor = new Vector3Int(current.x + offset.x, current.y + offset.y, current.z);
                if (!mapGrid.IsWalkable(mapId, neighbor))
                {
                    continue;
                }

                if (IsDiagonal(offset) && !CanMoveDiagonally(mapGrid, mapId, current, offset))
                {
                    continue;
                }

                yield return neighbor;
            }
        }

        private static bool CanMoveDiagonally(MapGridManager mapGrid, string mapId, Vector3Int current, Vector2Int offset)
        {
            Vector3Int horizontal = new Vector3Int(current.x + offset.x, current.y, current.z);
            Vector3Int vertical = new Vector3Int(current.x, current.y + offset.y, current.z);
            return mapGrid.IsWalkable(mapId, horizontal) && mapGrid.IsWalkable(mapId, vertical);
        }

        private static bool IsDiagonal(Vector2Int offset)
        {
            return offset.x != 0 && offset.y != 0;
        }

        private static int GetMoveCost(Vector3Int from, Vector3Int to)
        {
            return from.x != to.x && from.y != to.y ? DiagonalCost : StraightCost;
        }

        private static int GetHeuristicCost(Vector3Int from, Vector3Int to)
        {
            int dx = Mathf.Abs(from.x - to.x);
            int dy = Mathf.Abs(from.y - to.y);
            int diagonalSteps = Mathf.Min(dx, dy);
            int straightSteps = Mathf.Abs(dx - dy);
            return DiagonalCost * diagonalSteps + StraightCost * straightSteps;
        }

        private static void BuildPath(
            Dictionary<Vector3Int, Vector3Int> cameFrom,
            Vector3Int targetCell,
            List<Vector3Int> pathCells)
        {
            pathCells.Clear();
            Vector3Int current = targetCell;
            pathCells.Add(current);

            while (cameFrom.TryGetValue(current, out Vector3Int previous))
            {
                current = previous;
                pathCells.Add(current);
            }

            pathCells.Reverse();
        }
    }
}
