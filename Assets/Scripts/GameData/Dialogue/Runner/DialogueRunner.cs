using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 基础对话运行器，只负责根据 DialogueGraph_SO 推进节点数据，不处理 UI 和资源加载。
    /// </summary>
    public sealed class DialogueRunner
    {
        #region 字段
        private readonly List<DialogueChoiceNode> currentChoices = new List<DialogueChoiceNode>();

        private IDialogueServices services;
        private DialogueGraph_SO currentGraph;
        private DialogueNode currentNode;
        private DialogueSpeechNode currentSpeech;
        private DialogueRunnerState state = DialogueRunnerState.Idle;
        #endregion

        #region 初始化
        /// <summary>
        /// 创建一个使用空服务表的对话运行器。
        /// </summary>
        public DialogueRunner() : this(new DialogueServices())
        {
        }

        /// <summary>
        /// 创建一个使用指定服务表的对话运行器。
        /// </summary>
        /// <param name="services">对话运行时服务表。</param>
        public DialogueRunner(IDialogueServices services)
        {
            SetServices(services);
        }
        #endregion

        #region 属性
        /// <summary>
        /// 当前 Runner 状态。
        /// </summary>
        public DialogueRunnerState State => state;

        /// <summary>
        /// 当前正在运行的对话图。
        /// </summary>
        public DialogueGraph_SO CurrentGraph => currentGraph;

        /// <summary>
        /// 当前停留的原始节点。
        /// </summary>
        public DialogueNode CurrentNode => currentNode;

        /// <summary>
        /// 当前对白节点；只有处于 Speech 或 Choice 状态时通常有值。
        /// </summary>
        public DialogueSpeechNode CurrentSpeech => currentSpeech;

        /// <summary>
        /// 当前对白可用的选项列表，顺序与 GraphView 中的视觉坐标一致。
        /// </summary>
        public IReadOnlyList<DialogueChoiceNode> CurrentChoices => currentChoices;

        /// <summary>
        /// 当前 Runner 使用的运行时服务表。
        /// </summary>
        public IDialogueServices Services => services;

        /// <summary>
        /// Runner 是否处于可继续交互的对话过程中。
        /// </summary>
        public bool IsRunning => state is DialogueRunnerState.Speech or DialogueRunnerState.Choice;
        #endregion

        #region 服务
        /// <summary>
        /// 设置当前 Runner 使用的运行时服务表。
        /// </summary>
        /// <param name="services">新的运行时服务表。</param>
        public void SetServices(IDialogueServices services)
        {
            this.services = services ?? new DialogueServices();
        }
        #endregion

        #region 对话控制
        /// <summary>
        /// 开始运行指定对话图，并从 Start 节点指向的第一个 Speech 节点进入。
        /// </summary>
        /// <param name="graph">要运行的对话图资源。</param>
        public void Start(DialogueGraph_SO graph)
        {
            ResetSession(graph);

            if (graph == null)
            {
                EndWithWarning("Start failed because graph is null.");
                return;
            }

            DialogueStartNode startNode = graph.StartNode;
            if (startNode == null)
            {
                EndWithWarning($"Graph '{graph.name}' has no Start node.");
                return;
            }

            if (startNode.NextNode == null)
            {
                EndWithWarning($"Graph '{graph.name}' Start node has no target Speech node.");
                return;
            }

            EnterNode(startNode.NextNode);
        }

        /// <summary>
        /// 当前对白没有选项时，沿 Speech.nextNode 推进到下一个节点。
        /// </summary>
        public void Continue()
        {
            if (state != DialogueRunnerState.Speech)
            {
                Debug.LogWarning($"[DialogueRunner] Continue ignored. Current state is {state}.");
                return;
            }

            if (currentSpeech == null)
            {
                EndWithWarning("Continue failed because current speech is null.");
                return;
            }

            if (currentChoices.Count > 0)
            {
                state = DialogueRunnerState.Choice;
                Debug.LogWarning("[DialogueRunner] Continue ignored because current Speech has choices. Use SelectChoice instead.");
                return;
            }

            if (currentSpeech.NextNode == null)
            {
                EndWithWarning($"Speech '{GetNodeName(currentSpeech)}' has no next node and no choices.");
                return;
            }

            EnterNode(currentSpeech.NextNode);
        }

        /// <summary>
        /// 选择当前选项列表中的一项，执行该选项动作并跳转到目标节点。
        /// </summary>
        /// <param name="index">当前选项列表中的索引。</param>
        public void SelectChoice(int index)
        {
            if (state != DialogueRunnerState.Choice)
            {
                Debug.LogWarning($"[DialogueRunner] SelectChoice ignored. Current state is {state}.");
                return;
            }

            if (index < 0 || index >= currentChoices.Count)
            {
                Debug.LogWarning($"[DialogueRunner] SelectChoice index out of range: {index}. Choice count: {currentChoices.Count}.");
                return;
            }

            DialogueChoiceNode choice = currentChoices[index];
            if (choice == null)
            {
                EndWithWarning($"Choice at index {index} is null.");
                return;
            }

            if (choice.TargetNode == null)
            {
                EndWithWarning($"Choice '{choice.ChoiceText}' has no target node.");
                return;
            }

            ExecuteChoiceActions(choice);
            EnterNode(choice.TargetNode);
        }

        /// <summary>
        /// 停止当前对话会话并清空运行时状态。
        /// </summary>
        public void Stop()
        {
            ResetSession(null);
        }
        #endregion

        #region 查询
        /// <summary>
        /// 获取当前停留的原始节点。
        /// </summary>
        /// <returns>当前节点；未开始或已结束时可能为空。</returns>
        public DialogueNode GetCurrentNode()
        {
            return currentNode;
        }

        /// <summary>
        /// 获取当前对白节点。
        /// </summary>
        /// <returns>当前对白节点；当前不在对白上下文时为空。</returns>
        public DialogueSpeechNode GetCurrentSpeech()
        {
            return currentSpeech;
        }

        /// <summary>
        /// 获取当前可选择的选项列表。
        /// </summary>
        /// <returns>当前对白下可用的选项列表。</returns>
        public IReadOnlyList<DialogueChoiceNode> GetCurrentChoices()
        {
            return currentChoices;
        }

        /// <summary>
        /// 获取当前 Runner 状态。
        /// </summary>
        /// <returns>当前 Runner 状态。</returns>
        public DialogueRunnerState GetState()
        {
            return state;
        }
        #endregion

        #region 节点推进
        private void EnterNode(DialogueNode node)
        {
            currentNode = node;
            currentSpeech = null;
            currentChoices.Clear();

            switch (node)
            {
                case null:
                    EndWithWarning("Tried to enter a null node.");
                    break;
                case DialogueSpeechNode speech:
                    EnterSpeech(speech);
                    break;
                case DialogueEndNode:
                    state = DialogueRunnerState.Ended;
                    break;
                case DialogueChoiceNode choice:
                    EnterChoiceTarget(choice);
                    break;
                case DialogueStartNode start:
                    EnterNode(start.NextNode);
                    break;
                default:
                    EndWithWarning($"Unsupported dialogue node type: {node.GetType().Name}.");
                    break;
            }
        }

        private void EnterSpeech(DialogueSpeechNode speech)
        {
            currentSpeech = speech;
            BuildChoicesForSpeech(speech);
            state = currentChoices.Count > 0 ? DialogueRunnerState.Choice : DialogueRunnerState.Speech;

            if (currentChoices.Count > 0 && speech.NextNode != null)
            {
                Debug.LogWarning($"[DialogueRunner] Speech '{GetNodeName(speech)}' has both choices and nextNode. Runner will wait for choice.");
            }
        }

        private void EnterChoiceTarget(DialogueChoiceNode choice)
        {
            if (choice.TargetNode == null)
            {
                EndWithWarning($"Choice '{choice.ChoiceText}' has no target node.");
                return;
            }

            EnterNode(choice.TargetNode);
        }
        #endregion

        #region 选项处理
        private void BuildChoicesForSpeech(DialogueSpeechNode speech)
        {
            if (speech == null)
            {
                return;
            }

            IEnumerable<DialogueChoiceNode> choices = speech.Choices
                .Where(choice => choice != null)
                .OrderBy(choice => choice.Position.y)
                .ThenBy(choice => choice.Position.x);

            currentChoices.AddRange(choices);
        }

        private void ExecuteChoiceActions(DialogueChoiceNode choice)
        {
            foreach (DialogueAction action in choice.Actions)
            {
                if (action == null) continue;

                try
                {
                    action.Execute(services);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[DialogueRunner] Action '{action.name}' failed on choice '{choice.ChoiceText}'.\n{exception}");
                }
            }
        }
        #endregion

        #region 状态维护
        private void ResetSession(DialogueGraph_SO graph)
        {
            currentGraph = graph;
            currentNode = null;
            currentSpeech = null;
            currentChoices.Clear();
            state = DialogueRunnerState.Idle;
        }

        private void EndWithWarning(string message)
        {
            Debug.LogWarning($"[DialogueRunner] {message}");
            currentNode = null;
            currentSpeech = null;
            currentChoices.Clear();
            state = DialogueRunnerState.Ended;
        }
        #endregion

        #region 工具方法
        /// <summary>
        /// 获取用于日志输出的节点名称。
        /// </summary>
        /// <param name="node">要格式化的对话节点。</param>
        /// <returns>包含节点类型和编辑器标题的显示名称。</returns>
        public static string GetNodeName(DialogueNode node)
        {
            if (node == null)
            {
                return "null";
            }

            string title = string.IsNullOrWhiteSpace(node.EditorTitle) ? node.name : node.EditorTitle;
            return $"{node.GetType().Name}({title})";
        }
        #endregion
    }
}
