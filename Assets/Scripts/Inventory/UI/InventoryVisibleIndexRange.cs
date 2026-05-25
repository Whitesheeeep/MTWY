namespace WS_Modules.UIModule
{
    /// <summary>
    /// 背包虚拟滚动当前需要渲染的槽位索引范围。
    /// </summary>
    public readonly struct InventoryVisibleIndexRange
    {
        /// <summary>
        /// 空范围。
        /// </summary>
        public static readonly InventoryVisibleIndexRange Empty = new InventoryVisibleIndexRange(-1, -1);

        /// <summary>
        /// 起始索引。
        /// </summary>
        public int StartIndex { get; }

        /// <summary>
        /// 结束索引。
        /// </summary>
        public int EndIndex { get; }

        /// <summary>
        /// 范围是否有效。
        /// </summary>
        public bool IsValid => StartIndex >= 0 && EndIndex >= StartIndex;

        /// <summary>
        /// 创建可见索引范围。
        /// </summary>
        /// <param name="startIndex">起始索引。</param>
        /// <param name="endIndex">结束索引。</param>
        public InventoryVisibleIndexRange(int startIndex, int endIndex)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
        }

        /// <summary>
        /// 判断索引是否在当前范围内。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <returns>在范围内返回 true。</returns>
        public bool Contains(int index)
        {
            return IsValid && index >= StartIndex && index <= EndIndex;
        }
    }
}
