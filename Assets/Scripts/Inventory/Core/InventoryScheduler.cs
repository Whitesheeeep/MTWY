using System.Collections.Generic;

namespace Inventory
{
    /// <summary>
    /// 背包数据调度器，负责协调 Bar 和 Bag 之间的移动、合并、拆分和拾取分配。
    /// </summary>
    public sealed class InventoryScheduler
    {
        private readonly InventoryData barData;
        private readonly InventoryData bagData;

        /// <summary>
        /// 创建背包数据调度器。
        /// </summary>
        /// <param name="barData">快捷栏数据。</param>
        /// <param name="bagData">背包数据。</param>
        public InventoryScheduler(InventoryData barData, InventoryData bagData)
        {
            this.barData = barData;
            this.bagData = bagData;
        }

        /// <summary>
        /// 添加物品，优先进入 Bar，Bar 放不下的剩余数量进入 Bag。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">添加数量。</param>
        /// <param name="maxStackCount">单槽堆叠上限。</param>
        /// <param name="changeSet">受影响槽位集合。</param>
        /// <returns>最终未能放入的剩余数量。</returns>
        public int AddItem(int itemId, int count, int maxStackCount, InventoryChangeSet changeSet)
        {
            List<int> barChanged = new List<int>();
            int remaining = barData.AddItem(itemId, count, maxStackCount, barChanged);
            changeSet?.AddBarSlots(barChanged);

            if (remaining <= 0)
            {
                return 0;
            }

            List<int> bagChanged = new List<int>();
            remaining = bagData.AddItem(itemId, remaining, maxStackCount, bagChanged);
            changeSet?.AddBagSlots(bagChanged);
            return remaining;
        }

        /// <summary>
        /// 移动 Bar 内部槽位。
        /// </summary>
        public bool MoveBarSlot(int fromIndex, int toIndex, int maxStackCount, InventoryChangeSet changeSet)
        {
            System.Action<IEnumerable<int>> addChanged = changeSet == null ? null : changeSet.AddBarSlots;
            return MoveSlot(barData, fromIndex, toIndex, maxStackCount, addChanged);
        }

        /// <summary>
        /// 移动 Bag 内部槽位。
        /// </summary>
        public bool MoveBagSlot(int fromIndex, int toIndex, int maxStackCount, InventoryChangeSet changeSet)
        {
            System.Action<IEnumerable<int>> addChanged = changeSet == null ? null : changeSet.AddBagSlots;
            return MoveSlot(bagData, fromIndex, toIndex, maxStackCount, addChanged);
        }

        /// <summary>
        /// 将 Bag 槽位移动到 Bar 槽位。
        /// </summary>
        public bool MoveBagToBar(int bagIndex, int barIndex, int maxStackCount, InventoryChangeSet changeSet)
        {
            return MoveBetween(bagData, bagIndex, barData, barIndex, maxStackCount, changeSet, false);
        }

        /// <summary>
        /// 将 Bar 槽位移动到 Bag 槽位。
        /// </summary>
        public bool MoveBarToBag(int barIndex, int bagIndex, int maxStackCount, InventoryChangeSet changeSet)
        {
            return MoveBetween(barData, barIndex, bagData, bagIndex, maxStackCount, changeSet, true);
        }

        /// <summary>
        /// 从 Bag 拆分指定数量到 Bar。
        /// </summary>
        public bool SplitBagToBar(int bagIndex, int count, int barIndex, int maxStackCount, InventoryChangeSet changeSet)
        {
            return SplitBetween(bagData, bagIndex, count, barData, barIndex, maxStackCount, changeSet, false);
        }

        /// <summary>
        /// 从 Bar 拆分指定数量到 Bag。
        /// </summary>
        public bool SplitBarToBag(int barIndex, int count, int bagIndex, int maxStackCount, InventoryChangeSet changeSet)
        {
            return SplitBetween(barData, barIndex, count, bagData, bagIndex, maxStackCount, changeSet, true);
        }

        private static bool MoveSlot(InventoryData data, int fromIndex, int toIndex, int maxStackCount, System.Action<IEnumerable<int>> addChanged)
        {
            List<int> changed = new List<int>();
            bool success = data.MoveSlot(fromIndex, toIndex, maxStackCount, changed);
            if (success)
            {
                addChanged?.Invoke(changed);
            }

            return success;
        }

        private static bool MoveBetween(
            InventoryData fromData,
            int fromIndex,
            InventoryData toData,
            int toIndex,
            int maxStackCount,
            InventoryChangeSet changeSet,
            bool fromIsBar)
        {
            if (!TryGetMutableSlotPair(fromData, fromIndex, toData, toIndex, out InventorySlotData from, out InventorySlotData to))
            {
                return false;
            }

            if (from.IsEmpty)
            {
                return false;
            }

            if (to.IsEmpty)
            {
                to.Set(from.itemId, from.count);
                from.Clear();
                AddCrossChanged(changeSet, fromIsBar, fromIndex, toIndex);
                return true;
            }

            if (from.itemId == to.itemId && to.count < maxStackCount)
            {
                int moveCount = System.Math.Min(maxStackCount - to.count, from.count);
                to.count += moveCount;
                from.count -= moveCount;
                if (from.count <= 0)
                {
                    from.Clear();
                }

                AddCrossChanged(changeSet, fromIsBar, fromIndex, toIndex);
                return moveCount > 0;
            }

            int tempItemId = to.itemId;
            int tempCount = to.count;
            to.Set(from.itemId, from.count);
            from.Set(tempItemId, tempCount);
            AddCrossChanged(changeSet, fromIsBar, fromIndex, toIndex);
            return true;
        }

        private static bool SplitBetween(
            InventoryData fromData,
            int fromIndex,
            int count,
            InventoryData toData,
            int toIndex,
            int maxStackCount,
            InventoryChangeSet changeSet,
            bool fromIsBar)
        {
            if (count <= 0 ||
                !TryGetMutableSlotPair(fromData, fromIndex, toData, toIndex, out InventorySlotData from, out InventorySlotData to) ||
                from.IsEmpty ||
                from.count < count)
            {
                return false;
            }

            if (!to.IsEmpty && (to.itemId != from.itemId || to.count >= maxStackCount))
            {
                return false;
            }

            int targetCanReceive = to.IsEmpty ? maxStackCount : maxStackCount - to.count;
            if (targetCanReceive < count)
            {
                return false;
            }

            if (to.IsEmpty)
            {
                to.Set(from.itemId, count);
            }
            else
            {
                to.count += count;
            }

            from.count -= count;
            if (from.count <= 0)
            {
                from.Clear();
            }

            AddCrossChanged(changeSet, fromIsBar, fromIndex, toIndex);
            return true;
        }

        private static bool TryGetMutableSlotPair(
            InventoryData fromData,
            int fromIndex,
            InventoryData toData,
            int toIndex,
            out InventorySlotData from,
            out InventorySlotData to)
        {
            from = null;
            to = null;
            return fromData.TryGetMutableSlot(fromIndex, out from) && toData.TryGetMutableSlot(toIndex, out to);
        }

        private static void AddCrossChanged(InventoryChangeSet changeSet, bool fromIsBar, int fromIndex, int toIndex)
        {
            if (changeSet == null)
            {
                return;
            }

            if (fromIsBar)
            {
                changeSet.AddBarSlot(fromIndex);
                changeSet.AddBagSlot(toIndex);
            }
            else
            {
                changeSet.AddBagSlot(fromIndex);
                changeSet.AddBarSlot(toIndex);
            }
        }
    }
}
