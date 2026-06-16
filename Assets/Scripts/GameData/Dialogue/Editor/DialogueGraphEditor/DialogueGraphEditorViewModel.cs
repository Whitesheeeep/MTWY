using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameData.Editor
{
    /// <summary>
    /// 对话图编辑器 ViewModel，作为 View 层统一门面，负责编排图命令、连接、校验和布局服务。
    /// </summary>
    internal sealed class DialogueGraphEditorViewModel
    {
        #region 字段
        private readonly DialogueGraphEditorGraphCommands graphCommands = new();
        private readonly DialogueGraphConnectionService connectionService = new();
        private readonly DialogueGraphValidationService validationService = new();
        private readonly DialogueGraphAutoLayoutService autoLayoutService = new();
        private readonly List<DialogueGraphValidationMessage> validationMessages = new();
        #endregion

        #region 事件
        /// <summary>
        /// 图结构发生变化，GraphView 需要重建节点和连线。
        /// </summary>
        public event Action GraphChanged;

        /// <summary>
        /// 当前选中节点发生变化，Details 面板需要刷新。
        /// </summary>
        public event Action SelectionChanged;

        /// <summary>
        /// 节点内容数据发生变化，只需要刷新节点预览和校验。
        /// </summary>
        public event Action<DialogueNode> NodeDataChanged;

        /// <summary>
        /// 校验结果发生变化，Validation 面板需要刷新。
        /// </summary>
        public event Action ValidationChanged;
        #endregion

        #region 属性
        /// <summary>
        /// 当前正在编辑的对话图资源。
        /// </summary>
        public DialogueGraph_SO Graph { get; private set; }

        /// <summary>
        /// 当前 GraphView 中选中的节点。
        /// </summary>
        public DialogueNode SelectedNode { get; private set; }

        /// <summary>
        /// 当前对话图校验提示列表。
        /// </summary>
        public IReadOnlyList<DialogueGraphValidationMessage> ValidationMessages => validationMessages;
        #endregion

        #region 图资源与选择
        /// <summary>
        /// 设置当前编辑的对话图资源。
        /// </summary>
        public void SetGraph(DialogueGraph_SO graph)
        {
            Graph = graph;
            SelectedNode = null;
            graphCommands.InitializeGraph(Graph);

            RefreshValidation();
            GraphChanged?.Invoke();
            SelectionChanged?.Invoke();
        }

        /// <summary>
        /// 设置当前选中节点。
        /// </summary>
        public void SelectNode(DialogueNode node)
        {
            if (SelectedNode == node)
            {
                return;
            }

            SelectedNode = node;
            SelectionChanged?.Invoke();
        }

        /// <summary>
        /// 复制当前对话图资源，并切换到复制出的新资源。
        /// </summary>
        public DialogueGraph_SO DuplicateCurrentGraph()
        {
            DialogueGraph_SO duplicate = graphCommands.DuplicateGraph(Graph);
            if (duplicate == null)
            {
                return null;
            }

            SetGraph(duplicate);
            return duplicate;
        }
        #endregion

        #region 节点创建与删除
        /// <summary>
        /// 创建指定类型的节点。
        /// </summary>
        public DialogueNode CreateNode(Type nodeType, Vector2 position)
        {
            DialogueNode node = graphCommands.CreateNode(Graph, nodeType, position);
            if (node == null)
            {
                return null;
            }

            SelectNode(node);
            RefreshAll();
            return node;
        }

        /// <summary>
        /// 删除节点并按需要刷新整张图。
        /// </summary>
        public void DeleteNode(DialogueNode node, bool notifyGraphChanged = true)
        {
            if (!graphCommands.DeleteNode(Graph, node, connectionService))
            {
                return;
            }

            if (SelectedNode == node)
            {
                SelectedNode = null;
                SelectionChanged?.Invoke();
            }

            if (notifyGraphChanged)
            {
                RefreshAll();
            }
            else
            {
                RefreshViewStateOnly();
            }
        }
        #endregion

        #region 连接
        /// <summary>
        /// 判断两个节点是否允许建立连接。
        /// </summary>
        public bool CanConnect(DialogueNode outputNode, DialogueNode inputNode)
        {
            return connectionService.CanConnect(outputNode, inputNode);
        }

        /// <summary>
        /// 建立两个节点之间的连接。
        /// </summary>
        public void Connect(DialogueNode outputNode, DialogueNode inputNode)
        {
            if (connectionService.Connect(Graph, outputNode, inputNode))
            {
                RefreshViewStateOnly();
            }
        }

        /// <summary>
        /// 断开两个节点之间的连接。
        /// </summary>
        public void Disconnect(DialogueNode outputNode, DialogueNode inputNode)
        {
            if (connectionService.Disconnect(Graph, outputNode, inputNode))
            {
                RefreshViewStateOnly();
            }
        }

        /// <summary>
        /// 获取某个节点当前指向的所有目标节点。
        /// </summary>
        public IEnumerable<DialogueNode> GetTargetsFrom(DialogueNode node)
        {
            return connectionService.GetTargetsFrom(node);
        }

        /// <summary>
        /// 获取 Speech 持有的 Choice，并按节点视觉位置排序。
        /// </summary>
        public IEnumerable<DialogueChoiceNode> GetChoicesFrom(DialogueSpeechNode speech)
        {
            return connectionService.GetChoicesFrom(speech);
        }
        #endregion

        #region 节点编辑
        /// <summary>
        /// 记录 GraphView 拖拽产生的位置变化。
        /// </summary>
        public void MoveNode(DialogueNode node, Vector2 position)
        {
            graphCommands.MoveNode(node, position);
        }

        /// <summary>
        /// 由 GraphView 根据节点视图真实矩形计算完位置后写回数据。
        /// </summary>
        public void SetNodePositionFromView(DialogueNode node, Vector2 position, string undoName)
        {
            graphCommands.SetNodePositionFromView(Graph, node, position, undoName);
        }

        /// <summary>
        /// 设置图资源显示名称。
        /// </summary>
        public void SetGraphDisplayName(string value)
        {
            if (graphCommands.SetGraphDisplayName(Graph, value))
            {
                RefreshValidation();
            }
        }

        /// <summary>
        /// 设置节点编辑器标题。
        /// </summary>
        public void SetNodeTitle(DialogueNode node, string value)
        {
            if (graphCommands.SetNodeTitle(node, value))
            {
                RefreshNodeDataOnly(node);
            }
        }

        /// <summary>
        /// 设置 Speech 节点 SpeakerId。
        /// </summary>
        public void SetSpeakerId(DialogueSpeechNode node, string value)
        {
            if (graphCommands.SetSpeakerId(node, value))
            {
                RefreshSelectedNodeDataAndDetails(node);
            }
        }

        /// <summary>
        /// 设置 Speech 节点头像 Id。
        /// </summary>
        public void SetPortraitId(DialogueSpeechNode node, string value)
        {
            if (graphCommands.SetPortraitId(node, value))
            {
                RefreshSelectedNodeDataAndDetails(node);
            }
        }

        /// <summary>
        /// 设置 Speech 节点文本。
        /// </summary>
        public void SetSpeechText(DialogueSpeechNode node, string value)
        {
            if (graphCommands.SetSpeechText(node, value))
            {
                RefreshNodeDataOnly(node);
            }
        }

        /// <summary>
        /// 设置 Choice 节点文本。
        /// </summary>
        public void SetChoiceText(DialogueChoiceNode node, string value)
        {
            if (graphCommands.SetChoiceText(node, value))
            {
                RefreshNodeDataOnly(node);
            }
        }
        #endregion

        #region 长文本拆分
        /// <summary>
        /// 判断指定 Speech 节点是否可以拆分。
        /// </summary>
        public bool CanSplitSpeech(DialogueSpeechNode node)
        {
            return graphCommands.CanSplitSpeech(Graph, node);
        }

        /// <summary>
        /// 将 Speech 节点拆成两个连续 Speech 节点。
        /// </summary>
        public void SplitSpeechNode(DialogueSpeechNode node)
        {
            DialogueSpeechNode newSpeech = graphCommands.SplitSpeechNode(Graph, node, connectionService);
            if (newSpeech == null)
            {
                return;
            }

            SelectNode(newSpeech);
            RefreshAll();
        }
        #endregion

        #region 外部通知与保存
        /// <summary>
        /// 处理 Unity Undo/Redo 后的数据清理和界面刷新。
        /// </summary>
        public void HandleUndoRedo()
        {
            Graph?.RemoveNullNodes();
            RefreshAll();
        }

        /// <summary>
        /// 通知指定节点数据已由外部绑定控件修改。
        /// </summary>
        public void NotifyNodeDataChanged(DialogueNode node)
        {
            graphCommands.MarkNodeDataChanged(Graph, node);
            RefreshNodeDataOnly(node);
        }

        /// <summary>
        /// 保存当前对话图资源和节点 sub-asset。
        /// </summary>
        public void Save()
        {
            graphCommands.Save(Graph);
        }
        #endregion

        #region 自动排版
        /// <summary>
        /// 根据图连接关系计算自动排版层级。
        /// </summary>
        public Dictionary<DialogueNode, int> CalculateAutoLayoutLayers()
        {
            Dictionary<DialogueNode, int> layers = autoLayoutService.CalculateLayers(Graph, connectionService, out bool stoppedEarly);
            if (stoppedEarly)
            {
                validationMessages.Add(new DialogueGraphValidationMessage("Auto layout stopped early. The graph may contain a cycle."));
                ValidationChanged?.Invoke();
            }

            return layers;
        }
        #endregion

        #region 刷新通知
        private void RefreshAll()
        {
            RefreshValidation();
            GraphChanged?.Invoke();
            SelectionChanged?.Invoke();
        }

        private void RefreshViewStateOnly()
        {
            RefreshValidation();
            SelectionChanged?.Invoke();
        }

        private void RefreshNodeDataOnly(DialogueNode node)
        {
            graphCommands.MarkNodeDataChanged(Graph, node);
            RefreshValidation();
            NodeDataChanged?.Invoke(node);
        }

        private void RefreshSelectedNodeDataAndDetails(DialogueNode node)
        {
            RefreshNodeDataOnly(node);
            SelectionChanged?.Invoke();
        }

        private void RefreshValidation()
        {
            validationMessages.Clear();
            validationMessages.AddRange(validationService.Validate(Graph, connectionService));
            ValidationChanged?.Invoke();
        }
        #endregion
    }
}
