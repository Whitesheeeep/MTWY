using UnityEngine;
using UnityEngine.UI;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 背包网格滚动范围计算器，负责根据手动布局参数推导虚拟渲染范围。
    /// </summary>
    public sealed class InventoryGridScrollCalculator
    {
        private const int DefaultExtraRowCount = 2;

        /// <summary>
        /// 可视窗口外额外保留的缓冲行数。
        /// </summary>
        public int ExtraRowCount { get; set; } = DefaultExtraRowCount;

        /// <summary>
        /// 根据已解锁槽位数量计算 Content 高度。
        /// </summary>
        /// <param name="unlockedSlotCount">已解锁槽位数量。</param>
        /// <param name="layout">手动网格布局参数。</param>
        /// <param name="viewport">滚动视口。</param>
        /// <returns>Content 应设置的高度。</returns>
        public float CalculateContentHeight(
            int unlockedSlotCount,
            InventoryManualGridLayout layout,
            RectTransform viewport)
        {
            if (unlockedSlotCount <= 0) return 0f;

            int columnCount = GetColumnCount(layout, viewport, unlockedSlotCount);
            int rowCount = Mathf.CeilToInt(unlockedSlotCount / (float)columnCount);
            return layout.PaddingTop + layout.PaddingBottom +
                   layout.SlotSize.y * rowCount +
                   layout.SlotSpacing.y * Mathf.Max(0, rowCount - 1);
        }

        /// <summary>
        /// 计算当前滚动位置需要渲染的槽位索引范围。
        /// </summary>
        /// <param name="unlockedSlotCount">已解锁槽位数量。</param>
        /// <param name="scrollRect">滚动组件。</param>
        /// <param name="contentRoot">滚动内容节点。</param>
        /// <param name="layout">手动网格布局参数。</param>
        /// <returns>需要渲染的槽位索引范围。</returns>
        public InventoryVisibleIndexRange CalculateVisibleRange(
            int unlockedSlotCount,
            ScrollRect scrollRect,
            RectTransform contentRoot,
            InventoryManualGridLayout layout)
        {
            if (unlockedSlotCount <= 0 || scrollRect == null || contentRoot == null)
                return InventoryVisibleIndexRange.Empty;

            RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();
            if (viewport == null) return InventoryVisibleIndexRange.Empty;

            int columnCount = GetColumnCount(layout, viewport, unlockedSlotCount);
            int totalRowCount = Mathf.CeilToInt(unlockedSlotCount / (float)columnCount);
            float viewportHeight = viewport.rect.height;
            float contentHeight = CalculateContentHeight(unlockedSlotCount, layout, viewport);
            if (contentHeight <= viewportHeight) return new InventoryVisibleIndexRange(0, unlockedSlotCount - 1);

            float scrollY = Mathf.Max(0f, contentRoot.anchoredPosition.y);
            float rowHeight = GetRowHeight(layout);
            int firstRow = Mathf.FloorToInt((scrollY - layout.PaddingTop) / rowHeight);
            int lastRow = Mathf.CeilToInt((scrollY + viewportHeight - layout.PaddingTop) / rowHeight) - 1;

            firstRow = Mathf.Clamp(firstRow - Mathf.Max(0, ExtraRowCount), 0, totalRowCount - 1);
            lastRow = Mathf.Clamp(lastRow + Mathf.Max(0, ExtraRowCount), firstRow, totalRowCount - 1);

            int startIndex = firstRow * columnCount;
            int endIndex = Mathf.Min(unlockedSlotCount - 1, ((lastRow + 1) * columnCount) - 1);
            return new InventoryVisibleIndexRange(startIndex, endIndex);
        }

        /// <summary>
        /// 计算指定槽位索引在 Content 下的锚点位置。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <param name="layout">手动网格布局参数。</param>
        /// <param name="viewport">滚动视口。</param>
        /// <param name="totalCount">总槽位数量。</param>
        /// <returns>槽位锚点位置。</returns>
        public Vector2 CalculateSlotPosition(
            int index,
            InventoryManualGridLayout layout,
            RectTransform viewport,
            int totalCount)
        {
            int columnCount = GetColumnCount(layout, viewport, totalCount);
            float spacingX = GetHorizontalSpacing(layout, viewport, columnCount);
            int row = index / columnCount;
            int column = index % columnCount;
            float x = layout.PaddingLeft + column * (layout.SlotSize.x + spacingX);
            float y = -layout.PaddingTop - row * (layout.SlotSize.y + layout.SlotSpacing.y);
            return new Vector2(x, y);
        }

        private static float GetRowHeight(InventoryManualGridLayout layout)
        {
            return Mathf.Max(1f, layout.SlotSize.y + layout.SlotSpacing.y);
        }

        private static int GetColumnCount(InventoryManualGridLayout layout, RectTransform viewport, int totalCount)
        {
            if (layout.FixedColumnCount > 0) return Mathf.Max(1, layout.FixedColumnCount);

            float viewportWidth = viewport != null ? viewport.rect.width : 0f;
            float availableWidth = Mathf.Max(0f, viewportWidth - layout.PaddingLeft - layout.PaddingRight);
            float columnWidth = Mathf.Max(1f, layout.SlotSize.x + layout.SlotSpacing.x);
            return Mathf.Max(1, Mathf.Min(totalCount, Mathf.FloorToInt((availableWidth + layout.SlotSpacing.x) / columnWidth)));
        }

        private static float GetHorizontalSpacing(InventoryManualGridLayout layout, RectTransform viewport, int columnCount)
        {
            if (!layout.AutoFillHorizontalSpacing) return layout.SlotSpacing.x;
            if (columnCount <= 1) return 0f;

            float viewportWidth = viewport != null ? viewport.rect.width : 0f;
            float availableWidth = Mathf.Max(0f, viewportWidth - layout.PaddingLeft - layout.PaddingRight);
            float remainingWidth = availableWidth - columnCount * layout.SlotSize.x;
            return Mathf.Max(0f, remainingWidth / (columnCount - 1));
        }
    }
}
