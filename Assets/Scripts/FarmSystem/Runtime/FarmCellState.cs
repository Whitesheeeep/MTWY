using UnityEngine;

namespace FarmSystem
{
    /// <summary>
    /// 农田单格运行时状态快照，供表现层读取。
    /// </summary>
    public readonly struct FarmCellState
    {
        public FarmCellState(
            string mapId,
            Vector3Int cell,
            bool isTilled,
            bool isWatered,
            bool isPlanted)
        {
            MapId = mapId;
            Cell = cell;
            IsTilled = isTilled;
            IsWatered = isWatered;
            IsPlanted = isPlanted;
        }

        public string MapId { get; }
        public Vector3Int Cell { get; }
        public bool IsTilled { get; }
        public bool IsWatered { get; }
        public bool IsPlanted { get; }
    }
}
