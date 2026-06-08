using System.Collections.Generic;
using UnityEngine;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// Inventory 槽位布局接口，负责槽位实例、可见范围和显示刷新。
    /// </summary>
    public interface IInventorySlotViewLayout
    {
        /// <summary>
        /// 当前布局显示的槽位数量。
        /// </summary>
        /// <summary>
        /// 设置布局运行所需上下文。
        /// </summary>
        /// <param name="slotPrefab">槽位预制体。</param>
        /// <param name="slotRoot">槽位根节点。</param>
        /// <param name="eventModule">槽位输入事件模块。</param>
        void SetContext(
            InventorySlotView slotPrefab,
            Transform slotRoot,
            InventorySlotViewEventModule eventModule);

        /// <summary>
        /// 设置当前可显示的槽位数量。
        /// </summary>
        /// <param name="count">槽位数量。</param>
        /// <summary>
        /// 刷新指定槽位。
        /// </summary>
        void RefreshSlot(int index, InventorySlotViewData data, bool selected);

        /// <summary>
        /// 刷新全部可见槽位。
        /// </summary>
        void RefreshSlots(IReadOnlyList<InventorySlotViewData> dataList, int selectedIndex);

        /// <summary>
        /// 刷新选中状态。
        /// </summary>
        void RefreshSelection(int selectedIndex);

        /// <summary>
        /// 刷新拖拽放置预览。
        /// </summary>
        void RefreshDropPreview(int index, bool canDrop);

        /// <summary>
        /// 清理拖拽放置预览。
        /// </summary>
        void ClearDropPreview();

        /// <summary>
        /// 清空槽位显示。
        /// </summary>
        void ClearSlots();
    }
}
