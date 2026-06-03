using UnityEngine;

namespace GameData
{
    public readonly struct MapGridCellInfo
    {
        public MapGridCellInfo(
            Vector3Int cellPosition,
            int gridX,
            int gridY,
            MapGridCellFlags staticFlags,
            MapGridCellFlags finalFlags)
        {
            CellPosition = cellPosition;
            GridX = gridX;
            GridY = gridY;
            StaticFlags = staticFlags;
            FinalFlags = finalFlags;
        }

        public Vector3Int CellPosition { get; }
        public int GridX { get; }
        public int GridY { get; }
        public MapGridCellFlags StaticFlags { get; }
        public MapGridCellFlags FinalFlags { get; }
    }
}
