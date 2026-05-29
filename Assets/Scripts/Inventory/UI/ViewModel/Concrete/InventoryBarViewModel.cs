using GameData;
using Inventory;

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
    }
}
