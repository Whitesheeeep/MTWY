namespace Inventory
{
    /// <summary>
    /// 单个 Inventory 槽位容器内部事件类型。
    /// </summary>
    internal enum InventoryContainerEventType
    {
        /// <summary>
        /// 单个槽位数据发生变化。
        /// </summary>
        SlotChanged = 1,

        /// <summary>
        /// 槽位列表需要整体刷新。
        /// </summary>
        SlotsChanged,
    }
}
