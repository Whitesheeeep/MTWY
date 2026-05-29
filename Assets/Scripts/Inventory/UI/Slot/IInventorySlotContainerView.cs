using System.Collections.Generic;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// Inventory 槽位容器 View 契约，用于统一固定槽位和虚拟滚动槽位的刷新入口。
    /// </summary>
    public interface IInventorySlotContainerView
    {
        /// <summary>
        /// 刷新指定槽位显示。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <param name="data">槽位显示数据。</param>
        /// <param name="selected">是否选中。</param>
        void RefreshSlot(int index, InventorySlotViewData data, bool selected);

        /// <summary>
        /// 刷新槽位列表显示。
        /// </summary>
        /// <param name="slotDataList">槽位显示数据列表。</param>
        /// <param name="selectedSlotIndex">当前选中槽位索引。</param>
        void RefreshSlots(IReadOnlyList<InventorySlotViewData> slotDataList, int selectedSlotIndex);

        /// <summary>
        /// 刷新选中状态。
        /// </summary>
        /// <param name="selectedSlotIndex">当前选中槽位索引。</param>
        void RefreshSelection(int selectedSlotIndex);

        /// <summary>
        /// 刷新拖拽放置预览。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <param name="canDrop">是否可放置。</param>
        void RefreshDropPreview(int index, bool canDrop);

        /// <summary>
        /// 清理拖拽放置预览。
        /// </summary>
        void ClearDropPreview();
    }
}
