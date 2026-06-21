using UnityEngine;

namespace FarmSystem
{
    /// <summary>
    /// 农田单格状态变化事件参数。
    /// </summary>
    public readonly struct FarmCellStateChangedEventArgs
    {
        public FarmCellStateChangedEventArgs(FarmCellState state)
        {
            State = state;
        }

        public FarmCellState State { get; }
        public string MapId => State.MapId;
        public Vector3Int Cell => State.Cell;
        public bool IsTilled => State.IsTilled;
        public bool IsWatered => State.IsWatered;
        public bool IsPlanted => State.IsPlanted;
    }
}
