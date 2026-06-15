using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameData.Editor
{
    /// <summary>
    /// GraphView 中单个对话节点的视觉表现，负责端口、标题、预览信息和节点类型 USS class。
    /// </summary>
    internal sealed class DialogueNodeView : Node
    {
        #region 字段
        private readonly Label primaryLabel;
        private readonly Label textPreviewLabel;
        private readonly Label metaLabel;
        #endregion

        #region 初始化
        /// <summary>
        /// 创建对话节点视图。
        /// </summary>
        /// <param name="dialogueNode">该视图绑定的对话节点数据。</param>
        public DialogueNodeView(DialogueNode dialogueNode)
        {
            DialogueNode = dialogueNode;
            viewDataKey = dialogueNode.Guid;
            title = GetTitle(dialogueNode);

            AddToClassList("dialogue-node");
            AddToClassList(GetClassName(dialogueNode));
            mainContainer.AddToClassList("dialogue-node-main-container");
            topContainer.AddToClassList("dialogue-node-port-container");
            inputContainer.AddToClassList("dialogue-node-input-container");
            outputContainer.AddToClassList("dialogue-node-output-container");
            titleContainer.AddToClassList("dialogue-node-title-container");
            titleContainer.AddToClassList(GetTitleClassName(dialogueNode));
            extensionContainer.AddToClassList("dialogue-node-extension-container");

            CreatePorts(dialogueNode);

            VisualElement selectionIndicator = new VisualElement();
            selectionIndicator.AddToClassList("dialogue-node-selection-indicator");
            extensionContainer.Add(selectionIndicator);

            VisualElement previewContainer = new VisualElement();
            previewContainer.AddToClassList("dialogue-node-preview");
            extensionContainer.Add(previewContainer);

            primaryLabel = new Label();
            primaryLabel.AddToClassList("dialogue-node-primary");
            previewContainer.Add(primaryLabel);

            textPreviewLabel = new Label();
            textPreviewLabel.AddToClassList("dialogue-node-text-preview");
            previewContainer.Add(textPreviewLabel);

            metaLabel = new Label();
            metaLabel.AddToClassList("dialogue-node-meta");
            previewContainer.Add(metaLabel);

            SetPosition(new Rect(dialogueNode.Position, GetDefaultSize(dialogueNode)));
            RefreshFromData();
            RefreshExpandedState();
            RefreshPorts();
        }
        #endregion

        #region 属性
        /// <summary>
        /// 当前节点视图绑定的对话节点数据。
        /// </summary>
        public DialogueNode DialogueNode { get; }

        /// <summary>
        /// 节点输入端口；Start 节点没有输入端口。
        /// </summary>
        public Port InputPort { get; private set; }

        /// <summary>
        /// 节点输出端口；End 节点没有输出端口。
        /// </summary>
        public Port OutputPort { get; private set; }
        #endregion

        #region 刷新
        /// <summary>
        /// 根据当前绑定的数据刷新标题和节点卡片预览。
        /// </summary>
        public void RefreshFromData()
        {
            title = GetTitle(DialogueNode);

            switch (DialogueNode)
            {
                case DialogueStartNode start:
                    SetPreview("Entry", "Start node", start.NextNode == null ? "No target" : $"Next: {GetTitle(start.NextNode)}");
                    break;
                case DialogueSpeechNode speech:
                    SetSpeechPreview(speech);
                    break;
                case DialogueChoiceNode choice:
                    SetChoicePreview(choice);
                    break;
                case DialogueEndNode:
                    SetPreview("End", "Dialogue ends here.", string.Empty);
                    break;
                default:
                    SetPreview("Unknown", string.Empty, string.Empty);
                    break;
            }
        }
        #endregion

        #region 端口
        private void CreatePorts(DialogueNode node)
        {
            if (node is not DialogueStartNode)
            {
                InputPort = InstantiatePort(
                    Orientation.Horizontal,
                    Direction.Input,
                    Port.Capacity.Multi,
                    typeof(DialogueNode));
                InputPort.portName = "";
                inputContainer.Add(InputPort);
            }

            if (node is not DialogueEndNode)
            {
                Port.Capacity capacity = node is DialogueSpeechNode ? Port.Capacity.Multi : Port.Capacity.Single;
                OutputPort = InstantiatePort(
                    Orientation.Horizontal,
                    Direction.Output,
                    capacity,
                    typeof(DialogueNode));
                OutputPort.portName = "";
                outputContainer.Add(OutputPort);
            }
        }
        #endregion

        #region 预览
        private void SetSpeechPreview(DialogueSpeechNode speech)
        {
            string speaker = DialogueSpeakerDataListLocator.GetSpeakerDisplayName(speech.SpeakerId);
            string text = string.IsNullOrWhiteSpace(speech.Text) ? "Empty speech" : speech.Text.Trim();
            int choiceCount = CountChoices(speech);
            string portraitText = string.IsNullOrWhiteSpace(speech.PortraitId) ? "Portrait: Default" : $"Portrait: {speech.PortraitId}";
            string nextText = speech.NextNode == null ? "No linear next" : $"Next: {GetTitle(speech.NextNode)}";
            string choiceText = choiceCount == 0 ? "Choices: 0" : $"Choices: {choiceCount}";

            SetPreview(speaker, Shorten(text, 120), $"{portraitText}    {nextText}    {choiceText}");
        }

        private void SetChoicePreview(DialogueChoiceNode choice)
        {
            string targetText = choice.TargetNode == null ? "No target" : $"Target: {GetTitle(choice.TargetNode)}";
            string extensionText = GetChoiceExtensionSummary(choice);
            SetPreview("Choice", GetChoiceText(choice), $"{targetText}    {extensionText}");
        }

        private void SetPreview(string primary, string preview, string meta)
        {
            primaryLabel.text = primary;
            textPreviewLabel.text = preview;
            metaLabel.text = meta;
            metaLabel.EnableInClassList("is-hidden", string.IsNullOrWhiteSpace(meta));
        }

        private static string GetChoiceText(DialogueChoiceNode choice)
        {
            return string.IsNullOrWhiteSpace(choice.ChoiceText) ? "Empty choice" : choice.ChoiceText.Trim();
        }

        private static int CountChoices(DialogueSpeechNode speech)
        {
            int count = 0;
            foreach (DialogueChoiceNode choice in speech.Choices)
            {
                if (choice != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountConditions(DialogueChoiceNode choice)
        {
            int count = 0;
            foreach (DialogueCondition condition in choice.Conditions)
            {
                if (condition != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountActions(DialogueChoiceNode choice)
        {
            int count = 0;
            foreach (DialogueAction action in choice.Actions)
            {
                if (action != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static string GetChoiceExtensionSummary(DialogueChoiceNode choice)
        {
            int conditionCount = CountConditions(choice);
            int actionCount = CountActions(choice);
            string countText = $"Conditions: {conditionCount}    Actions: {actionCount}";

            string conditionNames = GetObjectNames(choice.Conditions, "Cond");
            string actionNames = GetObjectNames(choice.Actions, "Act");

            if (string.IsNullOrEmpty(conditionNames) && string.IsNullOrEmpty(actionNames))
            {
                return countText;
            }

            if (string.IsNullOrEmpty(conditionNames))
            {
                return $"{countText}\n{actionNames}";
            }

            if (string.IsNullOrEmpty(actionNames))
            {
                return $"{countText}\n{conditionNames}";
            }

            return $"{countText}    {conditionNames}    {actionNames}";
        }

        private static string GetObjectNames<T>(IEnumerable<T> objects, string prefix) where T : Object
        {
            string[] names = objects
                .Where(item => item != null)
                .Take(2)
                .Select(item => item.name)
                .ToArray();

            if (names.Length == 0)
            {
                return string.Empty;
            }

            return $"{prefix}:\n{string.Join(",\n", names)}";
        }

        private static string Shorten(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return $"{value[..maxLength].TrimEnd()}...";
        }
        #endregion

        #region 工具方法
        private static string GetTitle(DialogueNode node)
        {
            if (node == null)
            {
                return "Missing Node";
            }

            if (!string.IsNullOrWhiteSpace(node.EditorTitle))
            {
                return node.EditorTitle;
            }

            return node switch
            {
                DialogueStartNode => "Start",
                DialogueSpeechNode => "Speech",
                DialogueChoiceNode => "Choice",
                DialogueEndNode => "End",
                _ => node.name
            };
        }

        private static string GetClassName(DialogueNode node)
        {
            return node switch
            {
                DialogueStartNode => "dialogue-start-node",
                DialogueSpeechNode => "dialogue-speech-node",
                DialogueChoiceNode => "dialogue-choice-node",
                DialogueEndNode => "dialogue-end-node",
                _ => "dialogue-unknown-node"
            };
        }

        private static string GetTitleClassName(DialogueNode node)
        {
            return node switch
            {
                DialogueStartNode => "dialogue-start-node-title",
                DialogueSpeechNode => "dialogue-speech-node-title",
                DialogueChoiceNode => "dialogue-choice-node-title",
                DialogueEndNode => "dialogue-end-node-title",
                _ => "dialogue-unknown-node-title"
            };
        }

        private static Vector2 GetDefaultSize(DialogueNode node)
        {
            return node switch
            {
                DialogueSpeechNode => new Vector2(270f, 160f),
                DialogueChoiceNode => new Vector2(260f, 140f),
                _ => new Vector2(200f, 100f)
            };
        }
        #endregion
    }
}
