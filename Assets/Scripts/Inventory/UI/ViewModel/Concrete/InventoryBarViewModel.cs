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

        public override void SelectSlot(int index)
        {
            base.SelectSlot(index);
            // 触发全局快捷栏槽位选中事件，携带选中槽位索引和物品 ID 供外部系统使用。
            int itemId = index >= 0 && index < Slots.Count && !Slots[index].IsEmpty ? Slots[index].itemId : -1;
            // Debug.Log(itemId);
            EventSystem.EventTrigger_Int(
                (int)E_InventoryEvent.BarSlotSelected,
                new InventoryBarSlotSelectedEventArgs(itemId));
        }
    }
}
