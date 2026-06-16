using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 地图 Grid 运行时查询数据库。
    /// </summary>
    public interface IMapGridDatabase : IGameSubDatabase
    {
        /// <summary>
        /// 当前已加载地图的 ID。未加载时为空字符串。
        /// </summary>
        string CurrentMapId { get; }

        /// <summary>
        /// 当前已加载的静态地图数据资产。未加载时为空。
        /// </summary>
        MapGridData_SO CurrentMapData { get; }

        /// <summary>
        /// 当前已加载地图对应的场景 Grid。未加载时为空。
        /// </summary>
        Grid CurrentGrid { get; }

        /// <summary>
        /// 当前是否持有可用于世界坐标转换的 Grid。
        /// </summary>
        bool HasCurrentGrid { get; }

        UniTask<bool> EnsureLoadedAsync(string mapId);

        bool IsLoaded(string mapId);

        /// <summary>
        /// 加载地图数据并重建运行时索引，同时清空 runtime overrides。
        /// </summary>
        void LoadMap(MapGridData_SO mapData, Grid grid);

        /// <summary>
        /// 卸载当前地图并清空索引和 runtime overrides。
        /// </summary>
        void UnloadCurrentMap();

        /// <summary>
        /// 使用当前 MapGridData_SO 重新构建运行时索引。
        /// </summary>
        void ReloadCurrentMap();

        /// <summary>
        /// 将 cell 坐标转换为当前 Grid 中的世界中心点。
        /// </summary>
        Vector3 GetCellCenterWorld(Vector3Int cell);

        /// <summary>
        /// 将世界坐标转换为当前 Grid 中的 cell 坐标。
        /// </summary>
        Vector3Int WorldToCell(Vector3 worldPosition);

        /// <summary>
        /// 在当前地图中查询 cell。
        /// </summary>
        bool TryGetCell(Vector3Int cell, out MapGridCellInfo info);

        /// <summary>
        /// 在指定地图中查询 cell。第一版只支持当前地图。
        /// </summary>
        bool TryGetCell(string mapId, Vector3Int cell, out MapGridCellInfo info);

        /// <summary>
        /// 判断当前地图中的 cell 是否包含指定 flag。
        /// </summary>
        bool HasFlag(Vector3Int cell, MapGridCellFlags flag);

        /// <summary>
        /// 判断指定地图中的 cell 是否包含指定 flag。第一版只支持当前地图。
        /// </summary>
        bool HasFlag(string mapId, Vector3Int cell, MapGridCellFlags flag);

        /// <summary>
        /// 判断当前地图中的 cell 是否可通行。
        /// </summary>
        bool IsWalkable(Vector3Int cell);

        /// <summary>
        /// 判断指定地图中的 cell 是否可通行。第一版只支持当前地图。
        /// </summary>
        bool IsWalkable(string mapId, Vector3Int cell);

        /// <summary>
        /// 获取当前地图中可通行的邻居 cell。
        /// </summary>
        IEnumerable<Vector3Int> GetNeighbors(Vector3Int cell, bool includeDiagonal = false);

        /// <summary>
        /// 获取指定地图中可通行的邻居 cell。第一版只支持当前地图。
        /// </summary>
        IEnumerable<Vector3Int> GetNeighbors(string mapId, Vector3Int cell, bool includeDiagonal = false);

        void SetRuntimeOverride(string mapId, string sourceId, Vector3Int cell, MapGridCellFlags addFlags, MapGridCellFlags removeFlags = MapGridCellFlags.None);

        void ClearRuntimeOverride(string mapId, string sourceId, Vector3Int cell);

        void ClearRuntimeOverrides(string mapId, string sourceId);

        void ClearAllRuntimeOverrides(string mapId);

        /// <summary>
        /// 设置某个来源对 cell 的运行时覆盖。
        /// </summary>
        void SetRuntimeOverride(string sourceId, Vector3Int cell, MapGridCellFlags addFlags, MapGridCellFlags removeFlags = MapGridCellFlags.None);

        /// <summary>
        /// 清除某个来源在指定 cell 上的运行时覆盖。
        /// </summary>
        void ClearRuntimeOverride(string sourceId, Vector3Int cell);

        /// <summary>
        /// 清除某个来源在所有 cell 上的运行时覆盖。
        /// </summary>
        void ClearRuntimeOverrides(string sourceId);

        /// <summary>
        /// 清除当前地图上的全部运行时覆盖。
        /// </summary>
        void ClearAllRuntimeOverrides();
    }
}
