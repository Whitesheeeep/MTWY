using GameData;
using UnityEngine;

namespace CursorSystem
{
    /// <summary>
    /// Read-only context passed to a hovered target when checking whether the current tool can interact.
    /// </summary>
    public readonly struct ToolInteractionContext
    {
        public ToolInteractionContext(
            global::Player player,
            ItemData toolData,
            Vector2 mouseScreenPosition,
            Vector3 mouseWorldPosition,
            Vector3Int originCell,
            Vector3Int targetCell,
            int toolUseRadius,
            bool inToolRange,
            GameObject target,
            CursorTargetType targetType = CursorTargetType.Entity,
            MapGridCellInfo cellInfo = default)
        {
            Player = player;
            ToolData = toolData;
            ToolType = toolData != null ? toolData.itemType : E_ItemType.None;
            MouseScreenPosition = mouseScreenPosition;
            MouseWorldPosition = mouseWorldPosition;
            OriginCell = originCell;
            TargetCell = targetCell;
            ToolUseRadius = toolUseRadius;
            InToolRange = inToolRange;
            Target = target;
            TargetType = targetType;
            CellInfo = cellInfo;
        }

        public global::Player Player { get; }
        public ItemData ToolData { get; }
        public E_ItemType ToolType { get; }
        public Vector2 MouseScreenPosition { get; }
        public Vector3 MouseWorldPosition { get; }
        public Vector3Int OriginCell { get; }
        public Vector3Int TargetCell { get; }
        public int ToolUseRadius { get; }
        public bool InToolRange { get; }
        public GameObject Target { get; }
        public CursorTargetType TargetType { get; }
        public MapGridCellInfo CellInfo { get; }
    }
}
