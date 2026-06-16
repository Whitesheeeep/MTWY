using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GameData.Editor
{
    /// <summary>
    /// 对话图编辑器的主 View，负责顶部栏、GraphView、Validation 面板的 UI 绑定。
    /// </summary>
    internal sealed class DialogueGraphEditorView : IDisposable
    {
        #region Fields
        private readonly VisualElement root;
        private readonly DialogueGraphEditorViewModel viewModel;
        private readonly List<Label> validationRows = new();

        private DialogueGraphView graphView;
        private DialogueGraphDetailsView detailsView;
        private ObjectField graphField;
        private TextField graphNameField;
        private Button saveButton;
        private Button autoLayoutButton;
        private Button duplicateButton;
        private VisualElement graphContainer;
        private VisualElement validationList;
        private bool isRefreshing;
        #endregion

        #region Initialize
        /// <summary>
        /// 创建对话图编辑器主 View。
        /// </summary>
        public DialogueGraphEditorView(VisualElement root, DialogueGraphEditorViewModel viewModel)
        {
            this.root = root;
            this.viewModel = viewModel;
        }

        /// <summary>
        /// 查询 UXML 元素、创建子 View 并注册事件。
        /// </summary>
        public void Bind()
        {
            QueryElements();
            ConfigureGraphView();
            ConfigureDetailsView();
            ConfigureFields();
            RegisterViewModelEvents();
            RefreshAll();
        }
        #endregion

        #region Lifecycle
        /// <summary>
        /// 释放 ViewModel 事件订阅与子 View 绑定。
        /// </summary>
        public void Dispose()
        {
            UnregisterViewModelEvents();
            detailsView?.Dispose();
        }
        #endregion

        #region Runtime Highlight
        /// <summary>
        /// 设置 GraphView 中的运行时当前节点高亮。
        /// </summary>
        public void SetRuntimeCurrentNode(DialogueNode node)
        {
            graphView?.SetRuntimeCurrentNode(node);
        }
        #endregion

        #region UXML
        private void QueryElements()
        {
            graphField = root.Q<ObjectField>("GraphField");
            graphNameField = root.Q<TextField>("GraphNameField");
            saveButton = root.Q<Button>("SaveButton");
            autoLayoutButton = root.Q<Button>("AutoLayoutButton");
            duplicateButton = root.Q<Button>("DuplicateButton");
            graphContainer = root.Q<VisualElement>("GraphContainer");
            validationList = root.Q<VisualElement>("ValidationList");
        }

        private void ConfigureGraphView()
        {
            graphView = new DialogueGraphView(viewModel);
            graphView.StretchToParentSize();
            graphContainer.Add(graphView);
        }

        private void ConfigureDetailsView()
        {
            detailsView = new DialogueGraphDetailsView(root, viewModel);
            detailsView.Bind();
        }

        private void ConfigureFields()
        {
            graphField.objectType = typeof(DialogueGraph_SO);
            graphField.allowSceneObjects = false;
            graphField.RegisterValueChangedCallback(evt => viewModel.SetGraph(evt.newValue as DialogueGraph_SO));

            graphNameField.RegisterValueChangedCallback(evt =>
            {
                if (!isRefreshing)
                {
                    viewModel.SetGraphDisplayName(evt.newValue);
                }
            });

            saveButton.clicked += viewModel.Save;
            autoLayoutButton.clicked += graphView.AutoLayoutGraph;
            duplicateButton.clicked += DuplicateCurrentGraph;
        }
        #endregion

        #region ViewModel Events
        private void RegisterViewModelEvents()
        {
            viewModel.GraphChanged += RefreshGraph;
            viewModel.NodeDataChanged += RefreshNodeData;
            viewModel.ValidationChanged += RefreshValidation;
        }

        private void UnregisterViewModelEvents()
        {
            viewModel.GraphChanged -= RefreshGraph;
            viewModel.NodeDataChanged -= RefreshNodeData;
            viewModel.ValidationChanged -= RefreshValidation;
        }
        #endregion

        #region Refresh
        private void RefreshAll()
        {
            RefreshGraph();
            RefreshValidation();
        }

        private void RefreshGraph()
        {
            isRefreshing = true;
            graphField.SetValueWithoutNotify(viewModel.Graph);
            graphNameField.SetValueWithoutNotify(viewModel.Graph?.DisplayName ?? string.Empty);
            graphNameField.SetEnabled(viewModel.Graph != null);
            saveButton.SetEnabled(viewModel.Graph != null);
            autoLayoutButton.SetEnabled(viewModel.Graph != null);
            duplicateButton.SetEnabled(viewModel.Graph != null);
            isRefreshing = false;

            graphView.RequestPopulate();
        }

        private void DuplicateCurrentGraph()
        {
            DialogueGraph_SO duplicate = viewModel.DuplicateCurrentGraph();
            if (duplicate != null)
            {
                Selection.activeObject = duplicate;
                EditorGUIUtility.PingObject(duplicate);
            }
        }

        private void RefreshNodeData(DialogueNode node)
        {
            graphView.RefreshNodeViews();
        }

        private void RefreshValidation()
        {
            validationRows.Clear();
            validationList.Clear();

            foreach (DialogueGraphValidationMessage message in viewModel.ValidationMessages)
            {
                Label row = new Label(message.Message);
                row.AddToClassList("validation-row");
                validationRows.Add(row);
                validationList.Add(row);
            }

            if (validationRows.Count == 0)
            {
                Label row = new Label(viewModel.Graph == null
                    ? "Select a DialogueGraph asset."
                    : "No validation issues.");
                row.AddToClassList("validation-row");
                row.AddToClassList("validation-row-ok");
                validationList.Add(row);
            }
        }
        #endregion
    }
}
