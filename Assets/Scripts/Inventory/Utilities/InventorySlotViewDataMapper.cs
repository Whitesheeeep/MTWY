using GameData;
using UnityEngine;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 背包槽位显示数据转换器，负责把运行时槽位数据转换成 ViewData。
    /// </summary>
    internal static class InventorySlotViewDataMapper
    {
        /// <summary>
        /// 创建槽位显示数据。
        /// </summary>
        /// <param name="slotIndex">槽位索引。</param>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">物品数量。</param>
        /// <param name="itemDatabaseProvider">物品配置读取器。</param>
        /// <returns>槽位显示数据。</returns>
        public static InventorySlotViewData Create(
            int slotIndex,
            int itemId,
            int count,
            System.Func<int, ItemData> itemDatabaseProvider)
        {
            if (itemId <= 0 || count <= 0)
            {
                return InventorySlotViewData.Empty(slotIndex);
            }

            Sprite icon = itemDatabaseProvider?.Invoke(itemId)?.icon;
            return new InventorySlotViewData(slotIndex, itemId, count, icon);
        }
    }
}
