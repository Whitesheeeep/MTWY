using UnityEngine;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// Inventory 槽位拖拽事件参数。
    /// </summary>
    public readonly struct InventorySlotDragEventArgs
    {
        /// <summary>
        /// 槽位索引。
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// 当前屏幕坐标。
        /// </summary>
        public Vector2 ScreenPosition { get; }

        /// <summary>
        /// 目标槽位的屏幕尺寸。
        /// </summary>
        public Vector2 TargetScreenSize { get; }

        /// <summary>
        /// 创建槽位拖拽事件参数。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <param name="screenPosition">当前屏幕坐标。</param>
        public InventorySlotDragEventArgs(int index, Vector2 screenPosition)
        {
            Index = index;
            ScreenPosition = screenPosition;
            TargetScreenSize = Vector2.zero;
        }

        /// <summary>
        /// 创建带目标槽位屏幕尺寸的槽位拖拽事件参数。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <param name="screenPosition">当前屏幕坐标。</param>
        /// <param name="targetScreenSize">目标槽位的屏幕尺寸。</param>
        public InventorySlotDragEventArgs(int index, Vector2 screenPosition, Vector2 targetScreenSize)
        {
            Index = index;
            ScreenPosition = screenPosition;
            TargetScreenSize = targetScreenSize;
        }
    }

    /// <summary>
    /// Inventory 槽位释放事件参数。
    /// </summary>
    public readonly struct InventorySlotDropEventArgs
    {
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
        /// <param name="targetIndex">释放目标槽位索引。</param>
        /// <param name="screenPosition">当前屏幕坐标。</param>
        public InventorySlotDropEventArgs(int targetIndex, Vector2 screenPosition)
        {
            TargetIndex = targetIndex;
            ScreenPosition = screenPosition;
        }
    }
}
