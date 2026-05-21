namespace Inventory
{
    /// <summary>
    /// Inventory 单个槽位变化事件参数。
    /// </summary>
    public readonly struct InventorySlotChangedEventArgs
    {
        /// <summary>
        /// 发生变化的槽位索引。
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// 创建单个槽位变化事件参数。
        /// </summary>
        /// <param name="index">发生变化的槽位索引。</param>
        public InventorySlotChangedEventArgs(int index)
        {
            Index = index;
        }
    }

    /// <summary>
    /// Inventory 槽位列表整体变化事件参数。
    /// </summary>
    public readonly struct InventorySlotsChangedEventArgs
    {
        /// <summary>
        /// 默认整体变化事件参数。
        /// </summary>
        public static readonly InventorySlotsChangedEventArgs Default = new InventorySlotsChangedEventArgs();
    }
}
