using System;
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 可序列化的单格静态地图数据，由 Editor Bake 写入 MapGridData_SO。
    /// </summary>
    [Serializable]
    public struct MapGridCellData
    {
        /// <summary>
        /// Unity Tilemap 的原始 cell 坐标。
        /// </summary>
        public Vector3Int cellPosition;

        /// <summary>
        /// 基于地图统一 originCell 计算出的 X 轴数组索引。
        /// </summary>
        public int gridX;

        /// <summary>
        /// 基于地图统一 originCell 计算出的 Y 轴数组索引。
        /// </summary>
        public int gridY;

        /// <summary>
        /// Editor 扫描 Tilemap 图层得到的静态逻辑属性。
        /// </summary>
        public MapGridCellFlags staticFlags;
    }
}
