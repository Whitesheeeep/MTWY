using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 背包快捷栏 View，负责固定槽位布局配置和 Editor 初始化同步。
    /// </summary>
    public sealed class InventoryBarView : InventorySlotContainerViewBase<InventoryBarViewModel>
    {
        #region Fields
        [SerializeField] private int visibleSlotCount = 10;
        [SerializeField] private InventoryFixedSlotViewLayout fixedLayout = new InventoryFixedSlotViewLayout();
        #endregion

        #region Properties
        /// <inheritdoc />
        public int VisibleSlotCount => Mathf.Max(0, visibleSlotCount);

        /// <inheritdoc />
        protected override IInventorySlotViewLayout SlotLayout => fixedLayout;
        #endregion

        #region Public Methods
        /// <summary>
        /// 设置快捷栏运行时显示槽位数量。
        /// </summary>
        /// <param name="slotCount">目标显示槽位数量，小于 0 时按 0 处理。</param>
        public void SetVisibleSlotCount(int slotCount)
        {
            visibleSlotCount = Mathf.Max(0, slotCount);
            ConfigureLayout();
            fixedLayout.SetSlotCount(visibleSlotCount);
        }
        #endregion

        #region Unity LifeCycle
        private void Reset()
        {
            SlotRoot = transform;
        }
        #endregion

        #region Layout
        protected override void ConfigureLayout()
        {
            EnsureReferences();
            base.ConfigureLayout();
        }

        private void EnsureReferences()
        {
            SlotRoot ??= transform;
            if (SlotPrefab == null) SlotPrefab = GetComponentInChildren<InventorySlotView>(true);
        }
        #endregion

#if UNITY_EDITOR
        #region Editor
        private void OnValidate()
        {
            visibleSlotCount = Mathf.Max(0, visibleSlotCount);
            if (Application.isPlaying) return;

            EditorApplication.delayCall -= ApplyEditorSlotCount;
            EditorApplication.delayCall += ApplyEditorSlotCount;
        }

        private void ApplyEditorSlotCount()
        {
            EditorApplication.delayCall -= ApplyEditorSlotCount;
            if (this == null || Application.isPlaying) return;

            EnsureEditorSlots();
        }

        private void EnsureEditorSlots()
        {
            bool changed = false;
            if (SlotRoot == null)
            {
                Undo.RecordObject(this, "Assign Inventory Bar Slot Root");
                SlotRoot = transform;
                changed = true;
            }

            if (SlotPrefab == null)
            {
                InventorySlotView foundSlotPrefab = GetComponentInChildren<InventorySlotView>(true);
                if (foundSlotPrefab != null)
                {
                    Undo.RecordObject(this, "Assign Inventory Bar Slot Prefab");
                    SlotPrefab = foundSlotPrefab;
                    changed = true;
                }
            }

            if (SlotPrefab == null || SlotRoot == null)
            {
                if (changed) EditorUtility.SetDirty(this);
                return;
            }

            List<InventorySlotView> editorSlots = CollectDirectSlotViews();
            int targetCount = VisibleSlotCount;
            while (editorSlots.Count < targetCount)
            {
                InventorySlotView slot = CreateEditorSlot(editorSlots.Count);
                if (slot == null) break;

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

            ConfigureLayout();
            fixedLayout.SetSlotCount(targetCount);
            if (changed) EditorUtility.SetDirty(this);
        }

        private InventorySlotView CreateEditorSlot(int index)
        {
            GameObject slotObject = null;
            if (PrefabUtility.IsPartOfPrefabAsset(SlotPrefab.gameObject))
                slotObject = PrefabUtility.InstantiatePrefab(SlotPrefab.gameObject, SlotRoot) as GameObject;

            if (slotObject == null) slotObject = Instantiate(SlotPrefab.gameObject, SlotRoot);
            if (slotObject == null) return null;

            Undo.RegisterCreatedObjectUndo(slotObject, "Create Inventory Bar Slot");
            slotObject.name = $"BarItem ({index + 1})";
            return slotObject.GetComponent<InventorySlotView>();
        }

        private List<InventorySlotView> CollectDirectSlotViews()
        {
            List<InventorySlotView> result = new List<InventorySlotView>();
            for (int i = 0; i < SlotRoot.childCount; i++)
            {
                InventorySlotView slot = SlotRoot.GetChild(i).GetComponent<InventorySlotView>();
                if (slot != null) result.Add(slot);
            }

            return result;
        }

        #endregion
#endif
    }
}
