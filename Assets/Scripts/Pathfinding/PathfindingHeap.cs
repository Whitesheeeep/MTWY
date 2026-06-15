using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    internal readonly struct PathfindingHeapNode
    {
        public PathfindingHeapNode(Vector3Int cell, int fCost, int hCost, int sequence)
        {
            Cell = cell;
            FCost = fCost;
            HCost = hCost;
            Sequence = sequence;
        }

        public Vector3Int Cell { get; }
        public int FCost { get; }
        public int HCost { get; }
        public int Sequence { get; }
    }

    internal sealed class PathfindingHeapNodeComparer : IComparer<PathfindingHeapNode>
    {
        public static readonly PathfindingHeapNodeComparer Instance = new PathfindingHeapNodeComparer();

        private PathfindingHeapNodeComparer()
        {
        }

        public int Compare(PathfindingHeapNode x, PathfindingHeapNode y)
        {
            int fCostCompare = x.FCost.CompareTo(y.FCost);
            if (fCostCompare != 0)
            {
                return fCostCompare;
            }

            int hCostCompare = x.HCost.CompareTo(y.HCost);
            if (hCostCompare != 0)
            {
                return hCostCompare;
            }

            return x.Sequence.CompareTo(y.Sequence);
        }
    }
}
