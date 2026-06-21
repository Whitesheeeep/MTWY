using GameData;
using UnityEngine;

namespace CursorSystem
{
    /// <summary>
    /// 当前选中物品与悬停目标进行交互判断时使用的只读上下文。
    /// </summary>
    public readonly struct ItemInteractionContext
    {
        public ItemInteractionContext(
            Player player,
            ItemData selectedItemData,
            Vector2 mouseScreenPosition,
            Vector3 mouseWorldPosition,
            Vector3Int originCell,
            Vector3Int targetCell,
            int itemUseRadius,
            bool inToolRange,
            GameObject target,
            CursorTargetType targetType = CursorTargetType.Entity,
            MapGridCellInfo cellInfo = default)
        {
            Player = player;
            SelectedItemData = selectedItemData;
            SelectedItemType = selectedItemData?.itemType ?? E_ItemType.None;
            MouseScreenPosition = mouseScreenPosition;
            MouseWorldPosition = mouseWorldPosition;
            OriginCell = originCell;
            TargetCell = targetCell;
            ItemUseRadius = itemUseRadius;
            InToolRange = inToolRange;
            Target = target;
            TargetType = targetType;
            CellInfo = cellInfo;
        }

        public Player Player { get; }
        public ItemData SelectedItemData { get; }
        public E_ItemType SelectedItemType { get; }
        public Vector2 MouseScreenPosition { get; }
        public Vector3 MouseWorldPosition { get; }
        public Vector3Int OriginCell { get; }
        public Vector3Int TargetCell { get; }
        public int ItemUseRadius { get; }
        public bool InToolRange { get; }
        public GameObject Target { get; }
        public CursorTargetType TargetType { get; }
        public MapGridCellInfo CellInfo { get; }
    }
}
