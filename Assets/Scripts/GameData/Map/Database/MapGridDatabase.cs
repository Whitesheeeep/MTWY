using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 地图 Grid 运行时数据库，负责把 MapGridData_SO 转成可 O(1) 查询的一维数组索引。
    /// </summary>
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

        /// <summary>
        /// 加载一张静态地图数据，并重建运行时查询索引。
        /// </summary>
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

            // 先填满统一矩形 bounds，保证缺失或旧版本资产不会造成数组空洞。
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

            // 再用 Bake 结果覆盖默认格子。
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

        /// <summary>
        /// 卸载当前地图，并清空所有运行时状态。
        /// </summary>
        public void UnloadCurrentMap()
        {
            loadedMapData = null;
            originCell = Vector3Int.zero;
            width = 0;
            height = 0;
            cells = Array.Empty<MapGridCellData>();
            runtimeOverrides.Clear();
        }

        /// <summary>
        /// 使用当前持有的 MapGridData_SO 重建索引。
        /// </summary>
        public void ReloadCurrentMap()
        {
            if (loadedMapData == null)
            {
                return;
            }

            LoadMap(loadedMapData);
        }

        /// <summary>
        /// 在当前地图中查询格子。
        /// </summary>
        public bool TryGetCell(Vector3Int cell, out MapGridCellInfo info)
        {
            return TryGetCell(CurrentMapId, cell, out info);
        }

        /// <summary>
        /// 查询指定地图中的格子。第一版只接受当前地图 ID。
        /// </summary>
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

        /// <summary>
        /// 判断当前地图中的格子是否包含指定属性。
        /// </summary>
        public bool HasFlag(Vector3Int cell, MapGridCellFlags flag)
        {
            return HasFlag(CurrentMapId, cell, flag);
        }

        /// <summary>
        /// 判断指定地图中的格子是否包含指定属性。
        /// </summary>
        public bool HasFlag(string mapId, Vector3Int cell, MapGridCellFlags flag)
        {
            return TryGetCell(mapId, cell, out MapGridCellInfo info) && (info.FinalFlags & flag) == flag;
        }

        /// <summary>
        /// 判断当前地图中的格子是否可通行。
        /// </summary>
        public bool IsWalkable(Vector3Int cell)
        {
            return IsWalkable(CurrentMapId, cell);
        }

        /// <summary>
        /// 判断指定地图中的格子是否可通行。
        /// </summary>
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

        /// <summary>
        /// 获取当前地图中可通行的邻居格子。
        /// </summary>
        public IEnumerable<Vector3Int> GetNeighbors(Vector3Int cell, bool includeDiagonal = false)
        {
            return GetNeighbors(CurrentMapId, cell, includeDiagonal);
        }

        /// <summary>
        /// 获取指定地图中可通行的邻居格子。
        /// </summary>
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

        /// <summary>
        /// 设置运行时覆盖。相同 sourceId 在同一 cell 上只保留最新一条。
        /// </summary>
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

        /// <summary>
        /// 清除某个来源在指定 cell 上的覆盖。
        /// </summary>
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

        /// <summary>
        /// 清除某个来源在当前地图所有 cell 上的覆盖。
        /// </summary>
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

        /// <summary>
        /// 清除当前地图的全部运行时覆盖。
        /// </summary>
        public void ClearAllRuntimeOverrides()
        {
            runtimeOverrides.Clear();
        }

        /// <summary>
        /// GameDatabase 清理入口。
        /// </summary>
        public void Clear()
        {
            UnloadCurrentMap();
        }

        /// <summary>
        /// 把 Unity cell 坐标转换成一维数组索引。
        /// </summary>
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

        /// <summary>
        /// 判断查询的地图 ID 是否为当前已加载地图。
        /// </summary>
        private bool IsCurrentMap(string mapId)
        {
            return loadedMapData != null && string.Equals(loadedMapData.mapId, mapId, StringComparison.Ordinal);
        }

        /// <summary>
        /// 将所有运行时覆盖叠加到静态属性上，得到最终查询属性。
        /// </summary>
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
