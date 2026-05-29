using System;

namespace Inventory
{
    /// <summary>
    /// Inventory 槽位容器转移服务，负责不同容器之间的整格移动、合并和交换。
    /// </summary>
    public static class InventorySlotTransferService
    {
        /// <summary>
        /// 将来源容器的槽位移动到目标容器的指定槽位。
        /// </summary>
        /// <param name="source">来源槽位容器。</param>
        /// <param name="sourceIndex">来源槽位索引。</param>
        /// <param name="target">目标槽位容器。</param>
        /// <param name="targetIndex">目标槽位索引。</param>
        /// <returns>移动成功返回 true。</returns>
        public static bool MoveSlot(
            IInventorySlotContainer source,
            int sourceIndex,
            IInventorySlotContainer target,
            int targetIndex)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (ReferenceEquals(source, target)) return source.MoveSlot(sourceIndex, targetIndex);
            if (!IsValidSlot(source, sourceIndex) || !IsValidSlot(target, targetIndex)) return false;

            InventorySlotData sourceSlot = source.GetSlot(sourceIndex);
            InventorySlotData targetSlot = target.GetSlot(targetIndex);
            if (sourceSlot.IsEmpty) return false;

            if (targetSlot.IsEmpty)
                return MoveToEmptySlot(source, sourceIndex, sourceSlot, target, targetIndex, targetSlot);

            if (sourceSlot.itemId == targetSlot.itemId && targetSlot.count < InventoryConstants.MaxStackCount)
                return MergeToSameItemSlot(source, sourceIndex, sourceSlot, target, targetIndex, targetSlot);

            return SwapSlots(source, sourceIndex, sourceSlot, target, targetIndex, targetSlot);
        }

        /// <summary>
        /// 将来源容器槽位中的指定数量拆分到目标容器槽位。
        /// </summary>
        /// <param name="source">来源槽位容器。</param>
        /// <param name="sourceIndex">来源槽位索引。</param>
        /// <param name="count">拆分数量。</param>
        /// <param name="target">目标槽位容器。</param>
        /// <param name="targetIndex">目标槽位索引。</param>
        /// <returns>拆分成功返回 true。</returns>
        public static bool SplitSlot(
            IInventorySlotContainer source,
            int sourceIndex,
            int count,
            IInventorySlotContainer target,
            int targetIndex)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (ReferenceEquals(source, target)) return source.SplitSlot(sourceIndex, count, targetIndex);
            if (count <= 0 || !IsValidSlot(source, sourceIndex) || !IsValidSlot(target, targetIndex)) return false;

            InventorySlotData sourceSlot = source.GetSlot(sourceIndex);
            InventorySlotData targetSlot = target.GetSlot(targetIndex);
            if (sourceSlot.IsEmpty || sourceSlot.count < count) return false;
            if (!targetSlot.IsEmpty && (targetSlot.itemId != sourceSlot.itemId || targetSlot.count >= InventoryConstants.MaxStackCount)) return false;

            int targetCanReceive = targetSlot.IsEmpty
                ? InventoryConstants.MaxStackCount
                : InventoryConstants.MaxStackCount - targetSlot.count;
            if (targetCanReceive < count) return false;

            InventorySlotData sourceSnapshot = sourceSlot.Clone();
            InventorySlotData targetSnapshot = targetSlot.Clone();
            sourceSlot.count -= count;

            if (targetSlot.IsEmpty) targetSlot.Set(sourceSnapshot.itemId, count);
            else targetSlot.count += count;

            bool targetApplied = ApplySlot(target, targetIndex, targetSlot);
            bool sourceApplied = targetApplied && (sourceSlot.count <= 0
                ? ClearSlot(source, sourceIndex)
                : ApplySlot(source, sourceIndex, sourceSlot));
            if (sourceApplied) return true;

            ApplySlot(target, targetIndex, targetSnapshot);
            ApplySlot(source, sourceIndex, sourceSnapshot);
            return false;
        }

        private static bool MoveToEmptySlot(
            IInventorySlotContainer source,
            int sourceIndex,
            InventorySlotData sourceSlot,
            IInventorySlotContainer target,
            int targetIndex,
            InventorySlotData targetSnapshot)
        {
            if (!ApplySlot(target, targetIndex, sourceSlot)) return false;
            if (ClearSlot(source, sourceIndex)) return true;

            ApplySlot(target, targetIndex, targetSnapshot);
            return false;
        }

        private static bool MergeToSameItemSlot(
            IInventorySlotContainer source,
            int sourceIndex,
            InventorySlotData sourceSlot,
            IInventorySlotContainer target,
            int targetIndex,
            InventorySlotData targetSlot)
        {
            int moveCount = Math.Min(InventoryConstants.MaxStackCount - targetSlot.count, sourceSlot.count);
            if (moveCount <= 0) return false;

            InventorySlotData sourceSnapshot = sourceSlot.Clone();
            InventorySlotData targetSnapshot = targetSlot.Clone();
            targetSlot.count += moveCount;
            sourceSlot.count -= moveCount;

            if (!ApplySlot(target, targetIndex, targetSlot)) return false;
            bool sourceApplied = sourceSlot.count <= 0
                ? ClearSlot(source, sourceIndex)
                : ApplySlot(source, sourceIndex, sourceSlot);
            if (sourceApplied) return true;

            ApplySlot(target, targetIndex, targetSnapshot);
            ApplySlot(source, sourceIndex, sourceSnapshot);
            return false;
        }

        private static bool SwapSlots(
            IInventorySlotContainer source,
            int sourceIndex,
            InventorySlotData sourceSlot,
            IInventorySlotContainer target,
            int targetIndex,
            InventorySlotData targetSlot)
        {
            if (!ApplySlot(target, targetIndex, sourceSlot)) return false;
            if (ApplySlot(source, sourceIndex, targetSlot)) return true;

            ApplySlot(target, targetIndex, targetSlot);
            return false;
        }

        private static bool IsValidSlot(IInventorySlotContainer container, int index)
        {
            return index >= 0 && index < container.SlotCount;
        }

        private static bool ApplySlot(IInventorySlotContainer container, int index, InventorySlotData slot)
        {
            if (slot == null || slot.IsEmpty) return ClearSlot(container, index);

            return container.SetSlot(index, slot.itemId, slot.count);
        }

        private static bool ClearSlot(IInventorySlotContainer container, int index)
        {
            return container.SetSlot(index, 0, 0);
        }
    }
}
