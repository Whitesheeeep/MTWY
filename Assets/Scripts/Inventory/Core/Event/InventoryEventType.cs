namespace Inventory
{
    /// <summary>
    /// InventoryManager 内部事件类型，用作模块内事件中心的事件 key。
    /// </summary>
    public enum InventoryEventType
    {
        /// <summary>
        /// Bar 中单个槽位数据发生变化。
        /// </summary>
        BarSlotChanged = 1,

        /// <summary>
        /// Bag 中单个槽位数据发生变化。
        /// </summary>
        BagSlotChanged,

        /// <summary>
        /// Bar 槽位列表需要整体刷新。
        /// </summary>
        BarSlotsChanged,

        /// <summary>
        /// Bag 槽位列表需要整体刷新。
        /// </summary>
        BagSlotsChanged,
    }
}
