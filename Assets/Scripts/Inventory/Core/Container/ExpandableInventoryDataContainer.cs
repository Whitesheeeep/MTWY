using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Inventory
{
    /// <summary>
    /// 可扩充的 Inventory 槽位容器，额外记录最大容量并限制扩容上限。
    /// </summary>
    [Serializable]
    public sealed class ExpandableInventoryDataContainer : InventoryDataContainer
    {
        [LabelText("最大容量")]
        [MinValue(0)]
        [OnValueChanged(nameof(OnMaxCapacityChanged))]
        [SerializeField] private int maxCapacity = 60;

        /// <summary>
        /// 创建可扩充槽位容器。
        /// </summary>
        public ExpandableInventoryDataContainer()
        {
            NormalizeMaxCapacity(maxCapacity);
        }

        /// <summary>
        /// 创建指定容量和最大容量的可扩充槽位容器。
        /// </summary>
        /// <param name="capacity">当前容量。</param>
        /// <param name="maxCapacity">最大容量。</param>
        public ExpandableInventoryDataContainer(int capacity, int maxCapacity)
            : base(capacity)
        {
            NormalizeMaxCapacity(maxCapacity);
        }

        /// <summary>
        /// 最大容量。
        /// </summary>
        public int MaxCapacity => maxCapacity;

        /// <inheritdoc />
        public override void NormalizeCapacity(int capacity)
        {
            base.NormalizeCapacity(capacity);
            maxCapacity = Mathf.Max(Capacity, maxCapacity);
        }

        /// <summary>
        /// 整理最大容量，保证最大容量不小于当前容量。
        /// </summary>
        /// <param name="maxCapacity">目标最大容量。</param>
        public void NormalizeMaxCapacity(int maxCapacity)
        {
            this.maxCapacity = Mathf.Max(Capacity, maxCapacity);
        }

        /// <summary>
        /// 扩展当前容量。
        /// </summary>
        /// <param name="additionalSlotCount">新增槽位数量。</param>
        public void ExpandCapacity(int additionalSlotCount)
        {
            if (additionalSlotCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(additionalSlotCount), "扩容槽位数量必须大于 0。");
            }

            if (additionalSlotCount > 0 && Capacity + additionalSlotCount > maxCapacity)
            {
                throw new InvalidOperationException("[ExpandableInventoryDataContainer] 扩容不能超过最大容量。");
            }

            NormalizeCapacity(Capacity + additionalSlotCount);
            NotifyAllChanged();
        }

        /// <summary>
        /// 尝试扩展当前容量，不允许超过最大容量。
        /// </summary>
        /// <param name="additionalSlotCount">新增槽位数量。</param>
        /// <returns>扩容成功返回 true。</returns>
        public bool TryExpandCapacity(int additionalSlotCount)
        {
            if (additionalSlotCount <= 0 || Capacity + additionalSlotCount > maxCapacity) return false;

            ExpandCapacity(additionalSlotCount);
            return true;
        }

        private void OnMaxCapacityChanged()
        {
            NormalizeMaxCapacity(maxCapacity);
        }
    }
}
