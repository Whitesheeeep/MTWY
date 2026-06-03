using System;
using System.Collections.Generic;
using GameData;
using Inventory;
using WS_Modules.CustomEventSystem;
using WS_Modules.MVVM;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 通用 Inventory 槽位容器 ViewModel，负责单个槽位容器的显示数据、选中状态和容器内操作。
    /// </summary>
    public class InventorySlotContainerViewModel : IViewModel, IUnRegisterList
    {
        #region Fields
        private readonly IInventorySlotContainer container;
        private readonly IItemDatabase itemDatabase;
        private readonly List<InventorySlotViewData> slots = new List<InventorySlotViewData>();
        private readonly List<IUnRegister> unRegisterList = new List<IUnRegister>();
        private int selectedSlotIndex = -1;
        #endregion

        #region Properties
        /// <summary>
        /// 当前槽位显示数据。
        /// </summary>
        public IReadOnlyList<InventorySlotViewData> Slots => slots;

        /// <summary>
        /// 容器容量。
        /// </summary>
        public int Capacity => container.Capacity;

        /// <summary>
        /// 当前槽位数量。
        /// </summary>
        public int SlotCount => container.SlotCount;

        /// <summary>
        /// 当前选中的槽位索引。
        /// </summary>
        public int SelectedSlotIndex => selectedSlotIndex;

        /// <summary>
        /// 当前 ViewModel 持有的事件注销句柄列表。
        /// </summary>
        public List<IUnRegister> UnRegisterList => unRegisterList;
        #endregion

        #region Events
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
        #endregion

        #region Constructor
        /// <summary>
        /// 创建通用槽位容器 ViewModel。
        /// </summary>
        /// <param name="container">槽位容器。</param>
        /// <param name="itemDatabase">物品数据查询依赖。</param>
        public InventorySlotContainerViewModel(
            IInventorySlotContainer container,
            IItemDatabase itemDatabase)
        {
            this.container = container ?? throw new ArgumentNullException(nameof(container));
            this.itemDatabase = itemDatabase ?? throw new ArgumentNullException(nameof(itemDatabase));

            this.container.RegisterSlotChanged(OnModelSlotChanged).AddToUnregisterList(this);
            this.container.RegisterSlotsChanged(OnModelSlotsChanged).AddToUnregisterList(this);
            RefreshSlotsFromModel();
        }
        #endregion

        #region Intent
        /// <summary>
        /// 选择槽位。
        /// </summary>
        /// <param name="index">槽位索引，传入负数表示取消选中。</param>
        public virtual void SelectSlot(int index)
        {
            if (selectedSlotIndex == index) return;

            selectedSlotIndex = index;
            SelectionChanged?.Invoke();
        }

        /// <summary>
        /// 移动容器内部槽位。
        /// </summary>
        /// <param name="fromIndex">来源槽位索引。</param>
        /// <param name="toIndex">目标槽位索引。</param>
        /// <returns>移动成功返回 true。</returns>
        public bool MoveSlot(int fromIndex, int toIndex)
        {
            return container.MoveSlot(fromIndex, toIndex);
        }

        /// <summary>
        /// 将当前容器中的槽位移动到另一个槽位容器中。
        /// </summary>
        /// <param name="target">目标槽位容器 ViewModel。</param>
        /// <param name="fromIndex">来源槽位索引。</param>
        /// <param name="toIndex">目标槽位索引。</param>
        /// <returns>移动成功返回 true。</returns>
        public bool MoveSlotTo(InventorySlotContainerViewModel target, int fromIndex, int toIndex)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            return InventorySlotTransferService.MoveSlot(container, fromIndex, target.container, toIndex);
        }

        /// <summary>
        /// 将指定槽位中的整格物品丢弃到世界中。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <returns>丢弃成功返回 true。</returns>
        public bool DropSlotToWorld(int index)
        {
            return InventorySlotWorldDropService.DropSlotToWorld(container, itemDatabase, index);
        }

        /// <summary>
        /// 获取指定槽位的物品提示窗口上下文。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <param name="context">物品提示窗口上下文。</param>
        /// <returns>成功获取返回 true。</returns>
        public bool TryGetItemTipContext(int index, out ItemTipContext context)
        {
            context = default;
            if (index < 0 || index >= slots.Count) return false;

            InventorySlotViewData slot = slots[index];
            if (slot.IsEmpty) return false;
            if (!itemDatabase.TryGet(slot.itemId, out ItemData itemData)) return false;

            context = new ItemTipContext(
                itemData.name,
                itemData.itemType.ToString(),
                itemData.description,
                itemData.price.ToString());
            return true;
        }

        /// <summary>
        /// 从一个槽位拆分指定数量到另一个槽位。
        /// </summary>
        /// <param name="fromIndex">来源槽位索引。</param>
        /// <param name="count">拆分数量。</param>
        /// <param name="toIndex">目标槽位索引。</param>
        /// <returns>拆分成功返回 true。</returns>
        public bool SplitSlot(int fromIndex, int count, int toIndex)
        {
            return container.SplitSlot(fromIndex, count, toIndex);
        }

        /// <summary>
        /// 清空容器槽位。
        /// </summary>
        public void Clear()
        {
            container.Clear();
        }
        #endregion

        #region LifeCycle
        /// <summary>
        /// 释放事件订阅。
        /// </summary>
        public void Dispose()
        {
            this.UnRegisterAll();
        }
        #endregion

        #region Model Events
        private void OnModelSlotChanged(InventorySlotChangedEventArgs eventArgs)
        {
            RefreshSlotFromModel(eventArgs.Index);
        }

        private void OnModelSlotsChanged(InventorySlotsChangedEventArgs eventArgs)
        {
            RefreshSlotsFromModel();
        }

        private void RefreshSlotFromModel(int index)
        {
            EnsureSlotListSize();
            if (index < 0 || index >= slots.Count) return;

            InventorySlotData slot = container.GetSlot(index);
            slots[index] = CreateViewData(index, slot);
            SlotChanged?.Invoke(index);
        }

        private void RefreshSlotsFromModel()
        {
            slots.Clear();
            IReadOnlyList<InventorySlotData> modelSlots = container.GetSlotsSnapshot();
            for (int i = 0; i < modelSlots.Count; i++)
                slots.Add(CreateViewData(i, modelSlots[i]));

            SlotsChanged?.Invoke();
        }

        private void EnsureSlotListSize()
        {
            int targetCount = container.SlotCount;
            while (slots.Count < targetCount)
                slots.Add(InventorySlotViewData.Empty(slots.Count));
        }
        #endregion

        #region ViewData
        private InventorySlotViewData CreateViewData(int index, InventorySlotData slot)
        {
            if (slot.IsEmpty) return InventorySlotViewData.Empty(index);

            ItemData itemData = itemDatabase.TryGet(slot.itemId, out ItemData result) ? result : null;
            return new InventorySlotViewData(index, slot.itemId, slot.count, itemData?.icon);
        }
        #endregion
    }
}
