#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using GameData;
using UnityEngine;
using WS_Modules.DataStructure;

namespace Pathfinding
{
    /// <summary>
    /// Editor-only A* debug service that records search snapshots for visualization.
    /// Remove this file together with tester process UI when search-process debugging is no longer needed.
    /// </summary>
    public static class MapPathfindingDebugService
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
        /// Finds an eight-direction path and records A* search snapshots for editor visualization.
        /// </summary>
        public static bool TryFindPathWithDebug(
            Vector3Int startCell,
            Vector3Int targetCell,
            List<Vector3Int> pathCells,
            MapPathfindingDebugResult debugResult)
        {
            if (pathCells == null)
            {
                throw new ArgumentNullException(nameof(pathCells));
            }

            if (debugResult == null)
            {
                throw new ArgumentNullException(nameof(debugResult));
            }

            pathCells.Clear();
            debugResult.Clear(startCell, targetCell);

            if (!TryGetMapGrid(out IMapGridDatabase mapGrid))
            {
                debugResult.Complete(false, "Current map data or Grid is not loaded.");
                return false;
            }

            if (!mapGrid.IsWalkable(startCell) || !mapGrid.IsWalkable(targetCell))
            {
                debugResult.Complete(false, "Start or target cell is not walkable.");
                return false;
            }

            if (startCell == targetCell)
            {
                pathCells.Add(startCell);
                debugResult.RecordStep(
                    "Start equals target.",
                    startCell,
                    startCell,
                    null,
                    null,
                    null,
                    null,
                    null,
                    pathCells);
                debugResult.Complete(true, string.Empty);
                return true;
            }

            BinaryHeap<PathfindingHeapNode> openSet = new BinaryHeap<PathfindingHeapNode>(PathfindingHeapNodeComparer.Instance);
            HashSet<Vector3Int> openCells = new HashSet<Vector3Int>();
            HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();
            Dictionary<Vector3Int, int> gScore = new Dictionary<Vector3Int, int>();
            Dictionary<Vector3Int, int> hScore = new Dictionary<Vector3Int, int>();
            Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();

            int sequence = 0;
            int startHeuristic = GetHeuristicCost(startCell, targetCell);
            gScore[startCell] = 0;
            hScore[startCell] = startHeuristic;
            openSet.Push(new PathfindingHeapNode(startCell, startHeuristic, startHeuristic, sequence++));
            openCells.Add(startCell);
            debugResult.RecordStep(
                "Initialize open set.",
                startCell,
                startCell,
                openCells,
                closedSet,
                gScore,
                hScore,
                cameFrom,
                null);

            while (openSet.Count > 0)
            {
                Vector3Int current = openSet.Pop().Cell;
                if (closedSet.Contains(current))
                {
                    continue;
                }

                openCells.Remove(current);
                debugResult.RecordStep(
                    "Select current node.",
                    current,
                    targetCell,
                    openCells,
                    closedSet,
                    gScore,
                    hScore,
                    cameFrom,
                    null);

                if (current == targetCell)
                {
                    BuildPath(cameFrom, current, pathCells);
                    debugResult.RecordStep(
                        "Target reached.",
                        current,
                        targetCell,
                        openCells,
                        closedSet,
                        gScore,
                        hScore,
                        cameFrom,
                        pathCells);
                    debugResult.Complete(true, string.Empty);
                    return true;
                }

                closedSet.Add(current);
                int currentGScore = gScore[current];
                debugResult.RecordStep(
                    "Close current node.",
                    current,
                    targetCell,
                    openCells,
                    closedSet,
                    gScore,
                    hScore,
                    cameFrom,
                    null);

                foreach (Vector3Int neighbor in GetWalkableNeighbors(mapGrid, current))
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
                    hScore[neighbor] = hCost;
                    openSet.Push(new PathfindingHeapNode(neighbor, fCost, hCost, sequence++));
                    openCells.Add(neighbor);
                    debugResult.RecordStep(
                        "Open or improve neighbor.",
                        neighbor,
                        targetCell,
                        openCells,
                        closedSet,
                        gScore,
                        hScore,
                        cameFrom,
                        null);
                }
            }

