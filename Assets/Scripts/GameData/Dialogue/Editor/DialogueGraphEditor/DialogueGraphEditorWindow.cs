using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameData.Editor
{
    /// <summary>
    /// 对话图编辑器窗口，负责加载 UXML、创建 ViewModel/View，并在播放模式下同步当前运行节点高亮。
    /// </summary>
    public sealed class DialogueGraphEditorWindow : EditorWindow
    {
        #region 常量
        private const string WindowTitle = "Dialogue Graph";
        private const string WindowUxmlPath = "Assets/Scripts/GameData/Dialogue/Editor/DialogueGraphEditor/DialogueGraphEditorWindow.uxml";
        #endregion

        #region 字段
        private DialogueGraph_SO initialGraph;
        private DialogueGraphEditorViewModel viewModel;
        private DialogueGraphEditorView view;
        private IDialogueRunnerController runtimeController;
        #endregion

        #region 打开窗口
        [MenuItem("Tools/GameData/Dialogue Graph Editor")]
        private static void ShowWindow()
        {
            DialogueGraphEditorWindow window = GetWindow<DialogueGraphEditorWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(980f, 620f);
            window.Show();
        }

        /// <summary>
        /// 打开编辑器窗口并加载指定对话图资源。
        /// </summary>
        /// <param name="graph">要编辑的对话图资源。</param>
        public static void Open(DialogueGraph_SO graph)
        {
            DialogueGraphEditorWindow window = GetWindow<DialogueGraphEditorWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(980f, 620f);
            window.initialGraph = graph;
            window.Show();

            if (window.viewModel != null)
            {
                window.viewModel.SetGraph(graph);
            }
        }
        #endregion

        #region 生命周期
        private void CreateGUI()
        {
            rootVisualElement.Clear();

            VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WindowUxmlPath);
            if (uxml == null)
            {
                rootVisualElement.Add(new HelpBox($"Missing UXML: {WindowUxmlPath}", HelpBoxMessageType.Error));
                return;
            }

            uxml.CloneTree(rootVisualElement);

            viewModel = new DialogueGraphEditorViewModel();
            view = new DialogueGraphEditorView(rootVisualElement, viewModel);
            view.Bind();

            if (initialGraph != null)
            {
                viewModel.SetGraph(initialGraph);
            }
            else if (Selection.activeObject is DialogueGraph_SO selectedGraph)
            {
                viewModel.SetGraph(selectedGraph);
            }

            RefreshSelectionContext();
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += HandleUndoRedoPerformed;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleUndoRedoPerformed;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            view?.Dispose();
        }

        private void OnSelectionChange()
        {
            RefreshSelectionContext();
        }

        private void OnInspectorUpdate()
        {
            RefreshRuntimeCurrentNode();
        }
        #endregion

        #region 事件处理
        private void HandleUndoRedoPerformed()
        {
            viewModel?.HandleUndoRedo();
        }

        private void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state is PlayModeStateChange.EnteredPlayMode or PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += RefreshSelectionContext;
            }

            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                runtimeController = null;
                view?.SetRuntimeCurrentNode(null);
            }
        }
        #endregion

        #region 选择与运行时同步
        private void RefreshSelectionContext()
        {
            if (viewModel == null)
            {
                return;
            }

            runtimeController = Application.isPlaying ? FindSelectedRunnerController() : null;
            if (runtimeController?.Graph != null)
            {
                viewModel.SetGraph(runtimeController.Graph);
                return;
            }

            view?.SetRuntimeCurrentNode(null);
            if (!Application.isPlaying && Selection.activeObject is DialogueGraph_SO selectedGraph)
            {
                viewModel.SetGraph(selectedGraph);
            }
        }

        private void RefreshRuntimeCurrentNode()
        {
            if (!Application.isPlaying)
            {
                view?.SetRuntimeCurrentNode(null);
                return;
            }

            IDialogueRunnerController selectedController = FindSelectedRunnerController();
            if (selectedController != runtimeController)
            {
                runtimeController = selectedController;
                if (runtimeController?.Graph != null && runtimeController.Graph != viewModel?.Graph)
                {
                    viewModel?.SetGraph(runtimeController.Graph);
                }
            }

            DialogueNode currentNode = runtimeController?.Runner?.CurrentNode;
            view?.SetRuntimeCurrentNode(currentNode);
        }

        private static IDialogueRunnerController FindSelectedRunnerController()
        {
            if (Selection.activeGameObject == null)
            {
                return null;
            }

            return Selection.activeGameObject
                .GetComponents<MonoBehaviour>()
                .OfType<IDialogueRunnerController>()
                .FirstOrDefault();
        }
        #endregion
    }
}
