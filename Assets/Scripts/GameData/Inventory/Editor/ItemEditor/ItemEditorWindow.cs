using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameData.Editor
{
    public sealed class ItemEditorWindow : EditorWindow
    {
        private const string WindowTitle = "ItemEditor";
        private const string DefaultUxmlPath = "Assets/Scripts/GameData/Inventory/Editor/ItemEditor/ItemEditorWindow.uxml";
        private const string ItemRowUxmlPath = "Assets/Scripts/GameData/Inventory/Editor/ItemEditor/ItemListRow.uxml";

        private ItemDataList_SO initialDataList;
        private ItemEditorViewModel viewModel;
        private ItemEditorView view;

        [MenuItem("Tools/GameData/Item Editor")]
        private static void ShowWindow()
        {
            ItemEditorWindow window = GetWindow<ItemEditorWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(920f, 560f);
            window.Show();
        }

        public static void Open(ItemDataList_SO dataList)
        {
            ItemEditorWindow window = GetWindow<ItemEditorWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(920f, 560f);
            window.initialDataList = dataList;
            window.Show();

            // 窗口已经创建时直接切换数据；未创建时交给 CreateGUI 初始化。
            if (window.viewModel != null)
            {
                window.viewModel.SetDataList(dataList);
            }
        }

        private void CreateGUI()
        {
            rootVisualElement.Clear();

            VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DefaultUxmlPath);
            if (uxml == null)
            {
                rootVisualElement.Add(new HelpBox($"Missing UXML: {DefaultUxmlPath}", HelpBoxMessageType.Error));
                return;
            }

            // EditorWindow 统一负责装配窗口和列表单元模板，View 只处理绑定逻辑。
            VisualTreeAsset itemRowTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ItemRowUxmlPath);
            if (itemRowTemplate == null)
            {
                rootVisualElement.Add(new HelpBox($"Missing UXML: {ItemRowUxmlPath}", HelpBoxMessageType.Error));
                return;
            }

            uxml.CloneTree(rootVisualElement);

            viewModel = new ItemEditorViewModel();
            if (initialDataList != null)
            {
                viewModel.SetDataList(initialDataList);
            }
            else
            {
                viewModel.LoadDefaultDataList();
            }

            view = new ItemEditorView(rootVisualElement, viewModel, itemRowTemplate);
            view.Bind();
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += HandleUndoRedoPerformed;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleUndoRedoPerformed;
        }

        private void HandleUndoRedoPerformed()
        {
            // Undo/Redo 由 Unity 直接改回 ScriptableObject，需要主动通知 MVVM 刷新。
            viewModel?.HandleUndoRedo();
        }
    }
}
