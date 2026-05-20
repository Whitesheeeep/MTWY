using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 背包快捷栏 View，负责管理固定数量槽位的创建、复用、刷新和点击转发。
    /// </summary>
    public sealed class InventoryBarView : MonoBehaviour
    {
        private const float BorderWidth = 1f;

        [SerializeField] private InventoryBarSlotView slotPrefab;
        [SerializeField] private Transform slotRoot;
        [SerializeField] private int visibleSlotCount = 10;

        private readonly List<InventoryBarSlotView> slots = new List<InventoryBarSlotView>();
        private readonly List<InventorySlotViewData> currentSlotData = new List<InventorySlotViewData>();
        private Action<int> onSlotClicked;
        private int selectedIndex = -1;

        /// <summary>
        /// 当前快捷栏显示的槽位数量。
        /// </summary>
        public int VisibleSlotCount => Mathf.Max(0, visibleSlotCount);

        /// <summary>
        /// 设置快捷栏运行时显示槽位数量，并立即同步槽位和边框宽度。
        /// </summary>
        /// <param name="slotCount">目标显示槽位数量，小于 0 时按 0 处理。</param>
        public void SetVisibleSlotCount(int slotCount)
        {
            visibleSlotCount = Mathf.Max(0, slotCount);
            EnsureSlots();
            RefreshSlots();
        }

        /// <summary>
        /// 初始化快捷栏点击回调并确保槽位实例存在。
        /// </summary>
        /// <param name="slotClickedCallback">槽位被点击时触发的回调。</param>
        public void Initialize(Action<int> slotClickedCallback)
        {
            onSlotClicked = slotClickedCallback;
            EnsureSlots();
        }

        /// <summary>
        /// 刷新快捷栏槽位显示。
        /// </summary>
        /// <param name="slotDataList">槽位显示数据列表。</param>
        /// <param name="selectedSlotIndex">当前选中的槽位索引，传入负数表示不选中。</param>
        public void RefreshSlots(IReadOnlyList<InventorySlotViewData> slotDataList, int selectedSlotIndex)
        {
            selectedIndex = selectedSlotIndex;
            CacheSlotData(slotDataList);
            EnsureSlots();
            RefreshSlots();
        }

        /// <summary>
        /// 刷新当前选中的槽位显示。
        /// </summary>
        /// <param name="selectedSlotIndex">目标槽位索引，传入负数表示取消选中。</param>
        public void RefreshSelection(int selectedSlotIndex)
        {
            selectedIndex = selectedSlotIndex;
            EnsureSlots();
            RefreshSlots();
        }

        /// <summary>
        /// 清空快捷栏显示。
        /// </summary>
        public void ClearSlots()
        {
            selectedIndex = -1;
            currentSlotData.Clear();
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            visibleSlotCount = Mathf.Max(0, visibleSlotCount);
            if (Application.isPlaying)
            {
                return;
            }

            EditorApplication.delayCall -= ApplyEditorSlotCount;
            EditorApplication.delayCall += ApplyEditorSlotCount;
        }

        private void ApplyEditorSlotCount()
        {
            EditorApplication.delayCall -= ApplyEditorSlotCount;
            if (this == null || Application.isPlaying)
            {
                return;
            }

            EnsureEditorSlots();
            EditorUtility.SetDirty(this);
        }

        private void EnsureEditorSlots()
        {
            slotRoot ??= transform;
            if (slotPrefab == null)
            {
                slotPrefab = GetComponentInChildren<InventoryBarSlotView>(true);
            }

            if (slotPrefab == null || slotRoot == null)
            {
                return;
            }

            List<InventoryBarSlotView> editorSlots = CollectDirectSlotViews();
            int targetCount = VisibleSlotCount;
            while (editorSlots.Count < targetCount)
            {
                InventoryBarSlotView slot = CreateEditorSlot(editorSlots.Count);
                if (slot == null)
                {
                    break;
                }

                editorSlots.Add(slot);
            }

            for (int i = editorSlots.Count - 1; i >= targetCount; i--)
            {
                Undo.DestroyObjectImmediate(editorSlots[i].gameObject);
                editorSlots.RemoveAt(i);
            }

            for (int i = 0; i < editorSlots.Count; i++)
            {
                editorSlots[i].gameObject.name = $"BarItem ({i + 1})";
                editorSlots[i].gameObject.SetActive(true);
                editorSlots[i].Initialize(i, null);
                editorSlots[i].Clear();
                EditorUtility.SetDirty(editorSlots[i]);
            }

            ResizeRootToFitSlots();
        }

        private InventoryBarSlotView CreateEditorSlot(int index)
        {
            GameObject slotObject = null;
            if (PrefabUtility.IsPartOfPrefabAsset(slotPrefab.gameObject))
            {
                slotObject = PrefabUtility.InstantiatePrefab(slotPrefab.gameObject, slotRoot) as GameObject;
            }

            if (slotObject == null)
            {
                slotObject = Instantiate(slotPrefab.gameObject, slotRoot);
            }

            if (slotObject == null)
            {
                return null;
            }

            Undo.RegisterCreatedObjectUndo(slotObject, "Create Inventory Bar Slot");
            slotObject.name = $"BarItem ({index + 1})";
            return slotObject.GetComponent<InventoryBarSlotView>();
        }

        private List<InventoryBarSlotView> CollectDirectSlotViews()
        {
            List<InventoryBarSlotView> result = new List<InventoryBarSlotView>();
            for (int i = 0; i < slotRoot.childCount; i++)
            {
                InventoryBarSlotView slot = slotRoot.GetChild(i).GetComponent<InventoryBarSlotView>();
                if (slot != null)
                {
                    result.Add(slot);
                }
            }

            return result;
        }
#endif

        private void EnsureSlots()
        {
            slotRoot ??= transform;
            if (slotPrefab == null)
            {
                slotPrefab = GetComponentInChildren<InventoryBarSlotView>(true);
            }

            if (slotPrefab == null)
            {
                Debug.LogWarning("[InventoryBarView] 缺少槽位预制体，无法创建快捷栏槽位。", this);
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
                slot.gameObject.name = $"BarItem ({i + 1})";
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

            ResizeRootToFitSlots();
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

        private void CacheSlotData(IReadOnlyList<InventorySlotViewData> slotDataList)
        {
            currentSlotData.Clear();
            int targetCount = VisibleSlotCount;
            for (int i = 0; i < targetCount; i++)
            {
                currentSlotData.Add(GetSlotData(slotDataList, i));
            }
        }

        private void RefreshSlots()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].gameObject.activeSelf)
                {
                    continue;
                }

                slots[i].Refresh(GetSlotData(currentSlotData, i), i == selectedIndex);
            }
        }

        private static InventorySlotViewData GetSlotData(IReadOnlyList<InventorySlotViewData> dataList, int index)
        {
            if (dataList == null || index < 0 || index >= dataList.Count)
            {
                return InventorySlotViewData.Empty(index);
            }

            return dataList[index];
        }

        private void ResizeRootToFitSlots()
        {
            RectTransform rootRect = slotRoot as RectTransform;
            RectTransform slotRect = slotPrefab != null ? slotPrefab.GetComponent<RectTransform>() : null;
            if (rootRect == null || slotRect == null)
            {
                return;
            }

            float spacing = 0f;
            UnityEngine.UI.HorizontalLayoutGroup layoutGroup = rootRect.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.padding.left = Mathf.RoundToInt(BorderWidth);
                layoutGroup.padding.right = Mathf.RoundToInt(BorderWidth);
                spacing = layoutGroup.spacing;
            }

            int targetCount = VisibleSlotCount;
            float contentWidth = targetCount <= 0
                ? 0f
                : slotRect.rect.width * targetCount + spacing * Mathf.Max(0, targetCount - 1);
            float targetWidth = contentWidth + BorderWidth * 2f;
            rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(rootRect);
                if (layoutGroup != null)
                {
                    EditorUtility.SetDirty(layoutGroup);
                }
            }
#endif
        }
    }
}
