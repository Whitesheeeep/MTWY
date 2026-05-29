using UnityEngine;
using UnityEngine.UI;
using WS_Modules.InputModule;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 背包窗口 View，负责 Bag 虚拟滚动布局和拖拽边缘滚动。
    /// </summary>
    public sealed class InventoryBagView : InventorySlotContainerViewBase<InventoryBagViewModel>
    {
        #region Fields
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private RectTransform topEdgeScrollArea;
        [SerializeField] private RectTransform bottomEdgeScrollArea;
        [SerializeField] private RectTransform edgeScrollDeadZoneArea;
        [SerializeField] private InventoryManualGridLayout manualLayout = InventoryManualGridLayout.CreateDefault();
        [SerializeField] private InventoryDragEdgeScrollController dragEdgeScroll;
        [SerializeField] private int visibleSlotCount = 30;
        private InventoryVirtualizedSlotViewLayout virtualizedLayout = new InventoryVirtualizedSlotViewLayout();

        private Vector2 lastDragEdgeScrollScreenPosition;
        private bool dragEdgeScrollActive;
        private bool hasDragEdgeScrollScreenPosition;
        #endregion

        #region Properties
        /// <inheritdoc />
        public override int VisibleSlotCount => Mathf.Max(0, visibleSlotCount);
        /// <inheritdoc />
        protected override IInventorySlotViewLayout SlotLayout => virtualizedLayout;
        #endregion

        #region Public Methods
        /// <summary>
        /// 设置背包窗口运行时显示槽位数量。
        /// </summary>
        /// <param name="slotCount">目标显示槽位数量，小于 0 时按 0 处理。</param>
        public override void SetVisibleSlotCount(int slotCount)
        {
            visibleSlotCount = Mathf.Max(0, slotCount);
            base.SetVisibleSlotCount(visibleSlotCount);
        }

        /// <summary>
        /// 开始检测拖拽边缘滚动。
        /// </summary>
        private void BeginDragEdgeScroll()
        {
            if (!EnsureReferences()) return;

            RectTransform viewport = GetViewport();
            dragEdgeScroll.Begin(viewport, contentRoot, topEdgeScrollArea, bottomEdgeScrollArea, edgeScrollDeadZoneArea);
            dragEdgeScrollActive = true;
            hasDragEdgeScrollScreenPosition = false;
        }

        /// <summary>
        /// 根据当前鼠标位置更新拖拽边缘滚动。
        /// </summary>
        /// <param name="screenPosition">鼠标屏幕坐标。</param>
        private void UpdateDragEdgeScroll(Vector2 screenPosition)
        {
            if (!EnsureReferences()) return;

            if (!dragEdgeScrollActive) BeginDragEdgeScroll();

            lastDragEdgeScrollScreenPosition = screenPosition;
            hasDragEdgeScrollScreenPosition = true;
        }

        /// <summary>
        /// 结束拖拽边缘滚动检测。
        /// </summary>
        public void EndDragEdgeScroll()
        {
            dragEdgeScroll.End();
            dragEdgeScrollActive = false;
            hasDragEdgeScrollScreenPosition = false;
        }
        #endregion

        #region Unity LifeCycle
        private void OnDisable()
        {
            EndDragEdgeScroll();
            UnregisterScrollEvent();
        }

        private void Update()
        {
            StepDragEdgeScroll();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EndDragEdgeScroll();
            UnregisterScrollEvent();
        }

        private void Reset()
        {
            SlotRoot = transform;
            manualLayout = InventoryManualGridLayout.CreateDefault();
        }
        #endregion

        #region Layout
        protected override int GetVisibleSlotCountFromViewModel()
        {
            return ViewModel != null ? ViewModel.UnlockedSlotCount : VisibleSlotCount;
        }

        protected override void ConfigureLayout()
        {
            EnsureReferences();
            RegisterScrollEvent();
            base.ConfigureLayout();
            virtualizedLayout.SetScrollContext(scrollRect, contentRoot, manualLayout);
            virtualizedLayout.SetMaxActiveSlotCount(VisibleSlotCount);
        }

        private bool EnsureReferences()
        {
            SlotRoot ??= transform;
            scrollRect ??= GetComponentInParent<ScrollRect>();
            contentRoot ??= scrollRect != null ? scrollRect.content : SlotRoot as RectTransform;
            if (contentRoot != null) SlotRoot = contentRoot;
            if (SlotPrefab == null) SlotPrefab = GetComponentInChildren<InventorySlotView>(true);

            if (SlotPrefab != null && SlotPrefab.transform.IsChildOf(SlotRoot)) SlotPrefab.gameObject.SetActive(false);
            if (SlotPrefab != null && scrollRect != null && contentRoot != null) return true;

            Debug.LogWarning("[InventoryBagView] 缺少 ScrollRect、Content 或槽位预制体，无法刷新背包槽位。", this);
            return false;
        }
        #endregion

        #region Events
        private void RegisterScrollEvent()
        {
            if (scrollRect == null) return;

            scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
            scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }

        private void UnregisterScrollEvent()
        {
            if (scrollRect == null) return;

            scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
        }

        private void OnScrollValueChanged(Vector2 value)
        {
            virtualizedLayout.RefreshVisibleSlots(false);
        }
        #endregion

        #region Tools
        private RectTransform GetViewport()
        {
            if (scrollRect == null) return null;

            return scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();
        }

        private void StepDragEdgeScroll()
        {
            if (ViewModel == null || !InventorySlotDragCoordinator.HasActiveDrag)
            {
                if (dragEdgeScrollActive) EndDragEdgeScroll();
                return;
            }

            Vector2 screenPosition = InputMgr.Instance.MouseScreenPosition;
            UpdateDragEdgeScroll(screenPosition);
            if (!dragEdgeScrollActive || !hasDragEdgeScrollScreenPosition) return;

            Camera uiCamera = UIManager.Instance.Camera ?? Camera.main;
            if (dragEdgeScroll.Update(lastDragEdgeScrollScreenPosition, uiCamera, Time.unscaledDeltaTime))
                virtualizedLayout.RefreshVisibleSlots(false);
        }
        #endregion
    }
}
