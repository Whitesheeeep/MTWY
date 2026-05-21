using System;

namespace Inventory
{
    /// <summary>
    /// 背包中的单个槽位数据，保存物品编号和当前堆叠数量。
    /// </summary>
    [Serializable]
    public sealed class InventorySlotData
    {
        public int itemId;
        public int count;

        /// <summary>
        /// 当前槽位是否为空。
        /// </summary>
        public bool IsEmpty => itemId <= 0 || count <= 0;

        /// <summary>
        /// 创建一个空槽位。
        /// </summary>
        public InventorySlotData()
        {
            Clear();
        }

        /// <summary>
        /// 创建一个带有指定物品和数量的槽位。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">物品数量。</param>
        public InventorySlotData(int itemId, int count)
        {
            Set(itemId, count);
        }

        /// <summary>
        /// 设置槽位中的物品和数量。
        /// </summary>
        /// <param name="newItemId">新的物品编号。</param>
        /// <param name="newCount">新的物品数量。</param>
        public void Set(int newItemId, int newCount)
        {
            itemId = newItemId;
            count = newCount;
        }

        /// <summary>
        /// 清空当前槽位。
        /// </summary>
        public void Clear()
        {
            itemId = 0;
            count = 0;
        }

        /// <summary>
        /// 创建当前槽位数据的副本。
        /// </summary>
        /// <returns>新的槽位数据实例。</returns>
        public InventorySlotData Clone()
        {
            return new InventorySlotData(itemId, count);
        }
    }
}
