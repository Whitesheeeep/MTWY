using System;
using System.Collections.Generic;
using UnityEngine;

namespace Inventory
{
    /// <summary>
    /// 固定槽位背包数据，负责槽位规范化、物品堆叠、拆分、合并和序列化快照。
    /// </summary>
    [Serializable]
    public sealed class InventoryData
    {
        [SerializeField] private List<InventorySlotData> slots = new List<InventorySlotData>();

        /// <summary>
        /// 当前背包槽位数量。
        /// </summary>
        public int SlotCount => slots.Count;

        /// <summary>
        /// 按固定容量整理槽位数量，缺少的槽位会补空，超出的槽位会截断。
        /// </summary>
        /// <param name="capacity">目标槽位数量。</param>
        public void NormalizeCapacity(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "背包容量不能小于 0。");
            }

            slots ??= new List<InventorySlotData>(capacity);

            while (slots.Count < capacity)
            {
                slots.Add(new InventorySlotData());
            }

            if (slots.Count > capacity)
            {
                slots.RemoveRange(capacity, slots.Count - capacity);
            }

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null)
                {
                    slots[i] = new InventorySlotData();
                }

                if (slots[i].IsEmpty)
                {
                    slots[i].Clear();
                }
            }
        }

        /// <summary>
        /// 向背包中加入物品，优先补充已有同类堆叠，再写入空槽。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">需要加入的数量。</param>
        /// <param name="maxStackCount">单槽最大堆叠数量。</param>
        /// <returns>未能放入背包的剩余数量，返回 0 表示全部放入。</returns>
        public int AddItem(int itemId, int count, int maxStackCount)
        {
            ValidatePositiveCount(count);
            ValidateMaxStackCount(maxStackCount);

            int remaining = count;

            // 先填充同种物品的未满槽位，保持类似 MC 的自然堆叠体验。
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                InventorySlotData slot = slots[i];
                if (slot.IsEmpty || slot.itemId != itemId || slot.count >= maxStackCount)
                {
                    continue;
                }

                int addCount = Math.Min(maxStackCount - slot.count, remaining);
                slot.count += addCount;
                remaining -= addCount;
            }

            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                InventorySlotData slot = slots[i];
                if (!slot.IsEmpty)
                {
                    continue;
                }

                int addCount = Math.Min(maxStackCount, remaining);
                slot.Set(itemId, addCount);
                remaining -= addCount;
            }

            return remaining;
        }

        /// <summary>
        /// 从背包中移除指定数量的物品，数量不足时不会修改背包。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">需要移除的数量。</param>
        /// <returns>移除成功返回 true，数量不足返回 false。</returns>
        public bool RemoveItem(int itemId, int count)
        {
            ValidatePositiveCount(count);

            if (!HasEnough(itemId, count))
            {
                return false;
            }

            int remaining = count;
            for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
            {
                InventorySlotData slot = slots[i];
                if (slot.IsEmpty || slot.itemId != itemId)
                {
                    continue;
                }

                int removeCount = Math.Min(slot.count, remaining);
                slot.count -= removeCount;
                remaining -= removeCount;

                if (slot.count <= 0)
                {
                    slot.Clear();
                }
            }

            return true;
        }

        /// <summary>
        /// 设置某个物品在背包中的总数量，会重新按堆叠上限分布到槽位。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">目标总数量，0 表示清空该物品。</param>
        /// <param name="maxStackCount">单槽最大堆叠数量。</param>
        /// <returns>设置成功返回 true，槽位不足返回 false。</returns>
        public bool SetCount(int itemId, int count, int maxStackCount)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "物品数量不能小于 0。");
            }

            ValidateMaxStackCount(maxStackCount);

            InventoryData snapshot = Clone();
            ClearItem(itemId);

            int remaining = count <= 0 ? 0 : AddItem(itemId, count, maxStackCount);
            if (remaining == 0)
            {
                return true;
            }

            CopyFrom(snapshot);
            return false;
        }

        /// <summary>
        /// 设置指定索引的槽位数据，数量为 0 时会清空槽位。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">物品数量。</param>
        /// <param name="maxStackCount">单槽最大堆叠数量。</param>
        /// <returns>设置成功返回 true，索引无效返回 false。</returns>
        public bool SetSlot(int index, int itemId, int count, int maxStackCount)
        {
            ValidateMaxStackCount(maxStackCount);

            if (!IsValidIndex(index))
            {
                return false;
            }

            InventorySlotData slot = slots[index];
            if (count <= 0 || itemId <= 0)
            {
                slot.Clear();
                return true;
            }

            slot.Set(itemId, Math.Min(count, maxStackCount));
            return true;
        }

        /// <summary>
        /// 移动槽位。目标为空时移动，目标同类时合并，目标不同类时交换。
        /// </summary>
        /// <param name="fromIndex">来源槽位索引。</param>
        /// <param name="toIndex">目标槽位索引。</param>
        /// <param name="maxStackCount">单槽最大堆叠数量。</param>
        /// <returns>操作成功返回 true，索引无效或无法移动时返回 false。</returns>
        public bool MoveSlot(int fromIndex, int toIndex, int maxStackCount)
        {
            ValidateMaxStackCount(maxStackCount);

            if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex) || fromIndex == toIndex)
            {
                return false;
            }

            InventorySlotData from = slots[fromIndex];
            InventorySlotData to = slots[toIndex];
            if (from.IsEmpty)
            {
                return false;
            }

            if (to.IsEmpty)
            {
                to.Set(from.itemId, from.count);
                from.Clear();
                return true;
            }

            if (from.itemId == to.itemId)
            {
                return MergeSlots(fromIndex, toIndex, maxStackCount);
            }

            int tempItemId = to.itemId;
            int tempCount = to.count;
            to.Set(from.itemId, from.count);
            from.Set(tempItemId, tempCount);
            return true;
        }

        /// <summary>
        /// 将来源槽位尽量合并到目标槽位。
        /// </summary>
        /// <param name="fromIndex">来源槽位索引。</param>
        /// <param name="toIndex">目标槽位索引。</param>
        /// <param name="maxStackCount">单槽最大堆叠数量。</param>
        /// <returns>成功移动至少一个物品返回 true，否则返回 false。</returns>
        public bool MergeSlots(int fromIndex, int toIndex, int maxStackCount)
        {
            ValidateMaxStackCount(maxStackCount);

            if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex) || fromIndex == toIndex)
            {
                return false;
            }

            InventorySlotData from = slots[fromIndex];
            InventorySlotData to = slots[toIndex];
            if (from.IsEmpty)
            {
                return false;
            }

            if (to.IsEmpty)
            {
                to.Set(from.itemId, from.count);
                from.Clear();
                return true;
            }

            if (from.itemId != to.itemId || to.count >= maxStackCount)
            {
                return false;
            }

            int moveCount = Math.Min(maxStackCount - to.count, from.count);
            to.count += moveCount;
            from.count -= moveCount;

            if (from.count <= 0)
            {
                from.Clear();
            }

            return moveCount > 0;
        }

        /// <summary>
        /// 从一个槽位拆出指定数量到另一个槽位。
        /// </summary>
        /// <param name="fromIndex">来源槽位索引。</param>
        /// <param name="count">拆分数量。</param>
        /// <param name="toIndex">目标槽位索引。</param>
        /// <param name="maxStackCount">单槽最大堆叠数量。</param>
        /// <returns>拆分成功返回 true，条件不满足返回 false。</returns>
        public bool SplitSlot(int fromIndex, int count, int toIndex, int maxStackCount)
        {
            ValidatePositiveCount(count);
            ValidateMaxStackCount(maxStackCount);

            if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex) || fromIndex == toIndex)
            {
                return false;
            }

            InventorySlotData from = slots[fromIndex];
            InventorySlotData to = slots[toIndex];
            if (from.IsEmpty || from.count < count)
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

            return true;
        }

        /// <summary>
        /// 获取指定物品在所有槽位中的总数量。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <returns>背包中的物品总数量。</returns>
        public int GetCount(int itemId)
        {
            int total = 0;
            foreach (InventorySlotData slot in slots)
            {
                if (!slot.IsEmpty && slot.itemId == itemId)
                {
                    total += slot.count;
                }
            }

            return total;
        }

        /// <summary>
        /// 判断背包中是否拥有足够数量的指定物品。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">需要检查的数量。</param>
        /// <returns>数量足够返回 true，否则返回 false。</returns>
        public bool HasEnough(int itemId, int count)
        {
            ValidatePositiveCount(count);
            return GetCount(itemId) >= count;
        }

        /// <summary>
        /// 判断背包中是否存在指定物品。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <returns>存在至少一个该物品返回 true。</returns>
        public bool Contains(int itemId)
        {
            return GetCount(itemId) > 0;
        }

        /// <summary>
        /// 获取指定槽位的快照。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <returns>槽位快照。</returns>
        public InventorySlotData GetSlot(int index)
        {
            if (!IsValidIndex(index))
            {
                throw new ArgumentOutOfRangeException(nameof(index), "槽位索引超出范围。");
            }

            return slots[index].Clone();
        }

        /// <summary>
        /// 获取所有槽位的快照。
        /// </summary>
        /// <returns>槽位快照列表。</returns>
        public IReadOnlyList<InventorySlotData> GetSlotsSnapshot()
        {
            List<InventorySlotData> result = new List<InventorySlotData>(slots.Count);
            foreach (InventorySlotData slot in slots)
            {
                result.Add(slot.Clone());
            }

            return result;
        }

        /// <summary>
        /// 清空背包中的所有槽位。
        /// </summary>
        public void Clear()
        {
            foreach (InventorySlotData slot in slots)
            {
                slot.Clear();
            }
        }

        /// <summary>
        /// 清空指定物品的所有槽位。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        public void ClearItem(int itemId)
        {
            foreach (InventorySlotData slot in slots)
            {
                if (!slot.IsEmpty && slot.itemId == itemId)
                {
                    slot.Clear();
                }
            }
        }

        /// <summary>
        /// 复制另一个背包数据快照。
        /// </summary>
        /// <param name="source">来源背包数据。</param>
        public void CopyFrom(InventoryData source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            slots = new List<InventorySlotData>(source.slots.Count);
            foreach (InventorySlotData slot in source.slots)
            {
                slots.Add(slot == null ? new InventorySlotData() : slot.Clone());
            }
        }

        /// <summary>
        /// 创建当前背包数据的可序列化快照。
        /// </summary>
        /// <returns>新的背包数据实例。</returns>
        public InventoryData Clone()
        {
            InventoryData clone = new InventoryData();
            clone.CopyFrom(this);
            return clone;
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < slots.Count;
        }

        private static void ValidatePositiveCount(int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "物品数量必须大于 0。");
            }
        }

        private static void ValidateMaxStackCount(int maxStackCount)
        {
            if (maxStackCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxStackCount), "单槽堆叠上限必须大于 0。");
            }
        }
    }
}
