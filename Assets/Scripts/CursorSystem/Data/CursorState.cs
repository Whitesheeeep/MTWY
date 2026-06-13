using GameData;
using UnityEngine;

namespace CursorSystem
{
    /// <summary>
    /// Current cursor snapshot used by UI and interaction code.
    /// </summary>
    public readonly struct CursorState
    {
        public CursorState(
            Sprite icon,
            CursorVisualState visualState,
            ItemData toolData,
            GameObject target,
            IToolInteractable interactable,
            CursorTargetType targetType,
            MapGridCellInfo cellInfo,
            bool inToolRange,
            bool canInteract,
            Vector3Int originCell,
            Vector3Int targetCell)
        {
            Icon = icon;
            VisualState = visualState;
            ToolData = toolData;
            Target = target;
            Interactable = interactable;
            TargetType = targetType;
            CellInfo = cellInfo;
            InToolRange = inToolRange;
            CanInteract = canInteract;
            OriginCell = originCell;
            TargetCell = targetCell;
        }

        public Sprite Icon { get; }
        public CursorVisualState VisualState { get; }
        public ItemData ToolData { get; }
        public GameObject Target { get; }
        public IToolInteractable Interactable { get; }
        public CursorTargetType TargetType { get; }
        public MapGridCellInfo CellInfo { get; }
        public bool HasTool => ToolData != null;
        public bool HasTarget => Target != null;
        public bool HasCellTarget => TargetType == CursorTargetType.MapCell;
        public bool InToolRange { get; }
        public bool CanInteract { get; }
        public Vector3Int OriginCell { get; }
        public Vector3Int TargetCell { get; }

        public bool Equals(CursorState other)
        {
            return Icon == other.Icon
                && VisualState == other.VisualState
                && ToolData == other.ToolData
                && Target == other.Target
                && ReferenceEquals(Interactable, other.Interactable)
                && TargetType == other.TargetType
                && CellInfo.CellPosition == other.CellInfo.CellPosition
                && CellInfo.FinalFlags == other.CellInfo.FinalFlags
                && InToolRange == other.InToolRange
                && CanInteract == other.CanInteract
                && OriginCell == other.OriginCell
                && TargetCell == other.TargetCell;
        }
    }
}
