using UnityEngine;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 背包拖拽边缘滚动控制器，负责根据鼠标位置驱动 Content 连续滚动。
    /// </summary>
    [System.Serializable]
    public sealed class InventoryDragEdgeScrollController
    {
        #region 字段
        [SerializeField] private bool enabled = true;
        [SerializeField] private float maxScrollSpeed = 240f;
        [SerializeField] private float deadZone = 2f;

        private RectTransform viewport;
        private RectTransform contentRoot;
        private RectTransform topScrollArea;
        private RectTransform bottomScrollArea;
        private RectTransform deadZoneArea;
        private bool scrolling;
        #endregion

        #region API
        /// <summary>
        /// 开始边缘滚动检测。
        /// </summary>
        /// <param name="viewport">滚动视口。</param>
        /// <param name="contentRoot">滚动内容节点。</param>
        /// <param name="topScrollArea">顶部滚动触发区域。</param>
        /// <param name="bottomScrollArea">底部滚动触发区域。</param>
        /// <param name="deadZoneArea">中间死区区域。</param>
        public void Begin(
            RectTransform viewport,
            RectTransform contentRoot,
            RectTransform topScrollArea,
            RectTransform bottomScrollArea,
            RectTransform deadZoneArea)
        {
            this.viewport = viewport;
            this.contentRoot = contentRoot;
            this.topScrollArea = topScrollArea;
            this.bottomScrollArea = bottomScrollArea;
            this.deadZoneArea = deadZoneArea;
            scrolling = enabled && viewport != null && contentRoot != null;
        }

        /// <summary>
        /// 根据当前屏幕坐标更新边缘滚动。
        /// </summary>
        /// <param name="screenPosition">鼠标屏幕坐标。</param>
        /// <param name="uiCamera">UI 相机，Overlay 模式可传入 null。</param>
        /// <param name="deltaTime">本帧时间。</param>
        /// <returns>本帧发生滚动返回 true。</returns>
        public bool Update(Vector2 screenPosition, Camera uiCamera, float deltaTime)
        {
            if (!scrolling || deltaTime <= 0f) return false;

            float direction = GetScrollDirection(screenPosition, uiCamera);
            if (Mathf.Approximately(direction, 0f)) return false;

            float maxScrollY = GetMaxScrollY();
            if (maxScrollY <= 0f) return false;

            Vector2 anchoredPosition = contentRoot.anchoredPosition;
            float nextY = Mathf.Clamp(anchoredPosition.y + direction * Mathf.Max(0f, maxScrollSpeed) * deltaTime, 0f, maxScrollY);
            if (Mathf.Abs(nextY - anchoredPosition.y) <= deadZone * deltaTime) return false;

            contentRoot.anchoredPosition = new Vector2(anchoredPosition.x, nextY);
            return true;
        }

        /// <summary>
        /// 结束边缘滚动检测。
        /// </summary>
        public void End()
        {
            scrolling = false;
            viewport = null;
            contentRoot = null;
            topScrollArea = null;
            bottomScrollArea = null;
            deadZoneArea = null;
        }
        #endregion

        #region 工具方法
        private float GetScrollDirection(Vector2 screenPosition, Camera uiCamera)
        {
            if (IsPointerInArea(deadZoneArea, screenPosition, uiCamera)) return 0f;
            if (IsPointerInArea(topScrollArea, screenPosition, uiCamera)) return -1f;
            if (IsPointerInArea(bottomScrollArea, screenPosition, uiCamera)) return 1f;

            return 0f;
        }

        private static bool IsPointerInArea(RectTransform area, Vector2 screenPosition, Camera uiCamera)
        {
            return area != null && RectTransformUtility.RectangleContainsScreenPoint(area, screenPosition, uiCamera);
        }

        private float GetMaxScrollY()
        {
            float contentHeight = contentRoot.rect.height;
            float viewportHeight = viewport.rect.height;
            return Mathf.Max(0f, contentHeight - viewportHeight);
        }
        #endregion
    }
}