            pathCells.Clear();
            debugResult.RecordStep(
                "Open set exhausted.",
                targetCell,
                targetCell,
                openCells,
                closedSet,
                gScore,
                hScore,
                cameFrom,
                null);
            debugResult.Complete(false, "Target unreachable.");
            return false;
        }

        /// <summary>
        /// Finds an eight-direction world path and records A* search snapshots for editor visualization.
        /// </summary>
        public static bool TryFindWorldPathWithDebug(
            Vector3 startWorld,
            Vector3 targetWorld,
            List<Vector3> worldPath,
            MapPathfindingDebugResult debugResult)
        {
            if (worldPath == null)
            {
                throw new ArgumentNullException(nameof(worldPath));
            }

            if (debugResult == null)
            {
                throw new ArgumentNullException(nameof(debugResult));
            }

            worldPath.Clear();

            if (!TryGetMapGrid(out IMapGridDatabase mapGrid))
            {
                return false;
            }

            List<Vector3Int> pathCells = new List<Vector3Int>();
            Vector3Int startCell = mapGrid.WorldToCell(startWorld);
            Vector3Int targetCell = mapGrid.WorldToCell(targetWorld);
            if (!TryFindPathWithDebug(startCell, targetCell, pathCells, debugResult))
            {
                return false;
            }

            foreach (Vector3Int cell in pathCells)
            {
                worldPath.Add(mapGrid.GetCellCenterWorld(cell));
            }

            return true;
        }

        private static bool TryGetMapGrid(out IMapGridDatabase mapGrid)
        {
            if (!GameDatabase.TryGet(out mapGrid))
            {
                return false;
            }

            return mapGrid.CurrentMapData != null && mapGrid.HasCurrentGrid;
        }

        private static IEnumerable<Vector3Int> GetWalkableNeighbors(IMapGridDatabase mapGrid, Vector3Int current)
        {
            foreach (Vector2Int offset in NeighborOffsets)
            {
                Vector3Int neighbor = new Vector3Int(current.x + offset.x, current.y + offset.y, current.z);
                if (!mapGrid.IsWalkable(neighbor))
                {
                    continue;
                }

                if (IsDiagonal(offset) && !CanMoveDiagonally(mapGrid, current, offset))
                {
                    continue;
                }

                yield return neighbor;
            }
        }

        private static bool CanMoveDiagonally(IMapGridDatabase mapGrid, Vector3Int current, Vector2Int offset)
        {
            Vector3Int horizontal = new Vector3Int(current.x + offset.x, current.y, current.z);
            Vector3Int vertical = new Vector3Int(current.x, current.y + offset.y, current.z);
            return mapGrid.IsWalkable(horizontal) && mapGrid.IsWalkable(vertical);
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

    /// <summary>
    /// A* search snapshots captured for tester visualization.
    /// </summary>
    public sealed class MapPathfindingDebugResult
    {
        private readonly List<MapPathfindingDebugStep> steps = new List<MapPathfindingDebugStep>();

        public IReadOnlyList<MapPathfindingDebugStep> Steps => steps;
        public bool Succeeded { get; private set; }
        public string FailureReason { get; private set; } = string.Empty;
        public Vector3Int StartCell { get; private set; }
        public Vector3Int TargetCell { get; private set; }

        internal void Clear(Vector3Int startCell, Vector3Int targetCell)
        {
            steps.Clear();
            Succeeded = false;
            FailureReason = string.Empty;
            StartCell = startCell;
            TargetCell = targetCell;
        }

        internal void Complete(bool succeeded, string failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason ?? string.Empty;
        }

        internal void RecordStep(
            string description,
            Vector3Int currentCell,
            Vector3Int targetCell,
            HashSet<Vector3Int> openCells,
            HashSet<Vector3Int> closedCells,
            Dictionary<Vector3Int, int> gScore,
            Dictionary<Vector3Int, int> hScore,
            Dictionary<Vector3Int, Vector3Int> cameFrom,
            List<Vector3Int> finalPath)
        {
            steps.Add(new MapPathfindingDebugStep(
                steps.Count,
                description,
                currentCell,
                targetCell,
                openCells,
                closedCells,
                gScore,
                hScore,
                cameFrom,
                finalPath));
        }
    }

    /// <summary>
    /// One immutable A* search snapshot.
    /// </summary>
    public sealed class MapPathfindingDebugStep
    {
        internal MapPathfindingDebugStep(
            int index,
            string description,
            Vector3Int currentCell,
            Vector3Int targetCell,
            HashSet<Vector3Int> openCells,
            HashSet<Vector3Int> closedCells,
            Dictionary<Vector3Int, int> gScore,
            Dictionary<Vector3Int, int> hScore,
            Dictionary<Vector3Int, Vector3Int> cameFrom,
            List<Vector3Int> finalPath)
        {
            Index = index;
            Description = description;
            CurrentCell = currentCell;
            TargetCell = targetCell;
            OpenCells = openCells != null ? new List<Vector3Int>(openCells) : new List<Vector3Int>();
            ClosedCells = closedCells != null ? new List<Vector3Int>(closedCells) : new List<Vector3Int>();
            GScore = gScore != null ? new Dictionary<Vector3Int, int>(gScore) : new Dictionary<Vector3Int, int>();
            HScore = hScore != null ? new Dictionary<Vector3Int, int>(hScore) : new Dictionary<Vector3Int, int>();
            CameFrom = cameFrom != null
                ? new Dictionary<Vector3Int, Vector3Int>(cameFrom)
                : new Dictionary<Vector3Int, Vector3Int>();
            FinalPath = finalPath != null ? new List<Vector3Int>(finalPath) : new List<Vector3Int>();
        }

        public int Index { get; }
        public string Description { get; }
        public Vector3Int CurrentCell { get; }
        public Vector3Int TargetCell { get; }
        public IReadOnlyList<Vector3Int> OpenCells { get; }
        public IReadOnlyList<Vector3Int> ClosedCells { get; }
        public IReadOnlyDictionary<Vector3Int, int> GScore { get; }
        public IReadOnlyDictionary<Vector3Int, int> HScore { get; }
        public IReadOnlyDictionary<Vector3Int, Vector3Int> CameFrom { get; }
        public IReadOnlyList<Vector3Int> FinalPath { get; }

        public bool TryGetCosts(Vector3Int cell, out int gCost, out int hCost, out int fCost)
        {
            bool hasG = GScore.TryGetValue(cell, out gCost);
            bool hasH = HScore.TryGetValue(cell, out hCost);
            fCost = hasG && hasH ? gCost + hCost : 0;
            return hasG && hasH;
        }
    }
}
#endif
