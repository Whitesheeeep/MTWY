using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 虚拟滚动槽位布局，适用于只实例化可见范围槽位的容器。
    /// </summary>
    [Serializable]
    public sealed class InventoryVirtualizedSlotViewLayout : IInventorySlotViewLayout
    {
        #region Fields
        private readonly Dictionary<int, InventorySlotView> activeSlots = new Dictionary<int, InventorySlotView>();
        private readonly List<int> recycleIndexBuffer = new List<int>();
        private readonly InventoryGridScrollCalculator scrollCalculator = new InventoryGridScrollCalculator();

        private InventorySlotView slotPrefab;
        private Transform slotRoot;
        private InventorySlotViewEventModule eventModule;
        private ScrollRect scrollRect;
        private RectTransform contentRoot;
        private InventoryManualGridLayout manualLayout;
        private IReadOnlyList<InventorySlotViewData> currentSlotDataList;
        private InventoryVisibleIndexRange currentVisibleRange = InventoryVisibleIndexRange.Empty;
        private int visibleSlotCount;
        private int maxActiveSlotCount;
        private int currentSelectedSlotIndex = -1;
        #endregion

        #region Properties
        /// <inheritdoc />
        public int VisibleSlotCount => Mathf.Max(0, visibleSlotCount);
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public void SetContext(
            InventorySlotView slotPrefab,
            Transform slotRoot,
            InventorySlotViewEventModule eventModule)
        {
            this.slotPrefab = slotPrefab;
            this.slotRoot = slotRoot;
            this.eventModule = eventModule;
        }

        /// <summary>
        /// 设置虚拟滚动布局所需的滚动上下文。
        /// </summary>
        public void SetScrollContext(
            ScrollRect scrollRect,
            RectTransform contentRoot,
            InventoryManualGridLayout manualLayout)
        {
            this.scrollRect = scrollRect;
            this.contentRoot = contentRoot;
            this.manualLayout = manualLayout;
            if (contentRoot != null) slotRoot = contentRoot;
        }

        /// <summary>
        /// 设置当前 View 最多实例化的可见槽位数量。
        /// </summary>
        /// <param name="count">最大实例化槽位数量，小于 0 时按 0 处理。</param>
        public void SetMaxActiveSlotCount(int count)
        {
            maxActiveSlotCount = Mathf.Max(0, count);
            RefreshVisibleSlots();
        }

        /// <inheritdoc />
        public void SetVisibleSlotCount(int count)
        {
            visibleSlotCount = Mathf.Max(0, count);
            RefreshVisibleSlots();
        }

        /// <inheritdoc />
        public void RefreshSlot(int index, InventorySlotViewData data, bool selected)
        {
            RefreshVisibleSlots();
            if (!currentVisibleRange.Contains(index)) return;
            if (!activeSlots.TryGetValue(index, out InventorySlotView slot)) return;

            slot.Refresh(data, selected);
        }

        /// <inheritdoc />
        public void RefreshSlots(IReadOnlyList<InventorySlotViewData> dataList, int selectedIndex)
        {
            currentSlotDataList = dataList;
            currentSelectedSlotIndex = selectedIndex;
            RefreshVisibleSlots();
        }

        /// <inheritdoc />
        public void RefreshSelection(int selectedIndex)
        {
            currentSelectedSlotIndex = selectedIndex;
            foreach (KeyValuePair<int, InventorySlotView> activeSlot in activeSlots)
                activeSlot.Value.RefreshSelection(activeSlot.Key == selectedIndex);
        }

        /// <inheritdoc />
        public void RefreshDropPreview(int index, bool canDrop)
        {
            if (!activeSlots.TryGetValue(index, out InventorySlotView slot)) return;

            slot.RefreshDropPreview(canDrop);
        }

        /// <inheritdoc />
        public void ClearDropPreview()
        {
            foreach (InventorySlotView slot in activeSlots.Values)
                if (slot != null) slot.ClearDropPreview();
        }

        /// <inheritdoc />
        public void ClearSlots()
        {
            foreach (InventorySlotView slot in activeSlots.Values)
                if (slot != null) slot.Clear();
        }

        /// <summary>
        /// 滚动位置变化后刷新可见槽位。
        /// </summary>
        public void RefreshVisibleSlots(bool forceRefresh = true)
        {
            if (!EnsureReferences()) return;

            ApplyContentHeight();
            InventoryVisibleIndexRange nextRange = scrollCalculator.CalculateVisibleRange(
                VisibleSlotCount,
                scrollRect,
                contentRoot,
                manualLayout);
            nextRange = ClampRangeByMaxActiveSlotCount(nextRange);
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
        #endregion

        #region Slot Instance
        private bool EnsureReferences()
        {
            if (contentRoot != null) slotRoot = contentRoot;
            if (slotPrefab != null && slotRoot != null && scrollRect != null && contentRoot != null) return true;

            Debug.LogWarning("[InventoryVirtualizedSlotViewLayout] 缺少 ScrollRect、Content 或槽位预制体，无法刷新槽位。");
            return false;
        }

        private InventorySlotView EnsureSlot(int index)
        {
            if (activeSlots.TryGetValue(index, out InventorySlotView slot))
            {
                slot.gameObject.SetActive(true);
                InitializeSlot(slot, index);
                return slot;
            }

            slot = UnityEngine.Object.Instantiate(slotPrefab, contentRoot);
            slot.gameObject.name = $"BagItem ({index + 1})";
            InitializeSlot(slot, index);
            slot.gameObject.SetActive(true);
            activeSlots[index] = slot;
            return slot;
        }

        private void InitializeSlot(InventorySlotView slot, int index)
        {
            slot.Initialize(index, eventModule);
        }

        private void RemoveSlot(int index)
        {
            if (!activeSlots.TryGetValue(index, out InventorySlotView slot)) return;

            activeSlots.Remove(index);
            if (slot != null) UnityEngine.Object.Destroy(slot.gameObject);
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

        #region Layout
        private void ApplyContentHeight()
        {
            RectTransform viewport = GetViewport();
            float contentHeight = scrollCalculator.CalculateContentHeight(VisibleSlotCount, manualLayout, viewport);
            contentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
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

        private RectTransform GetViewport()
        {
            if (scrollRect == null) return null;

            return scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();
        }
        #endregion

        #region Data
        private static InventorySlotViewData GetSlotData(IReadOnlyList<InventorySlotViewData> dataList, int index)
        {
            if (dataList == null || index < 0 || index >= dataList.Count) return InventorySlotViewData.Empty(index);

            return dataList[index];
        }

        private static bool IsSameRange(InventoryVisibleIndexRange left, InventoryVisibleIndexRange right)
        {
            return left.StartIndex == right.StartIndex && left.EndIndex == right.EndIndex;
        }

        private InventoryVisibleIndexRange ClampRangeByMaxActiveSlotCount(InventoryVisibleIndexRange range)
        {
            if (!range.IsValid || maxActiveSlotCount <= 0) return InventoryVisibleIndexRange.Empty;

            int endIndex = Mathf.Min(range.EndIndex, range.StartIndex + maxActiveSlotCount - 1);
            return new InventoryVisibleIndexRange(range.StartIndex, endIndex);
        }
        #endregion
    }
}
