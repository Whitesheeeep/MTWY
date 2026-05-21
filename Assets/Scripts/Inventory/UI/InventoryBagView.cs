using System;
using System.Collections.Generic;
using UnityEngine;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 背包窗口 View，负责 Bag 槽位的创建、复用、刷新和点击转发。
    /// </summary>
    public sealed class InventoryBagView : MonoBehaviour
    {
        [SerializeField] private InventoryBarSlotView slotPrefab;
        [SerializeField] private Transform slotRoot;
        [SerializeField] private int visibleSlotCount = 30;

        private readonly List<InventoryBarSlotView> slots = new List<InventoryBarSlotView>();
        private Action<int> onSlotClicked;

        /// <summary>
        /// 当前背包窗口显示的槽位数量。
        /// </summary>
        public int VisibleSlotCount => Mathf.Max(0, visibleSlotCount);

        /// <summary>
        /// 设置背包窗口运行时显示槽位数量。
        /// </summary>
        /// <param name="slotCount">目标显示槽位数量，小于 0 时按 0 处理。</param>
        public void SetVisibleSlotCount(int slotCount)
        {
            visibleSlotCount = Mathf.Max(0, slotCount);
            EnsureSlots();
        }

        /// <summary>
        /// 初始化背包槽位点击回调并确保槽位实例存在。
        /// </summary>
        /// <param name="slotClickedCallback">槽位被点击时触发的回调。</param>
        public void Initialize(Action<int> slotClickedCallback)
        {
            onSlotClicked = slotClickedCallback;
            EnsureSlots();
        }

        /// <summary>
        /// 刷新指定槽位显示。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <param name="data">槽位显示数据。</param>
        /// <param name="selected">是否选中。</param>
        public void RefreshSlot(int index, InventorySlotViewData data, bool selected)
        {
            EnsureSlots();
            if (index < 0 || index >= slots.Count || !slots[index].gameObject.activeSelf)
            {
                return;
            }

            slots[index].Refresh(data, selected);
        }

        /// <summary>
        /// 刷新背包窗口槽位显示。
        /// </summary>
        /// <param name="slotDataList">槽位显示数据列表。</param>
        /// <param name="selectedSlotIndex">当前选中的槽位索引，传入负数表示不选中。</param>
        public void RefreshSlots(IReadOnlyList<InventorySlotViewData> slotDataList, int selectedSlotIndex)
        {
            EnsureSlots();
            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].gameObject.activeSelf)
                {
                    continue;
                }

                slots[i].Refresh(GetSlotData(slotDataList, i), i == selectedSlotIndex);
            }
        }

        /// <summary>
        /// 刷新当前选中的槽位显示。
        /// </summary>
        /// <param name="selectedSlotIndex">目标槽位索引，传入负数表示取消选中。</param>
        public void RefreshSelection(int selectedSlotIndex)
        {
            EnsureSlots();
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].gameObject.activeSelf)
                {
                    slots[i].RefreshSelection(i == selectedSlotIndex);
                }
            }
        }

        /// <summary>
        /// 清空背包窗口显示。
        /// </summary>
        public void ClearSlots()
        {
            EnsureSlots();
            foreach (InventoryBarSlotView slot in slots)
            {
                slot.Clear();
            }
        }

        private void Reset()
        {
            slotRoot = transform;
        }

        private void EnsureSlots()
        {
            slotRoot ??= transform;
            if (slotPrefab == null)
            {
                slotPrefab = GetComponentInChildren<InventoryBarSlotView>(true);
            }

            if (slotPrefab == null)
            {
                Debug.LogWarning("[InventoryBagView] 缺少槽位预制体，无法创建背包槽位。", this);
                return;
            }

            if (!slots.Contains(slotPrefab))
            {
                slots.Clear();
                CollectExistingSlots();
            }

            int targetCount = VisibleSlotCount;
            for (int i = slots.Count; i < targetCount; i++)
            {
                InventoryBarSlotView slot = Instantiate(slotPrefab, slotRoot);
                slot.gameObject.name = $"BagItem ({i + 1})";
                slots.Add(slot);
            }

            for (int i = 0; i < slots.Count; i++)
            {
                bool active = i < targetCount;
                slots[i].gameObject.SetActive(active);
                if (active)
                {
                    slots[i].Initialize(i, OnSlotClicked);
                }
            }
        }

        private void CollectExistingSlots()
        {
            slotRoot ??= transform;
            slotRoot.GetComponentsInChildren(true, slots);
        }

        private void OnSlotClicked(int index)
        {
            onSlotClicked?.Invoke(index);
        }

        private static InventorySlotViewData GetSlotData(IReadOnlyList<InventorySlotViewData> dataList, int index)
        {
            if (dataList == null || index < 0 || index >= dataList.Count)
            {
                return InventorySlotViewData.Empty(index);
            }

            return dataList[index];
        }
    }
}
