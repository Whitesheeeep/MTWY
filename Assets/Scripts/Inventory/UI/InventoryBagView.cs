using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 背包窗口 View，负责 Bag 槽位的手动虚拟滚动渲染、刷新和点击转发。
    /// </summary>
    public sealed class InventoryBagView : MonoBehaviour
    {
        #region 字段
        [SerializeField] private InventorySlotView slotPrefab;
        [SerializeField] private Transform slotRoot;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private RectTransform topEdgeScrollArea;
        [SerializeField] private RectTransform bottomEdgeScrollArea;
        [SerializeField] private RectTransform edgeScrollDeadZoneArea;
        [SerializeField] private InventoryManualGridLayout manualLayout = InventoryManualGridLayout.CreateDefault();
        [SerializeField] private InventoryDragEdgeScrollController dragEdgeScroll = new InventoryDragEdgeScrollController();
        [SerializeField] private int visibleSlotCount = 30;

        // 无限滚动列表
        private readonly Dictionary<int, InventorySlotView> activeSlots = new Dictionary<int, InventorySlotView>();
        private readonly List<int> recycleIndexBuffer = new List<int>();

        // 滚动部分
        private readonly InventoryGridScrollCalculator scrollCalculator = new InventoryGridScrollCalculator();
        private IReadOnlyList<InventorySlotViewData> currentSlotDataList;
        private InventoryVisibleIndexRange currentVisibleRange = InventoryVisibleIndexRange.Empty;
        private Action<int> onSlotClicked;
        private Action<InventorySlotDragEventArgs> onSlotDragStarted;
        private Action<InventorySlotDragEventArgs> onSlotDragging;
        private Action<InventorySlotDragEventArgs> onSlotDragEnded;
        private Action<InventorySlotDragEventArgs> onSlotDragEntered;
        private Action<InventorySlotDragEventArgs> onSlotDragExited;
        private Action<InventorySlotDropEventArgs> onSlotDropped;
        private int currentSelectedSlotIndex = -1;
        private Vector2 lastDragEdgeScrollScreenPosition;
        private bool dragEdgeScrollActive;
        private bool hasDragEdgeScrollScreenPosition;
        #endregion

        #region 属性
        /// <summary>
        /// 当前背包窗口显示的槽位数量。
        /// </summary>
        public int VisibleSlotCount => Mathf.Max(0, visibleSlotCount);
        #endregion

        #region 初始化
        /// <summary>
        /// 设置背包窗口运行时显示槽位数量。
        /// </summary>
        /// <param name="slotCount">目标显示槽位数量，小于 0 时按 0 处理。</param>
        public void SetVisibleSlotCount(int slotCount)
        {
            visibleSlotCount = Mathf.Max(0, slotCount);
            RefreshVisibleSlots();
        }

        /// <summary>
        /// 开始检测拖拽边缘滚动。
        /// </summary>
        public void BeginDragEdgeScroll()
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
        public void UpdateDragEdgeScroll(Vector2 screenPosition)
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

        /// <summary>
        /// 初始化背包槽位点击回调并确保槽位实例存在。
        /// </summary>
        /// <param name="slotClickedCallback">槽位被点击时触发的回调。</param>
        public void Initialize(Action<int> slotClickedCallback)
        {
            Initialize(slotClickedCallback, null, null, null, null, null, null);
        }

        /// <summary>
        /// 初始化背包槽位点击与拖拽回调并确保槽位实例存在。
        /// </summary>
        /// <param name="slotClickedCallback">槽位被点击时触发的回调。</param>
        /// <param name="slotDragStartedCallback">槽位开始拖拽时触发的回调。</param>
        /// <param name="slotDragEndedCallback">槽位拖拽结束时触发的回调。</param>
        /// <param name="slotDroppedCallback">拖拽释放到槽位时触发的回调。</param>
        public void Initialize(
            Action<int> slotClickedCallback,
            Action<InventorySlotDragEventArgs> slotDragStartedCallback,
            Action<InventorySlotDragEventArgs> slotDraggingCallback,
            Action<InventorySlotDragEventArgs> slotDragEndedCallback,
            Action<InventorySlotDragEventArgs> slotDragEnteredCallback,
            Action<InventorySlotDragEventArgs> slotDragExitedCallback,
            Action<InventorySlotDropEventArgs> slotDroppedCallback)
        {
            onSlotClicked = slotClickedCallback;
            onSlotDragStarted = slotDragStartedCallback;
            onSlotDragging = slotDraggingCallback;
            onSlotDragEnded = slotDragEndedCallback;
            onSlotDragEntered = slotDragEnteredCallback;
            onSlotDragExited = slotDragExitedCallback;
            onSlotDropped = slotDroppedCallback;
            EnsureReferences();
            RegisterScrollEvent();
            RefreshVisibleSlots();
        }
        #endregion

        #region 刷新
        /// <summary>
        /// 刷新指定槽位显示。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <param name="data">槽位显示数据。</param>
        /// <param name="selected">是否选中。</param>
        public void RefreshSlot(int index, InventorySlotViewData data, bool selected)
        {
            RefreshSlot(index, data, selected, VisibleSlotCount);
        }

        /// <summary>
        /// 刷新指定槽位显示。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <param name="data">槽位显示数据。</param>
        /// <param name="selected">是否选中。</param>
        /// <param name="unlockedSlotCount">当前已解锁槽位数量。</param>
        public void RefreshSlot(int index, InventorySlotViewData data, bool selected, int unlockedSlotCount)
        {
            visibleSlotCount = Mathf.Max(0, unlockedSlotCount);
            RefreshVisibleSlots();
            if (!currentVisibleRange.Contains(index)) return;
            if (!activeSlots.TryGetValue(index, out InventorySlotView slot)) return;

            slot.Refresh(data, selected);
        }

        /// <summary>
        /// 刷新背包窗口槽位显示。
        /// </summary>
        /// <param name="slotDataList">槽位显示数据列表。</param>
        /// <param name="selectedSlotIndex">当前选中的槽位索引，传入负数表示不选中。</param>
        public void RefreshSlots(IReadOnlyList<InventorySlotViewData> slotDataList, int selectedSlotIndex)
        {
            RefreshSlots(slotDataList, selectedSlotIndex, VisibleSlotCount);
        }

        /// <summary>
        /// 刷新背包窗口槽位显示。
        /// </summary>
        /// <param name="slotDataList">槽位显示数据列表。</param>
        /// <param name="selectedSlotIndex">当前选中的槽位索引，传入负数表示不选中。</param>
        /// <param name="unlockedSlotCount">当前已解锁槽位数量。</param>
        public void RefreshSlots(
            IReadOnlyList<InventorySlotViewData> slotDataList,
            int selectedSlotIndex,
            int unlockedSlotCount)
        {
            currentSlotDataList = slotDataList;
            currentSelectedSlotIndex = selectedSlotIndex;
            visibleSlotCount = Mathf.Max(0, unlockedSlotCount);
            RefreshVisibleSlots();
        }

        /// <summary>
        /// 刷新当前选中的槽位显示。
        /// </summary>
        /// <param name="selectedSlotIndex">目标槽位索引，传入负数表示取消选中。</param>
        public void RefreshSelection(int selectedSlotIndex)
        {
            currentSelectedSlotIndex = selectedSlotIndex;
            foreach (KeyValuePair<int, InventorySlotView> activeSlot in activeSlots)
                activeSlot.Value.RefreshSelection(activeSlot.Key == selectedSlotIndex);
        }

        /// <summary>
        /// 清空背包窗口显示。
        /// </summary>
        public void ClearSlots()
        {
            foreach (InventorySlotView slot in activeSlots.Values)
                slot.Clear();
        }

        /// <summary>
        /// 刷新指定背包槽位的拖拽放置预览。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <param name="canDrop">是否显示为可放置。</param>
        public void RefreshDropPreview(int index, bool canDrop)
        {
            if (!activeSlots.TryGetValue(index, out InventorySlotView slot)) return;

            slot.RefreshDropPreview(canDrop);
        }

        /// <summary>
        /// 清理全部可见背包槽位的拖拽放置预览。
        /// </summary>
        public void ClearDropPreview()
        {
            foreach (InventorySlotView slot in activeSlots.Values)
                slot.ClearDropPreview();
        }
        #endregion

        #region Unity 生命周期
        private void OnDisable()
        {
            EndDragEdgeScroll();
            UnregisterScrollEvent();
        }

        private void Update()
        {
            StepDragEdgeScroll();
        }

        private void OnDestroy()
        {
            EndDragEdgeScroll();
            UnregisterScrollEvent();
        }

        private void Reset()
        {
            slotRoot = transform;
            manualLayout = InventoryManualGridLayout.CreateDefault();
        }
        #endregion

        #region 槽位实例
        private void RefreshVisibleSlots(bool forceRefresh = true)
        {
            if (!EnsureReferences()) return;

            RegisterScrollEvent();
            ApplyContentHeight();
            InventoryVisibleIndexRange nextRange = scrollCalculator.CalculateVisibleRange(
                VisibleSlotCount,
                scrollRect,
                contentRoot,
                manualLayout);
            if (!forceRefresh && IsSameRange(nextRange, currentVisibleRange)) return;

            currentVisibleRange = nextRange;
            if (!nextRange.IsValid)
            {
                RecycleAllSlots();
                return;
            }

            CollectRecycleIndices(nextRange);
            for (int i = 0; i < recycleIndexBuffer.Count; i++)
                RemoveSlot(recycleIndexBuffer[i]);

            for (int i = nextRange.StartIndex; i <= nextRange.EndIndex; i++)
            {
                InventorySlotView slot = EnsureSlot(i);
                PositionSlot(i, slot);
                slot.Refresh(GetSlotData(currentSlotDataList, i), i == currentSelectedSlotIndex);
            }
        }

        private InventorySlotView EnsureSlot(int index)
        {
            if (activeSlots.TryGetValue(index, out InventorySlotView slot))
            {
                slot.gameObject.SetActive(true);
                slot.Initialize(
                    index,
                    InventorySlotArea.Bag,
                    OnSlotClicked,
                    OnSlotDragStarted,
                    OnSlotDragging,
                    OnSlotDragEnded,
                    OnSlotDragEntered,
                    OnSlotDragExited,
                    OnSlotDropped);
                return slot;
            }

            slot = Instantiate(slotPrefab, contentRoot);
            slot.gameObject.name = $"BagItem ({index + 1})";
            slot.Initialize(
                index,
                InventorySlotArea.Bag,
                OnSlotClicked,
                OnSlotDragStarted,
                OnSlotDragging,
                OnSlotDragEnded,
                OnSlotDragEntered,
                OnSlotDragExited,
                OnSlotDropped);
            slot.gameObject.SetActive(true);
            activeSlots[index] = slot;
            return slot;
        }

        private void PositionSlot(int index, InventorySlotView slot)
        {
            RectTransform slotRect = slot.transform as RectTransform;
            if (slotRect == null) return;

            RectTransform viewport = GetViewport();
            slotRect.anchorMin = new Vector2(0f, 1f);
            slotRect.anchorMax = new Vector2(0f, 1f);
            slotRect.pivot = new Vector2(0f, 1f);
            slotRect.sizeDelta = manualLayout.SlotSize;
            slotRect.anchoredPosition = scrollCalculator.CalculateSlotPosition(index, manualLayout, viewport, VisibleSlotCount);
        }

        private void CollectRecycleIndices(InventoryVisibleIndexRange nextRange)
        {
            recycleIndexBuffer.Clear();
            foreach (int index in activeSlots.Keys)
                if (!nextRange.Contains(index)) recycleIndexBuffer.Add(index);
        }

        private void RemoveSlot(int index)
        {
            if (!activeSlots.TryGetValue(index, out InventorySlotView slot)) return;

            activeSlots.Remove(index);
            Destroy(slot.gameObject);
        }

        private void RecycleAllSlots()
        {
            recycleIndexBuffer.Clear();
            foreach (int index in activeSlots.Keys)
                recycleIndexBuffer.Add(index);

            for (int i = 0; i < recycleIndexBuffer.Count; i++)
                RemoveSlot(recycleIndexBuffer[i]);
        }
        #endregion

        #region 引用和布局
        private bool EnsureReferences()
        {
            slotRoot ??= transform;
            scrollRect ??= GetComponentInParent<ScrollRect>();
            contentRoot ??= scrollRect != null ? scrollRect.content : slotRoot as RectTransform;
            if (contentRoot != null) slotRoot = contentRoot;
            if (slotPrefab == null) slotPrefab = GetComponentInChildren<InventorySlotView>(true);
            EnsureEdgeScrollAreas();

            if (slotPrefab != null && slotPrefab.transform.IsChildOf(slotRoot)) slotPrefab.gameObject.SetActive(false);
            if (slotPrefab != null && scrollRect != null && contentRoot != null) return true;

            Debug.LogWarning("[InventoryBagView] 缺少 ScrollRect、Content 或槽位预制体，无法刷新背包槽位。", this);
            return false;
        }

        private void ApplyContentHeight()
        {
            RectTransform viewport = GetViewport();
            float contentHeight = scrollCalculator.CalculateContentHeight(VisibleSlotCount, manualLayout, viewport);
            contentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
        }
        #endregion

        #region 事件
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
            RefreshVisibleSlots(false);
        }

        private void OnSlotClicked(int index)
        {
            onSlotClicked?.Invoke(index);
        }

        private void OnSlotDragStarted(InventorySlotDragEventArgs eventArgs)
        {
            onSlotDragStarted?.Invoke(eventArgs);
        }

        private void OnSlotDragging(InventorySlotDragEventArgs eventArgs)
        {
            onSlotDragging?.Invoke(eventArgs);
        }

        private void OnSlotDragEnded(InventorySlotDragEventArgs eventArgs)
        {
            onSlotDragEnded?.Invoke(eventArgs);
        }

        private void OnSlotDragEntered(InventorySlotDragEventArgs eventArgs)
        {
            onSlotDragEntered?.Invoke(eventArgs);
        }

        private void OnSlotDragExited(InventorySlotDragEventArgs eventArgs)
        {
            onSlotDragExited?.Invoke(eventArgs);
        }

        private void OnSlotDropped(InventorySlotDropEventArgs eventArgs)
        {
            onSlotDropped?.Invoke(eventArgs);
        }
        #endregion

        #region 工具方法
        private static InventorySlotViewData GetSlotData(IReadOnlyList<InventorySlotViewData> dataList, int index)
        {
            if (dataList == null || index < 0 || index >= dataList.Count) return InventorySlotViewData.Empty(index);

            return dataList[index];
        }

        private static bool IsSameRange(InventoryVisibleIndexRange left, InventoryVisibleIndexRange right)
        {
            return left.StartIndex == right.StartIndex && left.EndIndex == right.EndIndex;
        }

        private RectTransform GetViewport()
        {
            if (scrollRect == null) return null;

            return scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();
        }

        private Camera GetUICamera()
        {
            Canvas canvas = scrollRect != null ? scrollRect.GetComponentInParent<Canvas>() : null;
            return canvas != null ? canvas.worldCamera : null;
        }

        private void EnsureEdgeScrollAreas()
        {
            RectTransform viewport = GetViewport();
            if (viewport == null) return;

            topEdgeScrollArea ??= viewport.Find("EdgeScrollTopArea") as RectTransform;
            bottomEdgeScrollArea ??= viewport.Find("EdgeScrollBottomArea") as RectTransform;
            edgeScrollDeadZoneArea ??= viewport.Find("EdgeScrollDeadZoneArea") as RectTransform;
        }

        private void StepDragEdgeScroll()
        {
            if (!dragEdgeScrollActive || !hasDragEdgeScrollScreenPosition) return;

            Camera uiCamera = GetUICamera();
            if (dragEdgeScroll.Update(lastDragEdgeScrollScreenPosition, uiCamera, Time.unscaledDeltaTime))
                RefreshVisibleSlots(false);
        }

        #endregion
    }
}
