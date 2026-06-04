using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 运行时查询返回的格子信息，包含静态属性和叠加 runtime override 后的最终属性。
    /// </summary>
    public readonly struct MapGridCellInfo
    {
        /// <summary>
        /// 创建一份运行时格子查询结果。
        /// </summary>
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

        /// <summary>
        /// Unity Tilemap 的原始 cell 坐标。
        /// </summary>
        public Vector3Int CellPosition { get; }

        /// <summary>
        /// 基于当前地图统一 originCell 的 X 轴数组索引。
        /// </summary>
        public int GridX { get; }

        /// <summary>
        /// 基于当前地图统一 originCell 的 Y 轴数组索引。
        /// </summary>
        public int GridY { get; }

        /// <summary>
        /// MapGridData_SO 中记录的原始静态属性。
        /// </summary>
        public MapGridCellFlags StaticFlags { get; }

        /// <summary>
        /// 静态属性叠加所有运行时覆盖后的最终属性。
        /// </summary>
        public MapGridCellFlags FinalFlags { get; }
    }
}
