using System;
using UnityEngine;

namespace GameData
{
    [Serializable]
    public struct MapGridCellData
    {
        public Vector3Int cellPosition;
        public int gridX;
        public int gridY;
        public MapGridCellFlags staticFlags;
    }
}
