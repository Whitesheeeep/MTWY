using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GameData.Editor
{
    /// <summary>
    /// 对话图右侧节点详情 View，负责显示并编辑当前选中的节点数据。
    /// </summary>
    internal sealed class DialogueGraphDetailsView : IDisposable
    {
        #region Fields
        private readonly VisualElement root;
        private readonly DialogueGraphEditorViewModel viewModel;

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

        private SerializedObject speechSerializedObject;
        private SerializedObject choiceSerializedObject;
        private bool isRefreshing;
        #endregion

        #region Initialize
        /// <summary>
        /// 创建节点详情 View。
        /// </summary>
        public DialogueGraphDetailsView(VisualElement root, DialogueGraphEditorViewModel viewModel)
        {
            this.root = root;
            this.viewModel = viewModel;
        }

        /// <summary>
        /// 查询节点详情 UXML 元素并注册 UI / ViewModel 事件。
        /// </summary>
        public void Bind()
        {
            QueryElements();
            ConfigureFields();
            RegisterViewModelEvents();
            RefreshDetails();
        }
        #endregion

        #region Lifecycle
        /// <summary>
        /// 释放事件订阅和 SerializedObject 绑定。
        /// </summary>
        public void Dispose()
        {
            UnregisterViewModelEvents();
            ClearSpeechPropertyFields();
            ClearChoiceExtensionFields();
        }
        #endregion

        #region UXML
        private void QueryElements()
        {
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
        }

        private void ConfigureFields()
        {
            titleField.RegisterValueChangedCallback(evt =>
            {
                if (!isRefreshing)
                {
                    viewModel.SetNodeTitle(viewModel.SelectedNode, evt.newValue);
                }
            });

            speechTextField.multiline = true;
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
        }
        #endregion

        #region ViewModel Events
        private void RegisterViewModelEvents()
        {
            viewModel.SelectionChanged += RefreshDetails;
            viewModel.NodeDataChanged += RefreshNodeData;
        }

        private void UnregisterViewModelEvents()
        {
            viewModel.SelectionChanged -= RefreshDetails;
            viewModel.NodeDataChanged -= RefreshNodeData;
        }
        #endregion

        #region Refresh
        private void RefreshNodeData(DialogueNode node)
        {
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
        #endregion

        #region Speech Properties
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

        #region Choice Extensions
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
