using GameData;
using Inventory;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 背包窗口 ViewModel，保留 Bag 特有用户意图。
    /// </summary>
    public sealed class InventoryBagViewModel : InventorySlotContainerViewModel
    {
        #region Fields
        private readonly ExpandableInventoryDataContainer container;
        #endregion

        #region Properties
        /// <summary>
        /// Bag 最大槽位容量。
        /// </summary>
        public int SlotCapacity => container.MaxCapacity;

        /// <summary>
        /// Bag 当前已解锁槽位数量。
        /// </summary>
        public int UnlockedSlotCount => container.Capacity;
        #endregion

        #region Constructor
        /// <summary>
        /// 创建 Bag ViewModel。
        /// </summary>
        /// <param name="container">可扩容 Bag 槽位容器。</param>
        /// <param name="itemDatabase">物品数据查询依赖。</param>
        public InventoryBagViewModel(ExpandableInventoryDataContainer container, IItemDatabase itemDatabase)
            : base(container, itemDatabase)
        {
            this.container = container;
        }
        #endregion
    }
}
