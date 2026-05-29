using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 固定槽位布局，适用于快捷栏这类按顺序显示全部槽位的容器。
    /// </summary>
    [Serializable]
    public sealed class InventoryFixedSlotViewLayout : IInventorySlotViewLayout
    {
        #region Fields
        private readonly List<InventorySlotView> slots = new List<InventorySlotView>();
        private InventorySlotView slotPrefab;
        private Transform slotRoot;
        private InventorySlotViewEventModule eventModule;
        private int visibleSlotCount;
        private bool slotsDirty = true;
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
            if (this.slotPrefab != slotPrefab || this.slotRoot != slotRoot) slotsDirty = true;

            this.slotPrefab = slotPrefab;
            this.slotRoot = slotRoot;
            this.eventModule = eventModule;
        }

        /// <inheritdoc />
        public void SetVisibleSlotCount(int count)
        {
            int targetCount = Mathf.Max(0, count);
            if (visibleSlotCount != targetCount) slotsDirty = true;

            visibleSlotCount = targetCount;
            EnsureSlotInstances();
        }

        /// <inheritdoc />
        public void RefreshSlot(int index, InventorySlotViewData data, bool selected)
        {
            if (!TryGetSlot(index, out InventorySlotView slot)) return;

            slot.Refresh(data, selected);
        }

        /// <inheritdoc />
        public void RefreshSlots(IReadOnlyList<InventorySlotViewData> dataList, int selectedIndex)
        {
            EnsureSlotInstances();
            for (int i = 0; i < slots.Count; i++)
                slots[i].Refresh(GetSlotData(dataList, i), i == selectedIndex);
        }

        /// <inheritdoc />
        public void RefreshSelection(int selectedIndex)
        {
            if (!AreSlotsReady()) return;

            for (int i = 0; i < slots.Count; i++)
                slots[i].RefreshSelection(i == selectedIndex);
        }

        /// <inheritdoc />
        public void RefreshDropPreview(int index, bool canDrop)
        {
            if (!TryGetSlot(index, out InventorySlotView slot)) return;

            slot.RefreshDropPreview(canDrop);
        }

        /// <inheritdoc />
        public void ClearDropPreview()
        {
            foreach (var slot in slots.Where(slot => slot != null)) slot.ClearDropPreview();
        }

        /// <inheritdoc />
        public void ClearSlots()
        {
            if (!AreSlotsReady()) return;

            foreach (InventorySlotView slot in slots)
                if (slot != null) slot.Clear();
        }
        #endregion

        #region Slot Instance
        private void EnsureSlotInstances()
        {
            if (!slotsDirty && slots.Count == VisibleSlotCount) return;

            if (slotRoot == null || slotPrefab == null)
            {
                Debug.LogWarning("[InventoryFixedSlotViewLayout] 缺少槽位预制体或根节点，无法创建槽位。");
                return;
            }

            CollectDirectSlots();
            int targetCount = VisibleSlotCount;
            for (int i = slots.Count; i < targetCount; i++)
            {
                InventorySlotView slot = UnityEngine.Object.Instantiate(slotPrefab, slotRoot);
                slot.gameObject.name = $"BarItem ({i + 1})";
                slots.Add(slot);
            }

            for (int i = slots.Count - 1; i >= targetCount; i--)
            {
                DestroySlot(slots[i]);
                slots.RemoveAt(i);
            }

            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlotView slot = slots[i];
                slot.gameObject.name = $"BarItem ({i + 1})";
                if (!slot.gameObject.activeSelf) slot.gameObject.SetActive(true);
                slot.Initialize(i, eventModule);
            }

            slotsDirty = false;
        }

        private void CollectDirectSlots()
        {
            slots.Clear();
            if (slotRoot == null) return;

            for (int i = 0; i < slotRoot.childCount; i++)
            {
                InventorySlotView slot = slotRoot.GetChild(i).GetComponent<InventorySlotView>();
                if (slot != null) slots.Add(slot);
            }
        }

        private bool AreSlotsReady()
        {
            return !slotsDirty && slots.Count == VisibleSlotCount;
        }

        private bool TryGetSlot(int index, out InventorySlotView slot)
        {
            slot = null;
            if (!AreSlotsReady() || index < 0 || index >= slots.Count) return false;

            slot = slots[index];
            return slot != null;
        }

        private static void DestroySlot(InventorySlotView slot)
        {
            if (slot == null) return;

            if (Application.isPlaying) UnityEngine.Object.Destroy(slot.gameObject);
            else UnityEngine.Object.DestroyImmediate(slot.gameObject);
        }
        #endregion

        #region Data
        private static InventorySlotViewData GetSlotData(IReadOnlyList<InventorySlotViewData> dataList, int index)
        {
            if (dataList == null || index < 0 || index >= dataList.Count) return InventorySlotViewData.Empty(index);

            return dataList[index];
        }
        #endregion
    }
}
