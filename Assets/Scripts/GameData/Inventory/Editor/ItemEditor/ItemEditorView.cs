using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameData.Editor
{
    internal sealed class ItemEditorView
    {
        private readonly VisualElement root;
        private readonly ItemEditorViewModel viewModel;
        private readonly VisualTreeAsset itemRowTemplate;
        private readonly List<ItemRowViewData> rows = new();

        private ObjectField dataListField;
        private TextField searchField;
        private ListView itemListView;
        private Button addButton;
        private Button deleteButton;
        private Button saveButton;
        private VisualElement detailContent;
        private VisualElement emptyState;
        private Image iconPreview;
        private IntegerField idField;
        private TextField nameField;
        private EnumField typeField;
        private ObjectField iconField;
        private ObjectField worldIconField;
        private TextField descriptionField;
        private IntegerField useRadiusField;
        private Toggle canPickupToggle;
        private Toggle canDroppedToggle;
        private Toggle canCarriedToggle;
        private IntegerField priceField;
        private SliderInt sellPercentSlider;
        private IntegerField sellPercentField;

        // View 主动回填字段时会触发 ValueChanged，这个标记用于避免回填再次写入数据。
        private bool isRefreshing;

        public ItemEditorView(VisualElement root, ItemEditorViewModel viewModel, VisualTreeAsset itemRowTemplate)
        {
            this.root = root;
            this.viewModel = viewModel;
            this.itemRowTemplate = itemRowTemplate;
        }

        public void Bind()
        {
            QueryElements();
            ConfigureListView();
            ConfigureFields();
            RegisterViewModelEvents();
            RefreshAll();
        }

        private void QueryElements()
        {
            // 所有可视结构都来自 UXML，这里只缓存后续绑定需要用到的控件引用。
            dataListField = root.Q<ObjectField>("DataListField");
            searchField = root.Q<TextField>("SearchField");
            itemListView = root.Q<ListView>("ItemListView");
            addButton = root.Q<Button>("AddButton");
            deleteButton = root.Q<Button>("DeleteButton");
            saveButton = root.Q<Button>("SaveButton");
            detailContent = root.Q<VisualElement>("DetailContent");
            emptyState = root.Q<VisualElement>("EmptyState");
            iconPreview = root.Q<Image>("IconPreview");
            idField = root.Q<IntegerField>("IdField");
            nameField = root.Q<TextField>("NameField");
            typeField = root.Q<EnumField>("TypeField");
            iconField = root.Q<ObjectField>("IconField");
            worldIconField = root.Q<ObjectField>("WorldIconField");
            descriptionField = root.Q<TextField>("DescriptionField");
            useRadiusField = root.Q<IntegerField>("UseRadiusField");
            canPickupToggle = root.Q<Toggle>("CanPickupToggle");
            canDroppedToggle = root.Q<Toggle>("CanDroppedToggle");
            canCarriedToggle = root.Q<Toggle>("CanCarriedToggle");
            priceField = root.Q<IntegerField>("PriceField");
            sellPercentSlider = root.Q<SliderInt>("SellPercentSlider");
            sellPercentField = root.Q<IntegerField>("SellPercentField");
        }

        private void ConfigureListView()
        {
            itemListView.fixedItemHeight = 64;
            itemListView.selectionType = SelectionType.Single;
            itemListView.itemsSource = rows;
            // 行结构由独立 UXML 负责，代码只克隆模板并写入数据。
            itemListView.makeItem = MakeItemRow;
            itemListView.bindItem = BindItemRow;
            itemListView.selectionChanged += selection =>
            {
                foreach (object selected in selection)
                {
                    if (selected is ItemRowViewData row)
                    {
                        viewModel.Select(row.Item);
                        return;
                    }
                }

                viewModel.Select(null);
            };
        }

        private VisualElement MakeItemRow()
        {
            // ListView 会复用行元素，行结构必须从模板克隆，避免把样式写死在代码里。
            return itemRowTemplate.CloneTree();
        }

        private void BindItemRow(VisualElement element, int index)
        {
            if (index < 0 || index >= rows.Count)
            {
                return;
            }

            ItemRowViewData row = rows[index];
            // 直接使用 Sprite，避免 AssetPreview 异步生成导致图标刷新延迟。
            element.Q<Image>("RowIcon").sprite = row.Item.icon;
            element.Q<Label>("RowName").text = row.Name;
            element.Q<Label>("RowDetail").text = row.Detail;
        }

        private void ConfigureFields()
        {
            // ObjectField 的类型约束放在 C# 中，UXML 保持纯布局，避免 UI Builder 解析业务类型出错。
            dataListField.objectType = typeof(ItemDataList_SO);
            dataListField.allowSceneObjects = false;
            iconField.objectType = typeof(Sprite);
            iconField.allowSceneObjects = false;
            worldIconField.objectType = typeof(Sprite);
            worldIconField.allowSceneObjects = false;
            typeField.Init(E_ItemType.None);
            sellPercentSlider.lowValue = 0;
            sellPercentSlider.highValue = 100;

            dataListField.RegisterValueChangedCallback(evt => viewModel.SetDataList(evt.newValue as ItemDataList_SO));
            searchField.RegisterValueChangedCallback(evt => viewModel.SetSearchKeyword(evt.newValue));
            addButton.clicked += viewModel.AddItem;
            saveButton.clicked += viewModel.Save;
            deleteButton.clicked += () =>
            {
                ItemData selected = viewModel.SelectedItem;
                if (selected == null)
                {
                    return;
                }

                if (EditorUtility.DisplayDialog("Delete Item", $"Delete item '{selected.name}'?", "Delete", "Cancel"))
                {
                    viewModel.DeleteSelected();
                }
            };

            idField.RegisterValueChangedCallback(evt => Edit(item => item.Id = evt.newValue, "Edit Item ID"));
            nameField.RegisterValueChangedCallback(evt => Edit(item => item.name = evt.newValue, "Edit Item Name"));
            typeField.RegisterValueChangedCallback(evt => Edit(item => item.itemType = (E_ItemType)evt.newValue, "Edit Item Type"));
            iconField.RegisterValueChangedCallback(evt => EditIcon(evt.newValue as Sprite));
            worldIconField.RegisterValueChangedCallback(evt => Edit(item => item.worldIcon = evt.newValue as Sprite, "Edit Item World Sprite"));
            descriptionField.RegisterValueChangedCallback(evt => Edit(item => item.description = evt.newValue, "Edit Item Description"));
            useRadiusField.RegisterValueChangedCallback(evt => Edit(item => item.itemUseRadius = Mathf.Max(0, evt.newValue), "Edit Item Use Radius"));
            canPickupToggle.RegisterValueChangedCallback(evt => Edit(item => item.canPickedUp = evt.newValue, "Edit Item Can Pickup"));
            canDroppedToggle.RegisterValueChangedCallback(evt => Edit(item => item.canDropped = evt.newValue, "Edit Item Can Dropped"));
            canCarriedToggle.RegisterValueChangedCallback(evt => Edit(item => item.canCarried = evt.newValue, "Edit Item Can Carried"));
            priceField.RegisterValueChangedCallback(evt => Edit(item => item.price = Mathf.Max(0, evt.newValue), "Edit Item Price"));
            sellPercentSlider.RegisterValueChangedCallback(evt => Edit(item => item.sellPercent = Mathf.Clamp(evt.newValue, 0, 100), "Edit Item Sell Percent"));
            sellPercentField.RegisterValueChangedCallback(evt => Edit(item => item.sellPercent = Mathf.Clamp(evt.newValue, 0, 100), "Edit Item Sell Percent"));
        }

        private void RegisterViewModelEvents()
        {
            // ViewModel 只暴露数据变化事件，View 根据事件刷新对应区域。
            viewModel.DataListChanged += RefreshDataListField;
            viewModel.ItemsChanged += RefreshList;
            viewModel.SelectionChanged += RefreshDetails;
        }

        private void RefreshAll()
        {
            RefreshDataListField();
            RefreshList();
            RefreshDetails();
        }

        // DataListChanged 就只关注 DataList 不要管 ListItems 和 Selection
        private void RefreshDataListField()
        {
            dataListField.SetValueWithoutNotify(viewModel.DataList);
            searchField.SetValueWithoutNotify(viewModel.SearchKeyword);
        }

        private void RefreshList()
        {
            // 过滤结果由 ViewModel 维护，View 只把结果转换成行显示数据。
            rows.Clear();
            foreach (ItemData item in viewModel.FilteredItems)
            {
                rows.Add(new ItemRowViewData(item));
            }

            itemListView.Rebuild();

            int selectedIndex = rows.FindIndex(row => row.Item == viewModel.SelectedItem);
            if (selectedIndex >= 0)
            {
                itemListView.SetSelectionWithoutNotify(new[] { selectedIndex });
            }
            else
            {
                itemListView.ClearSelection();
            }
        }

        private void RefreshDetails()
        {
            ItemData item = viewModel.SelectedItem;
            bool hasSelection = item != null;
            // 显隐交给 USS class 控制，避免在代码里直接维护布局样式。
            detailContent.EnableInClassList("is-hidden", !hasSelection);
            emptyState.EnableInClassList("is-hidden", hasSelection);
            deleteButton.SetEnabled(hasSelection);

            if (!hasSelection)
            {
                return;
            }

            isRefreshing = true;
            // SetValueWithoutNotify 只同步 UI，不反向触发编辑事件。
            RefreshIconPreview(item.icon);
            idField.SetValueWithoutNotify(item.Id);
            nameField.SetValueWithoutNotify(item.name ?? string.Empty);
            typeField.SetValueWithoutNotify(item.itemType);
            iconField.SetValueWithoutNotify(item.icon);
            worldIconField.SetValueWithoutNotify(item.worldIcon);
            descriptionField.SetValueWithoutNotify(item.description ?? string.Empty);
            useRadiusField.SetValueWithoutNotify(item.itemUseRadius);
            canPickupToggle.SetValueWithoutNotify(item.canPickedUp);
            canDroppedToggle.SetValueWithoutNotify(item.canDropped);
            canCarriedToggle.SetValueWithoutNotify(item.canCarried);
            priceField.SetValueWithoutNotify(item.price);
            sellPercentSlider.SetValueWithoutNotify(Mathf.Clamp(item.sellPercent, 0, 100));
            sellPercentField.SetValueWithoutNotify(Mathf.Clamp(item.sellPercent, 0, 100));
            isRefreshing = false;
        }

        private void EditIcon(Sprite sprite)
        {
            if (isRefreshing)
            {
                return;
            }

            viewModel.EditSelected(item => item.icon = sprite, "Edit Item Icon");
            RefreshIconPreview(sprite);
            itemListView.Rebuild();
        }

        private void Edit(System.Action<ItemData> editAction, string undoName)
        {
            if (isRefreshing)
            {
                return;
            }

            // 所有字段编辑统一走 ViewModel，保证 Undo、Dirty 标记和刷新事件一致。
            viewModel.EditSelected(editAction, undoName);
        }

        private void RefreshIconPreview(Sprite sprite)
        {
            // Image.sprite 会按 Sprite 的裁剪区域显示，比直接使用 sprite.texture 更适合图集资源。
            iconPreview.sprite = sprite;
        }
    }
}
