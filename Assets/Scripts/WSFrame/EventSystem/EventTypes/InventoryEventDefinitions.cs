namespace WS_Modules.CustomEventSystem
{
    /// <summary>
    /// Inventory 跨系统事件枚举。
    /// </summary>
    public enum E_InventoryEvent
    {
        start = EventIdRange.InventoryStart,
        DropWorldItemRequested = start + 1,
        end,
    }

    /// <summary>
    /// 请求世界生成背包丢弃物的事件参数。
    /// </summary>
    public readonly struct InventoryDropWorldItemEventArgs
    {
        /// <summary>
        /// 物品编号。
        /// </summary>
        public int ItemId { get; }

        /// <summary>
        /// 物品数量。
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// 创建请求世界生成背包丢弃物的事件参数。
        /// </summary>
        public InventoryDropWorldItemEventArgs(int itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }
    }
}