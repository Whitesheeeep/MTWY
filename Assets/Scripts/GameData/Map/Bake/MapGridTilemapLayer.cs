using System;
using UnityEngine.Tilemaps;

namespace GameData
{
    /// <summary>
    /// 描述一个 Tilemap 图层在 Bake 时会写入哪些地图逻辑属性。
    /// </summary>
    [Serializable]
    public sealed class MapGridTilemapLayer
    {
        /// <summary>
        /// 参与扫描的 Tilemap。
        /// </summary>
        public Tilemap tilemap;

        /// <summary>
        /// 该 Tilemap 某个 cell 有 Tile 时写入的属性。
        /// </summary>
        public MapGridCellFlags flags;

        /// <summary>
        /// 是否参与统一地图 bounds 的计算。
        /// </summary>
        public bool affectsBounds = true;
    }
}
