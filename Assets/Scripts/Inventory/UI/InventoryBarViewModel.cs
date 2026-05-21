using System;
using System.Collections.Generic;
using GameData;
using Inventory;
using WS_Modules.CustomEventSystem;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 背包快捷栏 ViewModel，负责 Bar 显示数据、选中状态和用户意图入口。
    /// </summary>
    public sealed class InventoryBarViewModel : IDisposable, IUnRegisterList
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
        /// 当前 Bar 槽位显示数据。
        /// </summary>
        public IReadOnlyList<InventorySlotViewData> Slots => slots;

        /// <summary>
        /// 当前选中的 Bar 槽位索引。
        /// </summary>
        public int SelectedSlotIndex => selectedSlotIndex;

        /// <summary>
        /// 当前 ViewModel 持有的事件注销句柄列表。
        /// </summary>
        public List<IUnRegister> UnRegisterList => unRegisterList;

        /// <summary>
        /// 创建 Bar ViewModel。
        /// </summary>
        /// <param name="manager">背包管理器。</param>
        public InventoryBarViewModel(InventoryManager manager)
        {
            this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
            this.manager.RegisterBarSlotChanged(OnModelSlotChanged).AddToUnregisterList(this);
            this.manager.RegisterBarSlotsChanged(OnModelSlotsChanged).AddToUnregisterList(this);
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

            InventorySlotData slot = manager.GetBarSlot(index);
            slots[index] = CreateViewData(index, slot);
            SlotChanged?.Invoke(index);
        }

        /// <summary>
        /// 从数据层刷新全部 Bar 槽位显示数据。
        /// </summary>
        public void RefreshSlotsFromModel()
        {
            slots.Clear();
            IReadOnlyList<InventorySlotData> modelSlots = manager.GetBarSlots();
            for (int i = 0; i < modelSlots.Count; i++)
            {
                slots.Add(CreateViewData(i, modelSlots[i]));
            }

            SlotsChanged?.Invoke();
        }

        /// <summary>
        /// 选择 Bar 槽位。
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
        /// 移动 Bar 内部槽位。
        /// </summary>
        public bool MoveSlot(int fromIndex, int toIndex)
        {
            return manager.MoveBarSlot(fromIndex, toIndex);
        }

        /// <summary>
        /// 使用指定 Bar 槽位。
        /// </summary>
        public bool UseSlot(int index)
        {
            SelectSlot(index);
            return index >= 0 && index < slots.Count && !slots[index].IsEmpty;
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
            int targetCount = manager.BarCapacity;
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
