using System;
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// Builds and owns the static cell index loaded from MapGridData_SO.
    /// </summary>
    public sealed class MapGridStaticModule
    {
        private MapGridCellData[] cells = Array.Empty<MapGridCellData>();

        public MapGridData_SO LoadedMapData { get; private set; }
        public Vector3Int OriginCell { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public MapGridCellData[] Cells => cells;
        public bool IsLoaded => LoadedMapData != null;
        public string CurrentMapId => LoadedMapData != null ? LoadedMapData.mapId : string.Empty;

        public void Load(MapGridData_SO mapData)
        {
            if (mapData == null)
            {
                throw new ArgumentNullException(nameof(mapData));
            }

            if (!mapData.IsValid)
            {
                throw new ArgumentException($"[MapGridStaticModule] Invalid map data: {mapData.name}", nameof(mapData));
            }

            LoadedMapData = mapData;
            OriginCell = mapData.originCell;
            Width = mapData.width;
            Height = mapData.height;
            cells = new MapGridCellData[Width * Height];

            for (int gridY = 0; gridY < Height; gridY++)
            {
                for (int gridX = 0; gridX < Width; gridX++)
                {
                    int index = gridY * Width + gridX;
                    cells[index] = new MapGridCellData
                    {
                        cellPosition = new Vector3Int(OriginCell.x + gridX, OriginCell.y + gridY, OriginCell.z),
                        gridX = gridX,
                        gridY = gridY,
                        staticFlags = MapGridCellFlags.None
                    };
                }
            }

            foreach (MapGridCellData cellData in mapData.cells)
            {
                if (!TryGetIndex(cellData.cellPosition, out int index))
                {
                    Debug.LogWarning($"[MapGridStaticModule] Cell skipped because it is outside bounds. Map:{mapData.mapId}, Cell:{cellData.cellPosition}");
                    continue;
                }

                cells[index] = cellData;
            }
        }

        public void Unload()
        {
            LoadedMapData = null;
            OriginCell = Vector3Int.zero;
            Width = 0;
            Height = 0;
            cells = Array.Empty<MapGridCellData>();
        }

        public bool TryGetCellData(Vector3Int cell, out MapGridCellData data)
        {
            if (TryGetIndex(cell, out int index))
            {
                data = cells[index];
                return true;
            }

            data = default;
            return false;
        }

        public bool ContainsCell(Vector3Int cell)
        {
            return TryGetIndex(cell, out _);
        }

        private bool TryGetIndex(Vector3Int cell, out int index)
        {
            int gridX = cell.x - OriginCell.x;
            int gridY = cell.y - OriginCell.y;
            if (gridX < 0 || gridX >= Width || gridY < 0 || gridY >= Height)
            {
                index = -1;
                return false;
            }

            index = gridY * Width + gridX;
            return true;
        }
    }
}
