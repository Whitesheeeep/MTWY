using System.Collections.Generic;

namespace Inventory
{
    /// <summary>
    /// 背包数据变更集合，用于记录 Bar 和 Bag 中受影响的槽位索引。
    /// </summary>
    public class InventoryChangeSet
    {
        private readonly HashSet<int> barChangedIndices = new HashSet<int>();
        private readonly HashSet<int> bagChangedIndices = new HashSet<int>();

        /// <summary>
        /// Bar 是否需要整体刷新。
        /// </summary>
        public bool BarAllChanged { get; private set; }

        /// <summary>
        /// Bag 是否需要整体刷新。
        /// </summary>
        public bool BagAllChanged { get; private set; }

        /// <summary>
        /// Bar 中发生变化的槽位索引。
        /// </summary>
        public IReadOnlyCollection<int> BarChangedIndices => barChangedIndices;

        /// <summary>
        /// Bag 中发生变化的槽位索引。
        /// </summary>
        public IReadOnlyCollection<int> BagChangedIndices => bagChangedIndices;

        /// <summary>
        /// 是否没有任何变化。
        /// </summary>
        public bool IsEmpty => !BarAllChanged && !BagAllChanged && barChangedIndices.Count == 0 && bagChangedIndices.Count == 0;

        /// <summary>
        /// 记录一个 Bar 槽位变化。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        public void AddBarSlot(int index)
        {
            if (index >= 0)
            {
                barChangedIndices.Add(index);
            }
        }

        /// <summary>
        /// 记录一个 Bag 槽位变化。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        public void AddBagSlot(int index)
        {
            if (index >= 0)
            {
                bagChangedIndices.Add(index);
            }
        }

        /// <summary>
        /// 记录多个 Bar 槽位变化。
        /// </summary>
        /// <param name="indices">槽位索引集合。</param>
        public void AddBarSlots(IEnumerable<int> indices)
        {
            if (indices == null)
            {
                return;
            }

            foreach (int index in indices)
            {
                AddBarSlot(index);
            }
        }

        /// <summary>
        /// 记录多个 Bag 槽位变化。
        /// </summary>
        /// <param name="indices">槽位索引集合。</param>
        public void AddBagSlots(IEnumerable<int> indices)
        {
            if (indices == null)
            {
                return;
            }

            foreach (int index in indices)
            {
                AddBagSlot(index);
            }
        }

        /// <summary>
        /// 标记 Bar 需要整体刷新。
        /// </summary>
        public void MarkBarAllChanged()
        {
            BarAllChanged = true;
            barChangedIndices.Clear();
        }

        /// <summary>
        /// 标记 Bag 需要整体刷新。
        /// </summary>
        public void MarkBagAllChanged()
        {
            BagAllChanged = true;
            bagChangedIndices.Clear();
        }
    }
}
