using UnityEngine;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 背包手动网格布局参数，替代 Unity GridLayoutGroup 参与运行时计算。
    /// </summary>
    [System.Serializable]
    public struct InventoryManualGridLayout
    {
        [SerializeField] private Vector2 slotSize;
        [SerializeField] private Vector2 slotSpacing;
        [SerializeField] private Vector2Int padding;
        [SerializeField] private int fixedColumnCount;
        [SerializeField] private bool autoFillHorizontalSpacing;

        /// <summary>
        /// 单个槽位尺寸。
        /// </summary>
        public Vector2 SlotSize => new Vector2(Mathf.Max(1f, slotSize.x), Mathf.Max(1f, slotSize.y));

        /// <summary>
        /// 槽位间距。
        /// </summary>
        public Vector2 SlotSpacing => new Vector2(Mathf.Max(0f, slotSpacing.x), Mathf.Max(0f, slotSpacing.y));

        /// <summary>
        /// 左侧边距。
        /// </summary>
        public int PaddingLeft => Mathf.Max(0, padding.x);

        /// <summary>
        /// 右侧边距。
        /// </summary>
        public int PaddingRight => Mathf.Max(0, padding.x);

        /// <summary>
        /// 顶部边距。
        /// </summary>
        public int PaddingTop => Mathf.Max(0, padding.y);

        /// <summary>
        /// 底部边距。
        /// </summary>
        public int PaddingBottom => Mathf.Max(0, padding.y);

        /// <summary>
        /// 固定列数，0 表示按视口宽度自动计算。
        /// </summary>
        public int FixedColumnCount => Mathf.Max(0, fixedColumnCount);

        /// <summary>
        /// 是否自动填充横向间距，使每行槽位刚好撑满可用宽度。
        /// </summary>
        public bool AutoFillHorizontalSpacing => autoFillHorizontalSpacing;

        /// <summary>
        /// 创建默认手动网格布局参数。
        /// </summary>
        /// <returns>默认布局参数。</returns>
        public static InventoryManualGridLayout CreateDefault()
        {
            return new InventoryManualGridLayout
            {
                slotSize = new Vector2(22f, 22f),
                slotSpacing = Vector2.zero,
                padding = Vector2Int.zero,
                fixedColumnCount = 0,
                autoFillHorizontalSpacing = false
            };
        }
    }
}
