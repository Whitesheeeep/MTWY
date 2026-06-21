using UnityEngine;

namespace FarmSystem
{
    /// <summary>
    /// 水分自然消退事件参数，供后续作物成长、缺水判断或表现层扩展订阅。
    /// </summary>
    public readonly struct FarmWaterDrainedEventArgs
    {
        public FarmWaterDrainedEventArgs(
            string mapId,
            Vector3Int cell,
            FarmWaterState previousWaterState,
            FarmCellState previousCellState,
            FarmCellState currentCellState)
        {
            MapId = mapId;
            Cell = cell;
            PreviousWaterState = previousWaterState;
            PreviousCellState = previousCellState;
            CurrentCellState = currentCellState;
        }

        public string MapId { get; }
        public Vector3Int Cell { get; }
        public FarmWaterState PreviousWaterState { get; }
        public FarmCellState PreviousCellState { get; }
        public FarmCellState CurrentCellState { get; }
    }
}
