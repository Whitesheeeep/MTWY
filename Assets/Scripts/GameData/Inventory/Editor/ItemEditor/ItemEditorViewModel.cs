using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameData.Editor
{
    internal sealed class ItemEditorViewModel
    {
        private const string DefaultDataListPath = "Assets/Scripts/GameData/Inventory/SO/ItemDataList.asset";

        // ViewModel 只保存编辑状态，不直接引用任何 UI 控件。
        private readonly List<ItemData> filteredItems = new();
        private string searchKeyword = string.Empty;

        // 按变化范围拆分事件，View 可以只刷新受影响的 UI 区域。
        // SO 的 DataList 变化会导致整个列表结构改变，Items 变化则是内容改变但结构不变，Selection 变化只影响选中状态。
        public event Action DataListChanged;
        public event Action ItemsChanged;
        public event Action SelectionChanged;

        public ItemDataList_SO DataList { get; private set; }
        public ItemData SelectedItem { get; private set; }
        public IReadOnlyList<ItemData> FilteredItems => filteredItems;
        public string SearchKeyword => searchKeyword;

        public void LoadDefaultDataList()
        {
            SetDataList(AssetDatabase.LoadAssetAtPath<ItemDataList_SO>(DefaultDataListPath));
        }

        public void SetDataList(ItemDataList_SO dataList)
        {
            // 切换数据源时重建筛选结果，并默认选中第一条可编辑数据。
            DataList = dataList;
            EnsureList();
            SelectedItem = DataList?.items.FirstOrDefault();
            RefreshFilter();
            DataListChanged?.Invoke();
            ItemsChanged?.Invoke();
            SelectionChanged?.Invoke();
        }

        public void SetSearchKeyword(string keyword)
        {
            searchKeyword = keyword ?? string.Empty;
            RefreshFilter();

            // 当前选中项被搜索条件过滤掉时，自动移动到新的第一条结果。
            if (SelectedItem != null && !filteredItems.Contains(SelectedItem))
            {
                SelectedItem = filteredItems.FirstOrDefault();
                SelectionChanged?.Invoke();
            }

            ItemsChanged?.Invoke();
        }

        public void Select(ItemData item)
        {
            if (SelectedItem == item)
            {
                return;
            }

            SelectedItem = item;
            SelectionChanged?.Invoke();
        }

        public void AddItem()
        {
            if (DataList == null)
            {
                return;
            }

            EnsureList();
            // ScriptableObject 内部 List 修改需要记录宿主 SO，才能正确 Undo。
            Undo.RecordObject(DataList, "Add Item");

            ItemData item = new ItemData
            {
                Id = GetNextId(),
                name = "New Item",
                itemType = E_ItemType.None,
                itemUseRadius = 1,
                canPickedUp = true,
                sellPercent = 0
            };

            DataList.items.Add(item);
            SelectedItem = item;
            MarkDirty();
            RefreshFilter();
            ItemsChanged?.Invoke();
            SelectionChanged?.Invoke();
        }

        public void DeleteSelected()
        {
            if (DataList == null || SelectedItem == null)
            {
                return;
            }

            EnsureList();
            int index = DataList.items.IndexOf(SelectedItem);
            if (index < 0)
            {
                return;
            }

            Undo.RecordObject(DataList, "Delete Item");
            DataList.items.RemoveAt(index);
            // 删除后优先保持相近位置的选择，减少列表跳动。
            SelectedItem = DataList.items.Count == 0 ? null : DataList.items[Mathf.Clamp(index, 0, DataList.items.Count - 1)];
            MarkDirty();
            RefreshFilter();
            ItemsChanged?.Invoke();
            SelectionChanged?.Invoke();
        }

        public void EditSelected(Action<ItemData> editAction, string undoName = "Edit Item")
        {
            if (DataList == null || SelectedItem == null || editAction == null)
            {
                return;
            }

            // 所有字段编辑统一入口，保证 Undo、Dirty 和刷新事件不会遗漏。
            Undo.RecordObject(DataList, undoName);
            editAction(SelectedItem);
            MarkDirty();
            RefreshFilter();
            ItemsChanged?.Invoke();
            SelectionChanged?.Invoke();
        }

        public void Save()
        {
            if (DataList != null)
            {
                EditorUtility.SetDirty(DataList);
            }

            AssetDatabase.SaveAssets();
        }

        public void HandleUndoRedo()
        {
            if (DataList == null)
            {
                return;
            }

            // Undo/Redo 不经过 EditSelected，这里重新同步派生状态和选中引用。
            int? selectedId = SelectedItem?.Id;
            EnsureList();
            RefreshFilter();

            if (SelectedItem == null || !DataList.items.Contains(SelectedItem) || !filteredItems.Contains(SelectedItem))
            {
                SelectedItem = selectedId.HasValue
                    ? filteredItems.FirstOrDefault(item => item != null && item.Id == selectedId.Value)
                    : null;
                if (SelectedItem == null)
                {
                    SelectedItem = filteredItems.FirstOrDefault();
                }
            }

            ItemsChanged?.Invoke();
            SelectionChanged?.Invoke();
        }

        private void EnsureList()
        {
            // 兼容旧资产或手动创建资产时 items 为空的情况。
            if (DataList != null && DataList.items == null)
            {
                DataList.items = new List<ItemData>();
                MarkDirty();
            }
        }

        private void RefreshFilter()
        {
            // 筛选结果是 ViewModel 的派生状态，View 不参与搜索规则判断。
            filteredItems.Clear();
            if (DataList?.items == null)
            {
                return;
            }

            string keyword = searchKeyword.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                filteredItems.AddRange(DataList.items);
                return;
            }

            string lower = keyword.ToLowerInvariant();
            foreach (ItemData item in DataList.items)
            {
                if (item == null)
                {
                    continue;
                }

                if (item.Id.ToString().Contains(lower) ||
                    (item.name ?? string.Empty).ToLowerInvariant().Contains(lower) ||
                    item.itemType.ToString().ToLowerInvariant().Contains(lower))
                {
                    filteredItems.Add(item);
                }
            }
        }

        private int GetNextId()
        {
            if (DataList?.items == null || DataList.items.Count == 0)
            {
                return 1001;
            }

            // 采用当前最大 ID + 1，避免删除中间项后复用旧 ID。
            return DataList.items.Max(item => item?.Id ?? 0) + 1;
        }

        private void MarkDirty()
        {
            // 修改嵌套数据后必须标记宿主 SO，否则 Unity 可能不会保存到资产。
            if (DataList != null)
            {
                EditorUtility.SetDirty(DataList);
            }
        }
    }
}
