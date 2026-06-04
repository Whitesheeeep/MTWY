using System.Collections.Generic;
using UnityEngine;
using WS_Modules;

namespace GameData
{
    /// <summary>
    /// 一张地图的静态 Grid 数据资产，由 MapGridBakeSourceEditor 扫描 Tilemap 后生成。
    /// </summary>
    [CreateAssetMenu(fileName = "MapGridData", menuName = "GameData/Map/Grid Data", order = 0)]
    public sealed class MapGridData_SO : ScriptableObject
    {
        /// <summary>
        /// 地图 ID。当前约定为对应场景名，并通过 WSScene 从 Build Settings 选择。
        /// </summary>
        [WSScene]
        public string mapId;

        /// <summary>
        /// 统一 Grid 的左下角 cell 坐标。
        /// </summary>
        public Vector3Int originCell;

        /// <summary>
        /// 统一 Grid 宽度。
        /// </summary>
        public int width;

        /// <summary>
        /// 统一 Grid 高度。
        /// </summary>
        public int height;

        /// <summary>
        /// 来源 Grid 的 cellSize，用于后续从世界坐标换算时参考。
        /// </summary>
        public Vector3 cellSize = Vector3.one;

        /// <summary>
        /// 按 gridY * width + gridX 顺序保存的格子静态数据。
        /// </summary>
        public List<MapGridCellData> cells = new List<MapGridCellData>();

        /// <summary>
        /// 数据是否具备运行时加载所需的基本字段。
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(mapId) && width > 0 && height > 0 && cells != null;
    }
}
