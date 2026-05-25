using UnityEngine;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 背包槽位所属区域。
    /// </summary>
    public enum InventorySlotArea
    {
        None = 0,
        Bar = 1,
        Bag = 2,
    }

    /// <summary>
    /// 背包槽位拖拽事件参数。
    /// </summary>
    public readonly struct InventorySlotDragEventArgs
    {
        /// <summary>
        /// 拖拽来源区域。
        /// </summary>
        public InventorySlotArea Area { get; }

        /// <summary>
        /// 拖拽来源槽位索引。
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// 当前屏幕坐标。
        /// </summary>
        public Vector2 ScreenPosition { get; }

        /// <summary>
        /// 创建槽位拖拽事件参数。
        /// </summary>
        public InventorySlotDragEventArgs(InventorySlotArea area, int index, Vector2 screenPosition)
        {
            Area = area;
            Index = index;
            ScreenPosition = screenPosition;
        }
    }

    /// <summary>
    /// 背包槽位释放事件参数。
    /// </summary>
    public readonly struct InventorySlotDropEventArgs
    {
        /// <summary>
        /// 拖拽来源区域。
        /// </summary>
        public InventorySlotArea SourceArea { get; }

        /// <summary>
        /// 拖拽来源槽位索引。
        /// </summary>
        public int SourceIndex { get; }

        /// <summary>
        /// 释放目标区域。
        /// </summary>
        public InventorySlotArea TargetArea { get; }

        /// <summary>
        /// 释放目标槽位索引。
        /// </summary>
        public int TargetIndex { get; }

        /// <summary>
        /// 当前屏幕坐标。
        /// </summary>
        public Vector2 ScreenPosition { get; }

        /// <summary>
        /// 创建槽位释放事件参数。
        /// </summary>
        public InventorySlotDropEventArgs(
            InventorySlotArea sourceArea,
            int sourceIndex,
            InventorySlotArea targetArea,
            int targetIndex,
            Vector2 screenPosition)
        {
            SourceArea = sourceArea;
            SourceIndex = sourceIndex;
            TargetArea = targetArea;
            TargetIndex = targetIndex;
            ScreenPosition = screenPosition;
        }
    }
}
