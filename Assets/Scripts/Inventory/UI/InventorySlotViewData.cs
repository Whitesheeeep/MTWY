using UnityEngine;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 背包槽位的 UI 显示数据，只用于 View 渲染，不持有运行时背包数据引用。
    /// </summary>
    public readonly struct InventorySlotViewData
    {
        /// <summary>
        /// 槽位索引。
        /// </summary>
        public readonly int slotIndex;

        /// <summary>
        /// 物品编号。
        /// </summary>
        public readonly int itemId;

        /// <summary>
        /// 当前槽位物品数量。
        /// </summary>
        public readonly int count;

        /// <summary>
        /// 当前槽位物品图标。
        /// </summary>
        public readonly Sprite icon;

        /// <summary>
        /// 当前槽位是否为空。
        /// </summary>
        public bool IsEmpty => itemId <= 0 || count <= 0;

        /// <summary>
        /// 创建背包槽位 UI 显示数据。
        /// </summary>
        /// <param name="slotIndex">槽位索引。</param>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">物品数量。</param>
        /// <param name="icon">物品图标。</param>
        public InventorySlotViewData(int slotIndex, int itemId, int count, Sprite icon)
        {
            this.slotIndex = slotIndex;
            this.itemId = itemId;
            this.count = count;
            this.icon = icon;
        }

        /// <summary>
        /// 创建指定索引的空槽位显示数据。
        /// </summary>
        /// <param name="slotIndex">槽位索引。</param>
        /// <returns>空槽位显示数据。</returns>
        public static InventorySlotViewData Empty(int slotIndex)
        {
            return new InventorySlotViewData(slotIndex, 0, 0, null);
        }
    }
}
