using UnityEngine;

namespace FarmSystem
{
    /// <summary>
    /// 农田单格水分状态。存在于 WaterModule 中即表示该格处于湿润状态。
    /// </summary>
    public readonly struct FarmWaterState
    {
        public FarmWaterState(string mapId, Vector3Int cell, long expireTotalMinutes)
        {
            MapId = mapId;
            Cell = cell;
            ExpireTotalMinutes = expireTotalMinutes;
        }

        public string MapId { get; }
        public Vector3Int Cell { get; }
        public long ExpireTotalMinutes { get; }
    }
}
