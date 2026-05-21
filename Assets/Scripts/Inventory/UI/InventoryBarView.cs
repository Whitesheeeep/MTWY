using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using Inventory;
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
        private Action<int> onSlotClicked;
        private bool layoutCacheValid;
        private int lastLayoutSlotCount = -1;
        private float lastLayoutSlotWidth = -1f;
        private float lastLayoutSpacing = -1f;
        private float lastLayoutTargetWidth = -1f;

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
            EnsureSlotInstances(true);
        }

        /// <summary>
        /// 初始化快捷栏点击回调并确保槽位实例存在。
        /// </summary>
        /// <param name="slotClickedCallback">槽位被点击时触发的回调。</param>
        public void Initialize(Action<int> slotClickedCallback)
        {
            onSlotClicked = slotClickedCallback;
            EnsureSlotInstances(true);
        }

        /// <summary>
        /// 刷新指定槽位显示。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <param name="data">槽位显示数据。</param>
        /// <param name="selected">是否选中。</param>
        public void RefreshSlot(int index, InventorySlotViewData data, bool selected)
        {
            EnsureSlotInstances(false);
            if (index < 0 || index >= slots.Count || !slots[index].gameObject.activeSelf)
            {
                return;
            }

            slots[index].Refresh(data, selected);
        }

        /// <summary>
        /// 刷新快捷栏槽位显示。
        /// </summary>
        /// <param name="slotDataList">槽位显示数据列表。</param>
        /// <param name="selectedSlotIndex">当前选中的槽位索引，传入负数表示不选中。</param>
        public void RefreshSlots(IReadOnlyList<InventorySlotViewData> slotDataList, int selectedSlotIndex)
        {
            EnsureSlotInstances(false);
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
            EnsureSlotInstances(false);
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].gameObject.activeSelf)
                {
                    slots[i].RefreshSelection(i == selectedSlotIndex);
                }
            }
        }

        /// <summary>
        /// 清空快捷栏显示。
        /// </summary>
        public void ClearSlots()
        {
            EnsureSlotInstances(false);
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
        }

        private void EnsureEditorSlots()
        {
            bool changed = false;
            if (slotRoot == null)
            {
                Undo.RecordObject(this, "Assign Inventory Bar Slot Root");
                slotRoot = transform;
                changed = true;
            }

            if (slotPrefab == null)
            {
                InventoryBarSlotView foundSlotPrefab = GetComponentInChildren<InventoryBarSlotView>(true);
                if (foundSlotPrefab != null)
                {
                    Undo.RecordObject(this, "Assign Inventory Bar Slot Prefab");
                    slotPrefab = foundSlotPrefab;
                    changed = true;
                }
            }

            if (slotPrefab == null || slotRoot == null)
            {
                if (changed)
                {
                    EditorUtility.SetDirty(this);
                }

                return;
            }

            List<InventoryBarSlotView> editorSlots = CollectDirectSlotViews();
            int targetCount = VisibleSlotCount;
            SyncInventoryManagerBarCapacity(targetCount);
            while (editorSlots.Count < targetCount)
            {
                InventoryBarSlotView slot = CreateEditorSlot(editorSlots.Count);
                if (slot == null)
                {
                    break;
                }

                editorSlots.Add(slot);
                changed = true;
            }

            for (int i = editorSlots.Count - 1; i >= targetCount; i--)
            {
                Undo.DestroyObjectImmediate(editorSlots[i].gameObject);
                editorSlots.RemoveAt(i);
                changed = true;
            }

            for (int i = 0; i < editorSlots.Count; i++)
            {
                GameObject slotObject = editorSlots[i].gameObject;
                string targetName = $"BarItem ({i + 1})";
                if (slotObject.name != targetName)
                {
                    Undo.RecordObject(slotObject, "Rename Inventory Bar Slot");
                    slotObject.name = targetName;
                    EditorUtility.SetDirty(slotObject);
                    changed = true;
                }

                if (!slotObject.activeSelf)
                {
                    Undo.RecordObject(slotObject, "Activate Inventory Bar Slot");
                    slotObject.SetActive(true);
                    EditorUtility.SetDirty(slotObject);
                    changed = true;
                }
            }

            changed |= SyncLayoutSizeIfNeeded();
            if (changed)
            {
                EditorUtility.SetDirty(this);
            }
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

        private void SyncInventoryManagerBarCapacity(int targetCount)
        {
            InventoryManager manager = UnityEngine.Object.FindFirstObjectByType<InventoryManager>();
            if (manager == null) return;

            SerializedObject serializedManager = new SerializedObject(manager);
            SerializedProperty barCapacityProperty = serializedManager.FindProperty("barCapacity");
            if (barCapacityProperty == null || barCapacityProperty.intValue == targetCount) return;

            Undo.RecordObject(manager, "Sync Inventory Manager Bar Capacity");
            barCapacityProperty.intValue = targetCount;
            serializedManager.ApplyModifiedProperties();
            EditorUtility.SetDirty(manager);
        }
#endif

        private void EnsureSlotInstances(bool syncLayout)
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
                layoutCacheValid = false;
            }

            int targetCount = VisibleSlotCount;
            for (int i = slots.Count; i < targetCount; i++)
            {
                InventoryBarSlotView slot = Instantiate(slotPrefab, slotRoot);
                slot.gameObject.name = $"BarItem ({i + 1})";
                slots.Add(slot);
                layoutCacheValid = false;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                bool active = i < targetCount;
                if (slots[i].gameObject.activeSelf != active)
                {
                    slots[i].gameObject.SetActive(active);
                    layoutCacheValid = false;
                }

                if (active)
                {
                    slots[i].Initialize(i, OnSlotClicked);
                }
            }

            if (syncLayout)
            {
                SyncLayoutSizeIfNeeded();
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

        private bool SyncLayoutSizeIfNeeded()
        {
            RectTransform rootRect = slotRoot as RectTransform;
            RectTransform slotRect = slotPrefab != null ? slotPrefab.GetComponent<RectTransform>() : null;
            if (rootRect == null || slotRect == null)
            {
                return false;
            }

            bool changed = false;
            float spacing = 0f;
            UnityEngine.UI.HorizontalLayoutGroup layoutGroup = rootRect.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            int borderPadding = Mathf.RoundToInt(BorderWidth);
            if (layoutGroup != null)
            {
                if (layoutGroup.padding.left != borderPadding || layoutGroup.padding.right != borderPadding)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        Undo.RecordObject(layoutGroup, "Resize Inventory Bar Layout");
                    }
#endif
                    layoutGroup.padding.left = borderPadding;
                    layoutGroup.padding.right = borderPadding;
                    changed = true;
                }

                spacing = layoutGroup.spacing;
            }

            int targetCount = VisibleSlotCount;
            float slotWidth = slotRect.rect.width;
            float contentWidth = targetCount <= 0
                ? 0f
                : slotWidth * targetCount + spacing * Mathf.Max(0, targetCount - 1);
            float targetWidth = contentWidth + BorderWidth * 2f;

            bool layoutValueCached = layoutCacheValid &&
                                     lastLayoutSlotCount == targetCount &&
                                     Mathf.Approximately(lastLayoutSlotWidth, slotWidth) &&
                                     Mathf.Approximately(lastLayoutSpacing, spacing) &&
                                     Mathf.Approximately(lastLayoutTargetWidth, targetWidth);
            bool rootWidthMatched = Mathf.Approximately(rootRect.rect.width, targetWidth);
            bool paddingMatched = layoutGroup == null ||
                                  (layoutGroup.padding.left == borderPadding && layoutGroup.padding.right == borderPadding);
            if (!changed && layoutValueCached && rootWidthMatched && paddingMatched)
            {
                return false;
            }

            if (!Mathf.Approximately(rootRect.rect.width, targetWidth))
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Undo.RecordObject(rootRect, "Resize Inventory Bar Root");
                }
#endif
                rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
                changed = true;
            }

            layoutCacheValid = true;
            lastLayoutSlotCount = targetCount;
            lastLayoutSlotWidth = slotWidth;
            lastLayoutSpacing = spacing;
            lastLayoutTargetWidth = targetWidth;

#if UNITY_EDITOR
            if (!Application.isPlaying && changed)
            {
                EditorUtility.SetDirty(rootRect);
                if (layoutGroup != null)
                {
                    EditorUtility.SetDirty(layoutGroup);
                }
            }
#endif
            return changed;
        }
    }
}
