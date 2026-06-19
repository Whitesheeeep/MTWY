#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using WS_Modules;

namespace GameData
{
    /// <summary>
    /// 地图 Grid 数据的 Editor Bake 数据源，挂在地图根节点上。
    /// </summary>
    public sealed class MapGridBakeSource : MonoBehaviour
    {
        /// <summary>
        /// 当前地图对应的场景名，同时作为 MapId 使用。
        /// </summary>
        [WSScene]
        public string mapId;

        /// <summary>
        /// Bake 输出的地图静态数据资产。
        /// </summary>
        public MapGridData_SO outputData;

        /// <summary>
        /// 参与 Bake 的 Tilemap 图层配置。
        /// </summary>
        public List<MapGridTilemapLayer> layers = new List<MapGridTilemapLayer>();
    }
}
#endif
