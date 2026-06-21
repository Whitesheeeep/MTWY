using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 当前地图 Grid 资源加载并绑定到场景 Grid 后的事件参数。
    /// </summary>
    public readonly struct MapGridCurrentMapLoadedEventArgs
    {
        public MapGridCurrentMapLoadedEventArgs(string mapId, Grid grid, MapGridData_SO mapData)
        {
            MapId = mapId;
            Grid = grid;
            MapData = mapData;
        }

        public string MapId { get; }
        public Grid Grid { get; }
        public MapGridData_SO MapData { get; }
    }
}
