namespace WS_Modules.UIModule
{
    /// <summary>
    /// 背包 UI 当前拖拽状态，用于跨窗口识别拖拽来源。
    /// </summary>
    public static class InventorySlotDragState
    {
        /// <summary>
        /// 当前是否存在有效拖拽来源。
        /// </summary>
        public static bool HasActiveDrag { get; private set; }

        /// <summary>
        /// 当前拖拽来源区域。
        /// </summary>
        public static InventorySlotArea SourceArea { get; private set; } = InventorySlotArea.None;

        /// <summary>
        /// 当前拖拽来源槽位索引。
        /// </summary>
        public static int SourceIndex { get; private set; } = -1;

        /// <summary>
        /// 当前拖拽是否已经释放到有效槽位。
        /// </summary>
        public static bool DropHandled { get; private set; }

        /// <summary>
        /// 开始记录拖拽来源。
        /// </summary>
        public static void BeginDrag(InventorySlotArea sourceArea, int sourceIndex)
        {
            SourceArea = sourceArea;
            SourceIndex = sourceIndex;
            HasActiveDrag = sourceArea != InventorySlotArea.None && sourceIndex >= 0;
            DropHandled = false;
        }

        /// <summary>
        /// 标记当前拖拽已经被槽位接收。
        /// </summary>
        public static void MarkDropHandled()
        {
            DropHandled = true;
        }

        /// <summary>
        /// 清理当前拖拽状态。
        /// </summary>
        public static void EndDrag()
        {
            SourceArea = InventorySlotArea.None;
            SourceIndex = -1;
            HasActiveDrag = false;
            DropHandled = false;
        }
    }
}
