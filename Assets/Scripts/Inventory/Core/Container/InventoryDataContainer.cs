using System;
using System.Collections.Generic;
using GameData;
using Sirenix.OdinInspector;
using UnityEngine;
using WS_Modules.CustomEventSystem;

namespace Inventory
{
    /// <summary>
    /// 基于 InventoryData 的槽位容器实现，负责单个容器的数据存储、数据操作与变更通知。
    /// </summary>
    [Serializable]
    public class InventoryDataContainer : IInventorySlotContainer
    {
        [LabelText("容量")]
        [MinValue(0)]
        [OnValueChanged(nameof(OnCapacityChanged))]
        [SerializeField] private int capacity;

        [LabelText("槽位数据")]
        [ReadOnly]
        [FoldoutGroup("数据")]
        [SerializeField] private InventoryData data = new InventoryData();

        [NonSerialized] private IItemDatabase itemDatabase;
        [NonSerialized] private IEventCenter<int> eventModule;

        /// <summary>
        /// 创建空槽位容器。
        /// </summary>
        public InventoryDataContainer()
        {
            NormalizeCapacity(capacity);
        }

        /// <summary>
        /// 创建指定容量的槽位容器。
        /// </summary>
        /// <param name="capacity">容器容量。</param>
        public InventoryDataContainer(int capacity)
        {
            NormalizeCapacity(capacity);
        }

        /// <inheritdoc />
        public int Capacity => capacity;

        /// <inheritdoc />
        public int SlotCount => Data.SlotCount;

        /// <summary>
        /// 容器持有的底层槽位数据。
        /// </summary>
        internal InventoryData Data
        {
            get
            {
                data ??= new InventoryData();
                return data;
            }
        }

        /// <summary>
        /// 整理容器容量，并同步底层槽位数据数量。
        /// </summary>
        /// <param name="capacity">目标容量。</param>
        public virtual void NormalizeCapacity(int capacity)
        {
            this.capacity = Mathf.Max(0, capacity);
            Data.NormalizeCapacity(this.capacity);
        }

        /// <summary>
        /// 初始化运行时依赖。
        /// </summary>
        /// <param name="itemDatabase">物品数据库依赖。</param>
        internal void InitializeRuntime(IItemDatabase itemDatabase)
        {
            this.itemDatabase = itemDatabase ?? throw new ArgumentNullException(nameof(itemDatabase));
            EnsureEventModule();
        }

        /// <inheritdoc />
        public InventorySlotData GetSlot(int index) => Data.GetSlot(index);

        /// <inheritdoc />
        public IReadOnlyList<InventorySlotData> GetSlotsSnapshot() => Data.GetSlotsSnapshot();

        /// <inheritdoc />
        public bool Contains(int itemId) => Data.Contains(itemId);

        /// <inheritdoc />
        public int GetCount(int itemId) => Data.GetCount(itemId);

        /// <inheritdoc />
        public int AddItem(int itemId, int count)
        {
            EnsureItemExists(itemId);
            List<int> changed = new List<int>();
            int remaining = Data.AddItem(itemId, count, InventoryConstants.MaxStackCount, changed);
            NotifySlotsChanged(changed);
            return remaining;
        }

        /// <inheritdoc />
        public bool TryAddItem(int itemId, int count)
        {
            EnsureItemExists(itemId);
            InventoryData snapshot = Data.Clone();
            List<int> changed = new List<int>();
            int remaining = Data.AddItem(itemId, count, InventoryConstants.MaxStackCount, changed);
            if (remaining == 0)
            {
                NotifySlotsChanged(changed);
                return true;
            }

            Data.CopyFrom(snapshot);
            return false;
        }

        /// <inheritdoc />
        public bool RemoveItem(int itemId, int count)
        {
            EnsureItemExists(itemId);
            List<int> changed = new List<int>();
            bool success = Data.RemoveItem(itemId, count, changed);
            if (success)
            {
                NotifySlotsChanged(changed);
            }

            return success;
        }

