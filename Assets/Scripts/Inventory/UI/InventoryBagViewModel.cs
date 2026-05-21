using System;
using System.Collections.Generic;
using GameData;
using Inventory;
using WS_Modules.CustomEventSystem;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 背包窗口 ViewModel，负责 Bag 显示数据、选中状态和用户意图入口。
    /// </summary>
    public sealed class InventoryBagViewModel : IDisposable, IUnRegisterList
    {
        private readonly InventoryManager manager;
        private readonly List<InventorySlotViewData> slots = new List<InventorySlotViewData>();
        private readonly List<IUnRegister> unRegisterList = new List<IUnRegister>();
        private int selectedSlotIndex = -1;

        /// <summary>
        /// 单个槽位显示数据变化时触发。
        /// </summary>
        public event Action<int> SlotChanged;

        /// <summary>
        /// 槽位列表需要整体刷新时触发。
        /// </summary>
        public event Action SlotsChanged;

        /// <summary>
        /// 选中槽位变化时触发。
        /// </summary>
        public event Action SelectionChanged;

        /// <summary>
        /// 当前 Bag 槽位显示数据。
        /// </summary>
        public IReadOnlyList<InventorySlotViewData> Slots => slots;

        /// <summary>
        /// 当前选中的 Bag 槽位索引。
        /// </summary>
        public int SelectedSlotIndex => selectedSlotIndex;

        /// <summary>
        /// 当前 ViewModel 持有的事件注销句柄列表。
        /// </summary>
        public List<IUnRegister> UnRegisterList => unRegisterList;

        /// <summary>
        /// 创建 Bag ViewModel。
        /// </summary>
        /// <param name="manager">背包管理器。</param>
        public InventoryBagViewModel(InventoryManager manager)
        {
            this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
            this.manager.RegisterBagSlotChanged(OnModelSlotChanged).AddToUnregisterList(this);
            this.manager.RegisterBagSlotsChanged(OnModelSlotsChanged).AddToUnregisterList(this);
            RefreshSlotsFromModel();
        }

        /// <summary>
        /// 从数据层刷新指定槽位显示数据。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        public void RefreshSlotFromModel(int index)
        {
            EnsureSlotListSize();
            if (index < 0 || index >= slots.Count)
            {
                return;
            }

            InventorySlotData slot = manager.GetBagSlot(index);
            slots[index] = CreateViewData(index, slot);
            SlotChanged?.Invoke(index);
        }

        /// <summary>
        /// 从数据层刷新全部 Bag 槽位显示数据。
        /// </summary>
        public void RefreshSlotsFromModel()
        {
            slots.Clear();
            IReadOnlyList<InventorySlotData> modelSlots = manager.GetBagSlots();
            for (int i = 0; i < modelSlots.Count; i++)
            {
                slots.Add(CreateViewData(i, modelSlots[i]));
            }

            SlotsChanged?.Invoke();
        }

        /// <summary>
        /// 选择 Bag 槽位。
        /// </summary>
        /// <param name="index">槽位索引，传入负数表示取消选中。</param>
        public void SelectSlot(int index)
        {
            if (selectedSlotIndex == index)
            {
                return;
            }

            selectedSlotIndex = index;
            SelectionChanged?.Invoke();
        }

        /// <summary>
        /// 移动 Bag 内部槽位。
        /// </summary>
        public bool MoveSlot(int fromIndex, int toIndex)
        {
            return manager.MoveBagSlot(fromIndex, toIndex);
        }

        /// <summary>
        /// 从 Bag 槽位拆分指定数量到另一个 Bag 槽位。
        /// </summary>
        public bool SplitSlot(int fromIndex, int count, int toIndex)
        {
            return manager.SplitBagSlot(fromIndex, count, toIndex);
        }

        /// <summary>
        /// 将 Bag 槽位移动到 Bar 槽位。
        /// </summary>
        public bool MoveToBar(int bagIndex, int barIndex)
        {
            return manager.MoveBagToBar(bagIndex, barIndex);
        }

        /// <summary>
        /// 释放事件订阅。
        /// </summary>
        public void Dispose()
        {
            this.UnRegisterAll();
        }

        private void OnModelSlotChanged(InventorySlotChangedEventArgs eventArgs)
        {
            RefreshSlotFromModel(eventArgs.Index);
        }

        private void OnModelSlotsChanged(InventorySlotsChangedEventArgs eventArgs)
        {
            RefreshSlotsFromModel();
        }

        private void EnsureSlotListSize()
        {
            int targetCount = manager.BagCapacity;
            while (slots.Count < targetCount)
            {
                slots.Add(InventorySlotViewData.Empty(slots.Count));
            }
        }

        private InventorySlotViewData CreateViewData(int index, InventorySlotData slot)
        {
            return InventorySlotViewDataMapper.Create(index, slot.itemId, slot.count, GetItemData);
        }

        private ItemData GetItemData(int itemId)
        {
            return manager.TryGetItemData(itemId, out ItemData itemData) ? itemData : null;
        }
    }
}
