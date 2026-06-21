using GameData;
using Inventory;
using UnityEngine;
using WS_Modules.CustomEventSystem;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 背包快捷栏 ViewModel，保留 Bar 特有用户意图。
    /// </summary>
    public sealed class InventoryBarViewModel : InventorySlotContainerViewModel
    {
        /// <summary>
        /// 创建 Bar ViewModel。
        /// </summary>
        /// <param name="container">Bar 槽位容器。</param>
        /// <param name="itemDatabase">物品数据查询依赖。</param>
        public InventoryBarViewModel(IInventorySlotContainer container, IItemDatabase itemDatabase)
            : base(container, itemDatabase)
        {
        }

        /// <summary>
        /// 使用指定 Bar 槽位。
        /// </summary>
        /// <param name="index">Bar 槽位索引。</param>
        /// <returns>槽位存在物品时返回 true。</returns>
        public bool UseSlot(int index)
        {
            SelectSlot(index);
            return index >= 0 && index < Slots.Count && !Slots[index].IsEmpty;
        }

        /// <summary>
        /// 选择快捷栏槽位，并向外同步当前槽位的最新物品。
        /// </summary>
        /// <param name="index">快捷栏槽位索引。</param>
        public override void SelectSlot(int index)
        {
            base.SelectSlot(index);
            TriggerSelectedSlotEvent();
        }

        // 当快捷栏底层槽位变化时，同步选中槽位的最新物品给 Player。
        protected override void OnModelSlotChanged(InventorySlotChangedEventArgs eventArgs)
        {
            base.OnModelSlotChanged(eventArgs);

            if (eventArgs.Index == SelectedSlotIndex)
            {
                TriggerSelectedSlotEvent();
            }
        }

        // 触发全局快捷栏槽位选中事件，携带当前选中槽位的最新物品 ID。
        private void TriggerSelectedSlotEvent()
        {
            int itemId = SelectedSlotIndex >= 0 && SelectedSlotIndex < Slots.Count && !Slots[SelectedSlotIndex].IsEmpty
                ? Slots[SelectedSlotIndex].itemId
                : -1;

            EventSystem.EventTrigger_Int(
                (int)E_InventoryEvent.BarSlotSelected,
                new InventoryBarSlotSelectedEventArgs(itemId));
        }
    }
}
