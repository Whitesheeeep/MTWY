using GameData;
using UnityEngine;

namespace CursorSystem
{
    /// <summary>
    /// 当前光标状态快照，供 UI 显示和交互执行逻辑使用。
    /// </summary>
    public readonly struct CursorState
    {
        public CursorState(
            Sprite icon,
            CursorVisualState visualState,
            ItemData selectedItemData,
            GameObject target,
            IItemInteractable interactable,
            CursorTargetType targetType,
            ItemInteractionContext interactionContext,
            bool hasInteractionContext,
            MapGridCellInfo cellInfo,
            bool inToolRange,
            bool canInteract,
            Vector3Int originCell,
            Vector3Int targetCell)
        {
            Icon = icon;
            VisualState = visualState;
            SelectedItemData = selectedItemData;
            Target = target;
            Interactable = interactable;
            TargetType = targetType;
            InteractionContext = interactionContext;
            HasInteractionContext = hasInteractionContext;
            CellInfo = cellInfo;
            InToolRange = inToolRange;
            CanInteract = canInteract;
            OriginCell = originCell;
            TargetCell = targetCell;
        }

        public Sprite Icon { get; }
        public CursorVisualState VisualState { get; }
        public ItemData SelectedItemData { get; }
        public GameObject Target { get; }
        public IItemInteractable Interactable { get; }
        public CursorTargetType TargetType { get; }
        public ItemInteractionContext InteractionContext { get; }
        public bool HasInteractionContext { get; }
        public MapGridCellInfo CellInfo { get; }
        public bool HasSelectedItem => SelectedItemData != null;
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
                && SelectedItemData == other.SelectedItemData
                && Target == other.Target
                && ReferenceEquals(Interactable, other.Interactable)
                && TargetType == other.TargetType
                && HasInteractionContext == other.HasInteractionContext
                && CellInfo.CellPosition == other.CellInfo.CellPosition
                && CellInfo.FinalFlags == other.CellInfo.FinalFlags
                && InToolRange == other.InToolRange
                && CanInteract == other.CanInteract
                && OriginCell == other.OriginCell
                && TargetCell == other.TargetCell;
        }
    }
}
