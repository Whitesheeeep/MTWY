using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GameData.Editor
{
    /// <summary>
    /// 对话图编辑器的 UI Toolkit View 层，负责查询 UXML 元素、绑定控件事件并刷新界面状态。
    /// </summary>
    internal sealed class DialogueGraphEditorView : IDisposable
    {
        #region 字段
        private readonly VisualElement root;
        private readonly DialogueGraphEditorViewModel viewModel;
        private readonly List<Label> validationRows = new();

        private DialogueGraphView graphView;
        private ObjectField graphField;
        private TextField graphNameField;
        private Button saveButton;
        private Button autoLayoutButton;
        private VisualElement graphContainer;
        private VisualElement emptyDetails;
        private VisualElement detailsContent;
        private Label selectedTypeLabel;
        private TextField titleField;
        private VisualElement speakerPropertyContainer;
        private VisualElement portraitPropertyContainer;
        private TextField speechTextField;
        private TextField choiceTextField;
        private VisualElement choiceConditionsContainer;
        private VisualElement choiceActionsContainer;
        private Button splitButton;
        private VisualElement speechSection;
        private VisualElement choiceSection;
        private VisualElement readonlySection;
        private Label readonlyInfoLabel;
        private VisualElement validationList;

        private SerializedObject speechSerializedObject;
        private SerializedObject choiceSerializedObject;
        private bool isRefreshing;
        #endregion

        #region 初始化
        /// <summary>
        /// 创建对话图编辑器 View。
        /// </summary>
        /// <param name="root">EditorWindow 的根视觉元素。</param>
        /// <param name="viewModel">对话图编辑器 ViewModel。</param>
        public DialogueGraphEditorView(VisualElement root, DialogueGraphEditorViewModel viewModel)
        {
            this.root = root;
            this.viewModel = viewModel;
        }

        /// <summary>
        /// 查询 UXML 元素、创建 GraphView、注册控件事件并执行首次刷新。
        /// </summary>
        public void Bind()
        {
            QueryElements();
            ConfigureGraphView();
            ConfigureFields();
            RegisterViewModelEvents();
            RefreshAll();
        }
        #endregion

        #region 生命周期
        /// <summary>
        /// 释放 View 订阅的 ViewModel 事件和 SerializedObject 绑定。
        /// </summary>
        public void Dispose()
        {
            UnregisterViewModelEvents();
            ClearSpeechPropertyFields();
            ClearChoiceExtensionFields();
        }
        #endregion

        #region 运行时高亮
        /// <summary>
        /// 设置 GraphView 中的运行时当前节点高亮。
        /// </summary>
        /// <param name="node">当前运行到的对话节点；传入空值时清空高亮。</param>
        public void SetRuntimeCurrentNode(DialogueNode node)
        {
            graphView?.SetRuntimeCurrentNode(node);
        }
        #endregion

        #region UXML 查询与控件配置
        private void QueryElements()
        {
            graphField = root.Q<ObjectField>("GraphField");
            graphNameField = root.Q<TextField>("GraphNameField");
            saveButton = root.Q<Button>("SaveButton");
            autoLayoutButton = root.Q<Button>("AutoLayoutButton");
            graphContainer = root.Q<VisualElement>("GraphContainer");
            emptyDetails = root.Q<VisualElement>("EmptyDetails");
            detailsContent = root.Q<VisualElement>("DetailsContent");
            selectedTypeLabel = root.Q<Label>("SelectedTypeLabel");
            titleField = root.Q<TextField>("TitleField");
            speakerPropertyContainer = root.Q<VisualElement>("SpeakerPropertyContainer");
            portraitPropertyContainer = root.Q<VisualElement>("PortraitPropertyContainer");
            speechTextField = root.Q<TextField>("SpeechTextField");
            choiceTextField = root.Q<TextField>("ChoiceTextField");
            choiceConditionsContainer = root.Q<VisualElement>("ChoiceConditionsContainer");
            choiceActionsContainer = root.Q<VisualElement>("ChoiceActionsContainer");
            splitButton = root.Q<Button>("SplitButton");
            speechSection = root.Q<VisualElement>("SpeechSection");
            choiceSection = root.Q<VisualElement>("ChoiceSection");
            readonlySection = root.Q<VisualElement>("ReadonlySection");
            readonlyInfoLabel = root.Q<Label>("ReadonlyInfoLabel");
            validationList = root.Q<VisualElement>("ValidationList");
        }

        private void ConfigureGraphView()
        {
            graphView = new DialogueGraphView(viewModel);
            graphView.StretchToParentSize();
            graphContainer.Add(graphView);
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

            titleField.RegisterValueChangedCallback(evt =>
            {
                if (!isRefreshing)
                {
                    viewModel.SetNodeTitle(viewModel.SelectedNode, evt.newValue);
                }
            });

            speechTextField.RegisterValueChangedCallback(evt =>
            {
                if (!isRefreshing && viewModel.SelectedNode is DialogueSpeechNode speech)
                {
                    viewModel.SetSpeechText(speech, evt.newValue);
                }
            });

            choiceTextField.RegisterValueChangedCallback(evt =>
            {
                if (!isRefreshing && viewModel.SelectedNode is DialogueChoiceNode choice)
                {
                    viewModel.SetChoiceText(choice, evt.newValue);
                }
            });

            splitButton.clicked += () =>
            {
                if (viewModel.SelectedNode is DialogueSpeechNode speech)
                {
                    viewModel.SplitSpeechNode(speech);
                }
            };

            saveButton.clicked += viewModel.Save;
            autoLayoutButton.clicked += graphView.AutoLayoutGraph;
        }
        #endregion

        #region ViewModel 事件
        private void RegisterViewModelEvents()
        {
            viewModel.GraphChanged += RefreshGraph;
            viewModel.SelectionChanged += RefreshDetails;
            viewModel.NodeDataChanged += RefreshNodeData;
            viewModel.ValidationChanged += RefreshValidation;
        }

        private void UnregisterViewModelEvents()
        {
            viewModel.GraphChanged -= RefreshGraph;
            viewModel.SelectionChanged -= RefreshDetails;
            viewModel.NodeDataChanged -= RefreshNodeData;
            viewModel.ValidationChanged -= RefreshValidation;
        }
        #endregion

        #region 刷新
        private void RefreshAll()
        {
            RefreshGraph();
            RefreshDetails();
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
            isRefreshing = false;

            graphView.RequestPopulate();
        }

        private void RefreshNodeData(DialogueNode node)
        {
            graphView.RefreshNodeViews();

            if (node is DialogueSpeechNode speech && viewModel.SelectedNode == node)
            {
                splitButton.SetEnabled(viewModel.CanSplitSpeech(speech));
            }
        }

        private void RefreshDetails()
        {
            DialogueNode selected = viewModel.SelectedNode;
            bool hasSelection = selected != null;

            ClearSpeechPropertyFields();
            ClearChoiceExtensionFields();
            emptyDetails.EnableInClassList("is-hidden", hasSelection);
            detailsContent.EnableInClassList("is-hidden", !hasSelection);

            if (!hasSelection)
            {
                return;
            }

            isRefreshing = true;
            selectedTypeLabel.text = selected.GetType().Name;
            titleField.SetValueWithoutNotify(selected.EditorTitle ?? string.Empty);

            speechSection.EnableInClassList("is-hidden", selected is not DialogueSpeechNode);
            choiceSection.EnableInClassList("is-hidden", selected is not DialogueChoiceNode);
            readonlySection.EnableInClassList("is-hidden", selected is DialogueSpeechNode or DialogueChoiceNode);

            if (selected is DialogueSpeechNode speech)
            {
                BindSpeechPropertyFields(speech);
                speechTextField.SetValueWithoutNotify(speech.Text ?? string.Empty);
                splitButton.SetEnabled(viewModel.CanSplitSpeech(speech));
            }
            else if (selected is DialogueChoiceNode choice)
            {
                choiceTextField.SetValueWithoutNotify(choice.ChoiceText ?? string.Empty);
                BindChoiceExtensionFields(choice);
            }
            else if (selected is DialogueStartNode)
            {
                readonlyInfoLabel.text = "Start node is the graph entry. Connect it to one Speech node.";
            }
            else if (selected is DialogueEndNode)
            {
                readonlyInfoLabel.text = "End node terminates the dialogue flow.";
            }

            isRefreshing = false;
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

        #region Speech 属性绑定
        private void BindSpeechPropertyFields(DialogueSpeechNode speech)
        {
            if (speech == null || speakerPropertyContainer == null || portraitPropertyContainer == null)
            {
                return;
            }

            speechSerializedObject = new SerializedObject(speech);
            speechSerializedObject.Update();

            AddSpeechPropertyField(speakerPropertyContainer, speech, "speakerId", "Speaker");
            AddSpeechPropertyField(portraitPropertyContainer, speech, "portraitId", "Portrait");
        }

        private void AddSpeechPropertyField(VisualElement container, DialogueSpeechNode speech, string propertyName, string label)
        {
            SerializedProperty property = speechSerializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            PropertyField propertyField = new PropertyField(property, label);
            propertyField.RegisterCallback<SerializedPropertyChangeEvent>(_ =>
            {
                if (isRefreshing)
                {
                    return;
                }

                speechSerializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(speech);
                viewModel.NotifyNodeDataChanged(speech);
            });

            propertyField.Bind(speechSerializedObject);
            container.Add(propertyField);
        }

        private void ClearSpeechPropertyFields()
        {
            speakerPropertyContainer?.Unbind();
            portraitPropertyContainer?.Unbind();
            speakerPropertyContainer?.Clear();
            portraitPropertyContainer?.Clear();
            speechSerializedObject = null;
        }
        #endregion

        #region Choice 扩展绑定
        private void BindChoiceExtensionFields(DialogueChoiceNode choice)
        {
            if (choice == null || choiceConditionsContainer == null || choiceActionsContainer == null)
            {
                return;
            }

            choiceSerializedObject = new SerializedObject(choice);
            choiceSerializedObject.Update();

            AddChoiceExtensionField(choiceConditionsContainer, choice, "conditions", "Conditions");
            AddChoiceExtensionField(choiceActionsContainer, choice, "actions", "Actions");
        }

        private void AddChoiceExtensionField(VisualElement container, DialogueChoiceNode choice, string propertyName, string label)
        {
            SerializedProperty property = choiceSerializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            PropertyField propertyField = new PropertyField(property, label);
            propertyField.AddToClassList("choice-extension-field");
            propertyField.RegisterCallback<SerializedPropertyChangeEvent>(_ =>
            {
                if (isRefreshing)
                {
                    return;
                }

                choiceSerializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(choice);
                viewModel.NotifyNodeDataChanged(choice);
            });

            propertyField.Bind(choiceSerializedObject);
            container.Add(propertyField);
        }

        private void ClearChoiceExtensionFields()
        {
            choiceConditionsContainer?.Unbind();
            choiceActionsContainer?.Unbind();
            choiceConditionsContainer?.Clear();
            choiceActionsContainer?.Clear();
            choiceSerializedObject = null;
        }
        #endregion
    }
}
