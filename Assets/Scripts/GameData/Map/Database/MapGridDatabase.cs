using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameData
{
    public sealed class MapGridDatabase : IMapGridDatabase
    {
        private static readonly Vector3Int[] FourDirections =
        {
            Vector3Int.up,
            Vector3Int.right,
            Vector3Int.down,
            Vector3Int.left
        };

        private static readonly Vector3Int[] EightDirections =
        {
            Vector3Int.up,
            Vector3Int.right,
            Vector3Int.down,
            Vector3Int.left,
            new Vector3Int(1, 1, 0),
            new Vector3Int(1, -1, 0),
            new Vector3Int(-1, -1, 0),
            new Vector3Int(-1, 1, 0)
        };

        private readonly Dictionary<Vector3Int, List<MapGridRuntimeOverride>> runtimeOverrides =
            new Dictionary<Vector3Int, List<MapGridRuntimeOverride>>();

        private MapGridCellData[] cells = Array.Empty<MapGridCellData>();
        private MapGridData_SO loadedMapData;
        private Vector3Int originCell;
        private int width;
        private int height;

        public string CurrentMapId => loadedMapData != null ? loadedMapData.mapId : string.Empty;
        public MapGridData_SO CurrentMapData => loadedMapData;

        public void LoadMap(MapGridData_SO mapData)
        {
            if (mapData == null)
            {
                throw new ArgumentNullException(nameof(mapData));
            }

            if (!mapData.IsValid)
            {
                throw new ArgumentException($"[MapGridDatabase] Invalid map data: {mapData.name}", nameof(mapData));
            }

            loadedMapData = mapData;
            originCell = mapData.originCell;
            width = mapData.width;
            height = mapData.height;
            cells = new MapGridCellData[width * height];
            runtimeOverrides.Clear();

            for (int gridY = 0; gridY < height; gridY++)
            {
                for (int gridX = 0; gridX < width; gridX++)
                {
                    int index = gridY * width + gridX;
                    cells[index] = new MapGridCellData
                    {
                        cellPosition = new Vector3Int(originCell.x + gridX, originCell.y + gridY, originCell.z),
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
                    Debug.LogWarning($"[MapGridDatabase] Cell skipped because it is outside bounds. Map:{mapData.mapId}, Cell:{cellData.cellPosition}");
                    continue;
                }

                cells[index] = cellData;
            }
        }

        public void UnloadCurrentMap()
        {
            loadedMapData = null;
            originCell = Vector3Int.zero;
            width = 0;
            height = 0;
            cells = Array.Empty<MapGridCellData>();
            runtimeOverrides.Clear();
        }

        public void ReloadCurrentMap()
        {
            if (loadedMapData == null)
            {
                return;
            }

            LoadMap(loadedMapData);
        }

        public bool TryGetCell(Vector3Int cell, out MapGridCellInfo info)
        {
            return TryGetCell(CurrentMapId, cell, out info);
        }

        public bool TryGetCell(string mapId, Vector3Int cell, out MapGridCellInfo info)
        {
            info = new MapGridCellInfo();
            if (!IsCurrentMap(mapId) || !TryGetIndex(cell, out int index))
            {
                return false;
            }

            MapGridCellData cellData = cells[index];
            MapGridCellFlags finalFlags = ApplyRuntimeOverrides(cell, cellData.staticFlags);
            info = new MapGridCellInfo(cellData.cellPosition, cellData.gridX, cellData.gridY, cellData.staticFlags, finalFlags);
            return true;
        }

        public bool HasFlag(Vector3Int cell, MapGridCellFlags flag)
        {
            return HasFlag(CurrentMapId, cell, flag);
        }

        public bool HasFlag(string mapId, Vector3Int cell, MapGridCellFlags flag)
        {
            return TryGetCell(mapId, cell, out MapGridCellInfo info) && (info.FinalFlags & flag) == flag;
        }

        public bool IsWalkable(Vector3Int cell)
        {
            return IsWalkable(CurrentMapId, cell);
        }

        public bool IsWalkable(string mapId, Vector3Int cell)
        {
            if (!TryGetCell(mapId, cell, out MapGridCellInfo info))
            {
                return false;
            }

            const MapGridCellFlags BlockingFlags =
                MapGridCellFlags.Blocked | MapGridCellFlags.Water | MapGridCellFlags.NpcObstacle;
            return (info.FinalFlags & BlockingFlags) == MapGridCellFlags.None;
        }

        public IEnumerable<Vector3Int> GetNeighbors(Vector3Int cell, bool includeDiagonal = false)
        {
            return GetNeighbors(CurrentMapId, cell, includeDiagonal);
        }

        public IEnumerable<Vector3Int> GetNeighbors(string mapId, Vector3Int cell, bool includeDiagonal = false)
        {
            Vector3Int[] directions = includeDiagonal ? EightDirections : FourDirections;
            foreach (Vector3Int direction in directions)
            {
                Vector3Int neighbor = cell + direction;
                if (IsWalkable(mapId, neighbor))
                {
                    yield return neighbor;
                }
            }
        }

        public void SetRuntimeOverride(
            string sourceId,
            Vector3Int cell,
            MapGridCellFlags addFlags,
            MapGridCellFlags removeFlags = MapGridCellFlags.None)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException("[MapGridDatabase] Runtime override sourceId is empty.", nameof(sourceId));
            }

            if (!TryGetIndex(cell, out _))
            {
                Debug.LogWarning($"[MapGridDatabase] Runtime override ignored because cell is outside current map. Source:{sourceId}, Cell:{cell}");
                return;
            }

            if (!runtimeOverrides.TryGetValue(cell, out List<MapGridRuntimeOverride> overrides))
            {
                overrides = new List<MapGridRuntimeOverride>();
                runtimeOverrides.Add(cell, overrides);
            }

            overrides.RemoveAll(item => item.SourceId == sourceId);
            overrides.Add(new MapGridRuntimeOverride(sourceId, addFlags, removeFlags));
        }

        public void ClearRuntimeOverride(string sourceId, Vector3Int cell)
        {
            if (string.IsNullOrWhiteSpace(sourceId) || !runtimeOverrides.TryGetValue(cell, out List<MapGridRuntimeOverride> overrides))
            {
                return;
            }

            overrides.RemoveAll(item => item.SourceId == sourceId);
            if (overrides.Count == 0)
            {
                runtimeOverrides.Remove(cell);
            }
        }

        public void ClearRuntimeOverrides(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                return;
            }

            List<Vector3Int> emptyCells = null;
            foreach (KeyValuePair<Vector3Int, List<MapGridRuntimeOverride>> pair in runtimeOverrides)
            {
                pair.Value.RemoveAll(item => item.SourceId == sourceId);
                if (pair.Value.Count == 0)
                {
                    if (emptyCells == null)
                    {
                        emptyCells = new List<Vector3Int>();
                    }

                    emptyCells.Add(pair.Key);
                }
            }

            if (emptyCells == null)
            {
                return;
            }

            foreach (Vector3Int cell in emptyCells)
            {
                runtimeOverrides.Remove(cell);
            }
        }

        public void ClearAllRuntimeOverrides()
        {
            runtimeOverrides.Clear();
        }

        public void Clear()
        {
            UnloadCurrentMap();
        }

        private bool TryGetIndex(Vector3Int cell, out int index)
        {
            int gridX = cell.x - originCell.x;
            int gridY = cell.y - originCell.y;
            if (gridX < 0 || gridX >= width || gridY < 0 || gridY >= height)
            {
                index = -1;
                return false;
            }

            index = gridY * width + gridX;
            return true;
        }

        private bool IsCurrentMap(string mapId)
        {
            return loadedMapData != null && string.Equals(loadedMapData.mapId, mapId, StringComparison.Ordinal);
        }

        private MapGridCellFlags ApplyRuntimeOverrides(Vector3Int cell, MapGridCellFlags staticFlags)
        {
            if (!runtimeOverrides.TryGetValue(cell, out List<MapGridRuntimeOverride> overrides))
            {
                return staticFlags;
            }

            MapGridCellFlags finalFlags = staticFlags;
            foreach (MapGridRuntimeOverride runtimeOverride in overrides)
            {
                finalFlags |= runtimeOverride.AddFlags;
                finalFlags &= ~runtimeOverride.RemoveFlags;
            }

            return finalFlags;
        }
    }
}
