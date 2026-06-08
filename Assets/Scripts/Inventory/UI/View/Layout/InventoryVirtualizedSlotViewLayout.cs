using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WS_Modules.Pooling;

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
        private int currentSelectedSlotIndex = -1;
        private string slotPoolKey;
        private bool fallbackWarningLogged;
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public void SetContext(
            InventorySlotView slotPrefab,
            Transform slotRoot,
            InventorySlotViewEventModule eventModule)
        {
            if (this.slotPrefab != slotPrefab)
            {
                slotPoolKey = null;
                fallbackWarningLogged = false;
            }

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

            int slotCount = GetCurrentSlotCount();
            ApplyContentHeight(slotCount);
            InventoryVisibleIndexRange nextRange = scrollCalculator.CalculateVisibleRange(
                slotCount,
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

            PrepareSlotPool(GetRequiredSlotCount(nextRange));
            for (int i = nextRange.StartIndex; i <= nextRange.EndIndex; i++)
            {
                InventorySlotView slot = EnsureSlot(i);
                if (slot == null) continue;

                PositionSlot(i, slot);
                slot.Refresh(GetSlotData(currentSlotDataList, i), i == currentSelectedSlotIndex);
            }
        }
        #endregion

        #region Slot Instance
        private bool EnsureReferences()
        {
            if (contentRoot != null) slotRoot = contentRoot;
            if (slotPrefab != null && slotRoot != null && scrollRect != null && contentRoot != null)
            {
                return true;
            }

            Debug.LogWarning("[InventoryVirtualizedSlotViewLayout] 缺少 ScrollRect、Content 或槽位预制体，无法刷新槽位。");
            return false;
        }

        private void PrepareSlotPool(int targetAvailableCount)
        {
            if (slotPrefab == null || targetAvailableCount <= 0) return;

            EnsureSlotPoolKey();
            PoolManager.Instance.Prewarm(slotPrefab.gameObject, targetAvailableCount, -1);
        }

        private InventorySlotView EnsureSlot(int index)
        {
            if (activeSlots.TryGetValue(index, out InventorySlotView slot))
            {
                slot.gameObject.SetActive(true);
                InitializeSlot(slot, index);
                return slot;
            }

            slot = GetSlotFromPool();
            if (slot == null) slot = CreateFallbackSlot();
            if (slot == null) return null;

            InitializeSlot(slot, index);
            slot.gameObject.SetActive(true);
            activeSlots[index] = slot;
            return slot;
        }

        private InventorySlotView GetSlotFromPool()
        {
            EnsureSlotPoolKey();
            if (string.IsNullOrEmpty(slotPoolKey)) return null;

            GameObject slotObject = PoolManager.Instance.Get(slotPoolKey, contentRoot);
            if (slotObject == null) return null;

            InventorySlotView slot = slotObject.GetComponent<InventorySlotView>();
            if (slot != null) return slot;

            PoolManager.Instance.Recycle(slotPoolKey, slotObject);
            return null;
        }

        private InventorySlotView CreateFallbackSlot()
        {
            if (!fallbackWarningLogged)
            {
                Debug.LogWarning("[InventoryVirtualizedSlotViewLayout] 对象池未能提供槽位实例，已退化为直接实例化。");
                fallbackWarningLogged = true;
            }

            return UnityEngine.Object.Instantiate(slotPrefab, contentRoot);
        }

        private void InitializeSlot(InventorySlotView slot, int index)
        {
            slot.Initialize(index, eventModule);
        }

        private void RemoveSlot(int index)
        {
            if (!activeSlots.TryGetValue(index, out InventorySlotView slot)) return;

            activeSlots.Remove(index);
            RecycleSlot(slot);
        }

        private void RecycleAllSlots()
        {
            recycleIndexBuffer.Clear();
            foreach (int index in activeSlots.Keys)
                recycleIndexBuffer.Add(index);

            for (int i = 0; i < recycleIndexBuffer.Count; i++)
                RemoveSlot(recycleIndexBuffer[i]);
        }

        private void RecycleSlot(InventorySlotView slot)
        {
            if (slot == null) return;

            slot.ClearDropPreview();
            slot.Clear();
            if (string.IsNullOrEmpty(slotPoolKey))
            {
                slot.gameObject.SetActive(false);
                return;
            }

            PoolManager.Instance.Recycle(slotPoolKey, slot.gameObject);
        }

        private void EnsureSlotPoolKey()
        {
            if (!string.IsNullOrEmpty(slotPoolKey) || slotPrefab == null) return;

            slotPoolKey = slotPrefab.gameObject.name;
        }
        #endregion

        #region Layout
        private void ApplyContentHeight(int slotCount)
        {
            RectTransform viewport = GetViewport();
            float contentHeight = scrollCalculator.CalculateContentHeight(slotCount, manualLayout, viewport);
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
            slotRect.anchoredPosition = scrollCalculator.CalculateSlotPosition(index, manualLayout, viewport, GetCurrentSlotCount());
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

        private int GetCurrentSlotCount()
        {
            return currentSlotDataList?.Count ?? 0;
        }

        private int GetRequiredSlotCount(InventoryVisibleIndexRange range)
        {
            if (!range.IsValid) return 0;

            int rangeSlotCount = range.EndIndex - range.StartIndex + 1;
            return Mathf.Max(0, rangeSlotCount - activeSlots.Count);
        }

        private static bool IsSameRange(InventoryVisibleIndexRange left, InventoryVisibleIndexRange right)
        {
            return left.StartIndex == right.StartIndex && left.EndIndex == right.EndIndex;
        }
        #endregion
    }
}