        /// <inheritdoc />
        public bool RemoveFromSlot(int index, int count)
        {
            List<int> changed = new List<int>();
            bool success = Data.RemoveFromSlot(index, count, changed);
            if (success)
            {
                NotifySlotsChanged(changed);
            }

            return success;
        }

        /// <inheritdoc />
        public bool SetCount(int itemId, int count)
        {
            EnsureItemExists(itemId);
            List<int> changed = new List<int>();
            bool success = Data.SetCount(itemId, count, InventoryConstants.MaxStackCount, changed);
            if (success)
            {
                NotifySlotsChanged(changed);
            }

            return success;
        }

        /// <inheritdoc />
        public bool SetSlot(int index, int itemId, int count)
        {
            if (itemId > 0 && count > 0)
                EnsureItemExists(itemId);

            List<int> changed = new List<int>();
            bool success = Data.SetSlot(index, itemId, count, InventoryConstants.MaxStackCount, changed);
            if (success)
            {
                NotifySlotsChanged(changed);
            }

            return success;
        }

        /// <inheritdoc />
        public bool MoveSlot(int fromIndex, int toIndex)
        {
            List<int> changed = new List<int>();
            bool success = Data.MoveSlot(fromIndex, toIndex, InventoryConstants.MaxStackCount, changed);
            if (success)
            {
                NotifySlotsChanged(changed);
            }

            return success;
        }

        /// <inheritdoc />
        public bool MergeSlots(int fromIndex, int toIndex)
        {
            List<int> changed = new List<int>();
            bool success = Data.MergeSlots(fromIndex, toIndex, InventoryConstants.MaxStackCount, changed);
            if (success)
            {
                NotifySlotsChanged(changed);
            }

            return success;
        }

        /// <inheritdoc />
        public bool SplitSlot(int fromIndex, int count, int toIndex)
        {
            List<int> changed = new List<int>();
            bool success = Data.SplitSlot(fromIndex, count, toIndex, InventoryConstants.MaxStackCount, changed);
            if (success)
            {
                NotifySlotsChanged(changed);
            }

            return success;
        }

        /// <inheritdoc />
        public void Clear()
        {
            Data.Clear(null);
            NotifyAllChanged();
        }

        /// <inheritdoc />
        public IUnRegister RegisterSlotChanged(Action<InventorySlotChangedEventArgs> handler)
        {
            EnsureEventModule();
            return eventModule.Register((int)InventoryContainerEventType.SlotChanged, handler);
        }

        /// <inheritdoc />
        public IUnRegister RegisterSlotsChanged(Action<InventorySlotsChangedEventArgs> handler)
        {
            EnsureEventModule();
            return eventModule.Register((int)InventoryContainerEventType.SlotsChanged, handler);
        }

        /// <summary>
        /// 触发多个槽位变化事件，供跨容器调度完成后同步容器事件。
        /// </summary>
        /// <param name="indices">发生变化的槽位索引。</param>
        internal void NotifySlotsChanged(IEnumerable<int> indices)
        {
            if (indices == null) return;

            EnsureEventModule();
            foreach (int index in indices)
            {
                eventModule.EventTrigger(
                    (int)InventoryContainerEventType.SlotChanged,
                    new InventorySlotChangedEventArgs(index));
            }
        }

        /// <summary>
        /// 触发槽位列表整体变化事件。
        /// </summary>
        internal void NotifyAllChanged()
        {
            EnsureEventModule();
            eventModule.EventTrigger(
                (int)InventoryContainerEventType.SlotsChanged,
                InventorySlotsChangedEventArgs.Default);
        }

        private void EnsureItemExists(int itemId)
        {
            if (itemDatabase == null)
            {
                throw new InvalidOperationException("[InventoryDataContainer] 运行时物品数据库尚未初始化。");
            }

            if (!itemDatabase.TryGet(itemId, out _))
            {
                throw new KeyNotFoundException($"[InventoryDataContainer] 未找到物品配置，itemId: {itemId}");
            }
        }

        private void EnsureEventModule()
        {
            eventModule ??= new EventCenterModule<int>();
        }

        private void OnCapacityChanged()
        {
            NormalizeCapacity(capacity);
        }
    }
}
