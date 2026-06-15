using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameData.Editor
{
    /// <summary>
    /// 对话图编辑器 ViewModel，负责对话图数据修改、Undo、校验和界面刷新通知。
    /// </summary>
    internal sealed class DialogueGraphEditorViewModel
    {
        #region 字段
        private readonly List<DialogueGraphValidationMessage> validationMessages = new();
        #endregion

        #region 事件
        /// <summary>
        /// 图结构发生变化，需要 GraphView 重建节点和连线。
        /// </summary>
        public event Action GraphChanged;

        /// <summary>
        /// 当前选中节点发生变化，需要刷新详情面板。
        /// </summary>
        public event Action SelectionChanged;

        /// <summary>
        /// 节点内容数据发生变化，只需要刷新节点卡片预览和校验，不需要重建整张图。
        /// </summary>
        public event Action<DialogueNode> NodeDataChanged;

        /// <summary>
        /// 校验结果发生变化，需要刷新校验面板。
        /// </summary>
        public event Action ValidationChanged;
        #endregion

        #region 属性
        /// <summary>
        /// 当前正在编辑的对话图资源。
        /// </summary>
        public DialogueGraph_SO Graph { get; private set; }

        /// <summary>
        /// 当前在 GraphView 中选中的节点。
        /// </summary>
        public DialogueNode SelectedNode { get; private set; }

        /// <summary>
        /// 当前对话图校验提示列表。
        /// </summary>
        public IReadOnlyList<DialogueGraphValidationMessage> ValidationMessages => validationMessages;
        #endregion

        #region 图资源与选择
        /// <summary>
        /// 设置当前编辑的对话图资源，并确保基础图数据可编辑。
        /// </summary>
        /// <param name="graph">要编辑的对话图资源。</param>
        public void SetGraph(DialogueGraph_SO graph)
        {
            Graph = graph;
            SelectedNode = null;

            if (Graph != null)
            {
                Undo.RecordObject(Graph, "Open Dialogue Graph");
                EnsureGraphIdentity();
                Graph.EnsureStartNode();
                Graph.RemoveNullNodes();
                EditorUtility.SetDirty(Graph);
            }

            RefreshValidation();
            GraphChanged?.Invoke();
            SelectionChanged?.Invoke();
        }

        /// <summary>
        /// 设置当前选中节点。
        /// </summary>
        /// <param name="node">被选中的对话节点。</param>
        public void SelectNode(DialogueNode node)
        {
            if (SelectedNode == node)
            {
                return;
            }

            SelectedNode = node;
            SelectionChanged?.Invoke();
        }
        #endregion

        #region 节点创建与删除
        /// <summary>
        /// 在当前图中创建指定类型的节点。
        /// </summary>
        /// <param name="nodeType">要创建的节点类型。</param>
        /// <param name="position">节点在 GraphView content space 中的位置。</param>
        /// <returns>创建出的节点；当前没有图资源时返回空。</returns>
        public DialogueNode CreateNode(Type nodeType, Vector2 position)
        {
            if (Graph == null)
            {
                return null;
            }

            DialogueNode node = Graph.CreateNode(nodeType, position);
            ApplyDefaultNodeValues(node);
            MarkNodeDirty(node);
            MarkGraphDirty();
            SelectNode(node);
            RefreshAll();
            AssetDatabase.SaveAssets();
            return node;
        }

        /// <summary>
        /// 删除节点并清理所有指向该节点的引用。
        /// </summary>
        /// <param name="node">要删除的节点。</param>
        /// <param name="notifyGraphChanged">是否立即通知 GraphView 重建。</param>
        public void DeleteNode(DialogueNode node, bool notifyGraphChanged = true)
        {
            if (Graph == null || node == null || node == Graph.StartNode)
            {
                return;
            }

            RecordReferencesTo(node, "Delete Dialogue Node");
            Graph.DeleteNode(node);

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

            AssetDatabase.SaveAssets();
        }
        #endregion

        #region 连接
        /// <summary>
        /// 判断两个节点是否允许建立从 output 到 input 的连接。
        /// </summary>
        /// <param name="outputNode">输出端节点。</param>
        /// <param name="inputNode">输入端节点。</param>
        /// <returns>允许连接时返回 true。</returns>
        public bool CanConnect(DialogueNode outputNode, DialogueNode inputNode)
        {
            if (outputNode == null || inputNode == null || outputNode == inputNode)
            {
                return false;
            }

            if (outputNode is DialogueStartNode)
            {
                return inputNode is DialogueSpeechNode;
            }

            if (outputNode is DialogueSpeechNode)
            {
                return inputNode is DialogueChoiceNode or DialogueSpeechNode or DialogueEndNode;
            }

            if (outputNode is DialogueChoiceNode)
            {
                return inputNode is DialogueSpeechNode or DialogueEndNode;
            }

            return false;
        }

        /// <summary>
        /// 建立两个节点之间的连接，并写入对应节点引用。
        /// </summary>
        /// <param name="outputNode">输出端节点。</param>
        /// <param name="inputNode">输入端节点。</param>
        public void Connect(DialogueNode outputNode, DialogueNode inputNode)
        {
            if (Graph == null || !CanConnect(outputNode, inputNode))
            {
                return;
            }

            switch (outputNode)
            {
                case DialogueStartNode start when inputNode is DialogueSpeechNode speech:
                    Undo.RecordObject(start, "Connect Dialogue Edge");
                    start.NextNode = speech;
                    MarkNodeDirty(start);
                    break;
                case DialogueSpeechNode source when inputNode is DialogueChoiceNode choice:
                    MoveChoiceToSpeech(source, choice, "Connect Dialogue Edge");
                    break;
                case DialogueSpeechNode speech:
                    Undo.RecordObject(speech, "Connect Dialogue Edge");
                    speech.NextNode = inputNode;
                    MarkNodeDirty(speech);
                    break;
                case DialogueChoiceNode choice:
                    Undo.RecordObject(choice, "Connect Dialogue Edge");
                    choice.TargetNode = inputNode;
                    MarkNodeDirty(choice);
                    break;
            }

            RefreshViewStateOnly();
        }

        /// <summary>
        /// 断开两个节点之间的连接，并清空对应节点引用。
        /// </summary>
        /// <param name="outputNode">输出端节点。</param>
        /// <param name="inputNode">输入端节点。</param>
        public void Disconnect(DialogueNode outputNode, DialogueNode inputNode)
        {
            if (Graph == null || outputNode == null || inputNode == null)
            {
                return;
            }

            switch (outputNode)
            {
                case DialogueStartNode start when start.NextNode == inputNode:
                    Undo.RecordObject(start, "Disconnect Dialogue Edge");
                    start.NextNode = null;
                    MarkNodeDirty(start);
                    break;
                case DialogueSpeechNode source when inputNode is DialogueChoiceNode choice && OwnsChoice(source, choice):
                    Undo.RecordObject(source, "Disconnect Dialogue Edge");
                    source.RemoveChoice(choice);
                    MarkNodeDirty(source);
                    break;
                case DialogueSpeechNode speech when speech.NextNode == inputNode:
                    Undo.RecordObject(speech, "Disconnect Dialogue Edge");
                    speech.NextNode = null;
                    MarkNodeDirty(speech);
                    break;
                case DialogueChoiceNode choice when choice.TargetNode == inputNode:
                    Undo.RecordObject(choice, "Disconnect Dialogue Edge");
                    choice.TargetNode = null;
                    MarkNodeDirty(choice);
                    break;
            }

            RefreshViewStateOnly();
        }

        /// <summary>
        /// 获取某个节点当前指向的所有目标节点，用于 GraphView 恢复连线和自动排版。
        /// </summary>
        /// <param name="node">源节点。</param>
        /// <returns>源节点指向的目标节点集合。</returns>
        public IEnumerable<DialogueNode> GetTargetsFrom(DialogueNode node)
        {
            if (node is DialogueStartNode start && start.NextNode != null)
            {
                yield return start.NextNode;
            }
            else if (node is DialogueSpeechNode speech)
            {
                if (speech.NextNode != null)
                {
                    yield return speech.NextNode;
                }

                foreach (DialogueChoiceNode choice in GetChoicesFrom(speech))
                {
                    yield return choice;
                }
            }
            else if (node is DialogueChoiceNode choice && choice.TargetNode != null)
            {
                yield return choice.TargetNode;
            }
        }

        /// <summary>
        /// 获取 Speech 持有的 Choice，并按节点视觉位置排序。
        /// </summary>
        /// <param name="speech">源 Speech 节点。</param>
        /// <returns>按视觉位置排序后的 Choice 节点集合。</returns>
        public IEnumerable<DialogueChoiceNode> GetChoicesFrom(DialogueSpeechNode speech)
        {
            if (Graph == null || speech == null)
            {
                yield break;
            }

            foreach (DialogueChoiceNode choice in speech.Choices
                         .Where(choice => choice != null)
                         .OrderBy(choice => choice.Position.y)
                         .ThenBy(choice => choice.Position.x))
            {
                yield return choice;
            }
        }
        #endregion

        #region 节点编辑
        /// <summary>
        /// 记录 GraphView 拖拽产生的节点位置变化。
        /// </summary>
        /// <param name="node">被移动的节点。</param>
        /// <param name="position">节点新位置。</param>
        public void MoveNode(DialogueNode node, Vector2 position)
        {
            if (node == null)
            {
                return;
            }

            Undo.RecordObject(node, "Move Dialogue Node");
            node.Position = position;
            MarkNodeDirty(node);
        }

        /// <summary>
        /// 由 GraphView 根据节点视图真实矩形计算完位置后，统一写回 ScriptableObject 数据。
        /// </summary>
        /// <param name="node">需要写回位置的对话节点。</param>
        /// <param name="position">GraphView content space 中的节点左上角坐标。</param>
        /// <param name="undoName">Undo 操作名称。</param>
        public void SetNodePositionFromView(DialogueNode node, Vector2 position, string undoName)
        {
            if (node == null)
            {
                return;
            }

            Undo.RecordObject(node, undoName);
            node.Position = position;
            MarkNodeDirty(node);
            MarkGraphDirty();
        }

        /// <summary>
        /// 设置图资源显示名称。
        /// </summary>
        /// <param name="value">新的显示名称。</param>
        public void SetGraphDisplayName(string value)
        {
            if (Graph == null)
            {
                return;
            }

            Undo.RecordObject(Graph, "Edit Dialogue Graph Name");
            Graph.DisplayName = value ?? string.Empty;
            MarkGraphDirty();
            RefreshValidation();
        }

        /// <summary>
        /// 设置节点编辑器标题。
        /// </summary>
        /// <param name="node">要修改的节点。</param>
        /// <param name="value">新的标题。</param>
        public void SetNodeTitle(DialogueNode node, string value)
        {
            if (node == null)
            {
                return;
            }

            Undo.RecordObject(node, "Edit Dialogue Node Title");
            node.EditorTitle = value ?? string.Empty;
            MarkNodeDirty(node);
            RefreshNodeDataOnly(node);
        }

        /// <summary>
        /// 设置 Speech 节点的 SpeakerId。
        /// </summary>
        /// <param name="node">要修改的 Speech 节点。</param>
        /// <param name="value">新的 SpeakerId。</param>
        public void SetSpeakerId(DialogueSpeechNode node, string value)
        {
            if (node == null)
            {
                return;
            }

            Undo.RecordObject(node, "Edit Dialogue Speaker");
            node.SpeakerId = value ?? string.Empty;
            node.PortraitId = string.Empty;
            MarkNodeDirty(node);
            RefreshSelectedNodeDataAndDetails(node);
        }

        /// <summary>
        /// 设置 Speech 节点的头像 Id。
        /// </summary>
        /// <param name="node">要修改的 Speech 节点。</param>
        /// <param name="value">新的头像 Id。</param>
        public void SetPortraitId(DialogueSpeechNode node, string value)
        {
            if (node == null)
            {
                return;
            }

            Undo.RecordObject(node, "Edit Dialogue Portrait");
            node.PortraitId = value ?? string.Empty;
            MarkNodeDirty(node);
            RefreshSelectedNodeDataAndDetails(node);
        }

        /// <summary>
        /// 设置 Speech 节点文本。
        /// </summary>
        /// <param name="node">要修改的 Speech 节点。</param>
        /// <param name="value">新的文本。</param>
        public void SetSpeechText(DialogueSpeechNode node, string value)
        {
            if (node == null)
            {
                return;
            }

            Undo.RecordObject(node, "Edit Dialogue Text");
            node.Text = value ?? string.Empty;
            MarkNodeDirty(node);
            RefreshNodeDataOnly(node);
        }

        /// <summary>
        /// 设置 Choice 节点显示文本。
        /// </summary>
        /// <param name="node">要修改的 Choice 节点。</param>
        /// <param name="value">新的选项文本。</param>
        public void SetChoiceText(DialogueChoiceNode node, string value)
        {
            if (node == null)
            {
                return;
            }

            Undo.RecordObject(node, "Edit Dialogue Choice");
            node.ChoiceText = value ?? string.Empty;
            MarkNodeDirty(node);
            RefreshNodeDataOnly(node);
        }
        #endregion

        #region 长文本拆分
        /// <summary>
        /// 判断指定 Speech 节点是否可以拆分。
        /// </summary>
        /// <param name="node">待检查的 Speech 节点。</param>
        /// <returns>可以拆分时返回 true。</returns>
        public bool CanSplitSpeech(DialogueSpeechNode node)
        {
            return Graph != null && node != null && !string.IsNullOrWhiteSpace(node.Text) && node.Text.Trim().Length > 1;
        }

        /// <summary>
        /// 将一个 Speech 节点拆成两个连续 Speech 节点，并把原后续分支迁移到新节点后。
        /// </summary>
        /// <param name="node">要拆分的 Speech 节点。</param>
        public void SplitSpeechNode(DialogueSpeechNode node)
        {
            if (!CanSplitSpeech(node))
            {
                return;
            }

            string text = node.Text ?? string.Empty;
            int splitIndex = FindSplitIndex(text);
            string firstPart = text[..splitIndex].Trim();
            string secondPart = text[splitIndex..].Trim();

            if (string.IsNullOrEmpty(firstPart) || string.IsNullOrEmpty(secondPart))
            {
                return;
            }

            Vector2 speechPosition = node.Position;
            DialogueSpeechNode newSpeech = Graph.CreateNode<DialogueSpeechNode>(speechPosition + new Vector2(360f, 0f));
            DialogueNode originalNextNode = node.NextNode;
            List<DialogueChoiceNode> originalChoices = GetChoicesFrom(node).ToList();

            Undo.RecordObject(node, "Split Dialogue Text");
            Undo.RecordObject(newSpeech, "Split Dialogue Text");
            node.Text = firstPart;
            node.NextNode = newSpeech;
            node.ClearChoices();

            newSpeech.EditorTitle = string.IsNullOrWhiteSpace(node.EditorTitle) ? "Speech" : $"{node.EditorTitle} Continued";
            newSpeech.SpeakerId = node.SpeakerId;
            newSpeech.PortraitId = node.PortraitId;
            newSpeech.Text = secondPart;
            newSpeech.NextNode = originalNextNode;
            newSpeech.SetChoices(originalChoices);

            MarkNodeDirty(node);
            MarkNodeDirty(newSpeech);
            MarkGraphDirty();
            SelectNode(newSpeech);
            RefreshAll();
            AssetDatabase.SaveAssets();
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
        /// 通知编辑器指定节点数据已由外部绑定控件修改，需要标记脏数据并重新校验。
        /// </summary>
        /// <param name="node">发生数据变化的对话节点。</param>
        public void NotifyNodeDataChanged(DialogueNode node)
        {
            MarkNodeDirty(node);
            MarkGraphDirty();
            RefreshNodeDataOnly(node);
        }

        /// <summary>
        /// 保存当前对话图资源和节点 sub-asset。
        /// </summary>
        public void Save()
        {
            if (Graph != null)
            {
                EditorUtility.SetDirty(Graph);
            }

            AssetDatabase.SaveAssets();
        }
        #endregion

        #region 自动排版
        /// <summary>
        /// 根据图连接关系计算自动排版层级。该方法只计算层级，不负责节点几何排布。
        /// </summary>
        /// <returns>节点到层级序号的映射；不可达节点不会出现在映射中。</returns>
        public Dictionary<DialogueNode, int> CalculateAutoLayoutLayers()
        {
            if (Graph == null || Graph.StartNode == null)
            {
                return new Dictionary<DialogueNode, int>();
            }

            return CalculateLayers(Graph.EnumerateNodes().ToList());
        }

        private Dictionary<DialogueNode, int> CalculateLayers(IReadOnlyList<DialogueNode> allNodes)
        {
            Dictionary<DialogueNode, int> layerByNode = new();
            Dictionary<DialogueNode, HashSet<DialogueNode>> pathByNode = new();
            Queue<DialogueNode> queue = new();

            layerByNode[Graph.StartNode] = 0;
            pathByNode[Graph.StartNode] = new HashSet<DialogueNode> { Graph.StartNode };
            queue.Enqueue(Graph.StartNode);

            int iterationLimit = Mathf.Max(1, allNodes.Count * allNodes.Count);
            int iterations = 0;

            while (queue.Count > 0 && iterations < iterationLimit)
            {
                iterations++;
                DialogueNode current = queue.Dequeue();
                int currentLayer = layerByNode[current];

                foreach (DialogueNode target in GetTargetsFrom(current))
                {
                    if (target == null || !allNodes.Contains(target))
                    {
                        continue;
                    }

                    if (pathByNode.TryGetValue(current, out HashSet<DialogueNode> currentPath) && currentPath.Contains(target))
                    {
                        continue;
                    }

                    int targetLayer = currentLayer + 1;
                    if (!layerByNode.TryGetValue(target, out int existingLayer) || targetLayer > existingLayer)
                    {
                        layerByNode[target] = targetLayer;
                        pathByNode[target] = currentPath != null
                            ? new HashSet<DialogueNode>(currentPath) { target }
                            : new HashSet<DialogueNode> { target };
                        queue.Enqueue(target);
                    }
                }
            }

            if (iterations >= iterationLimit)
            {
                validationMessages.Add(new DialogueGraphValidationMessage("Auto layout stopped early. The graph may contain a cycle."));
                ValidationChanged?.Invoke();
            }

            return layerByNode;
        }

        #endregion

        #region 校验
        private void RefreshValidation()
        {
            validationMessages.Clear();

            if (Graph == null)
            {
                ValidationChanged?.Invoke();
                return;
            }

            Graph.RemoveNullNodes();

            if (Graph.StartNode == null)
            {
                validationMessages.Add(new DialogueGraphValidationMessage("Missing Start node."));
            }
            else if (Graph.StartNode.NextNode == null)
            {
                validationMessages.Add(new DialogueGraphValidationMessage("Start node has no target Speech node."));
            }

            foreach (DialogueSpeechNode speech in Graph.EnumerateNodes().OfType<DialogueSpeechNode>())
            {
                if (string.IsNullOrWhiteSpace(speech.SpeakerId))
                {
                    validationMessages.Add(new DialogueGraphValidationMessage($"{GetDisplayName(speech)} has no Speaker."));
                }
                else if (DialogueSpeakerDataListLocator.FindSpeaker(speech.SpeakerId) == null)
                {
                    validationMessages.Add(new DialogueGraphValidationMessage($"{GetDisplayName(speech)} references missing Speaker '{speech.SpeakerId}'."));
                }
                else
                {
                    DialogueSpeakerData speaker = DialogueSpeakerDataListLocator.FindSpeaker(speech.SpeakerId);
                    if (!string.IsNullOrWhiteSpace(speech.PortraitId) && !SpeakerHasPortraitId(speaker, speech.PortraitId))
                    {
                        validationMessages.Add(new DialogueGraphValidationMessage($"{GetDisplayName(speech)} references missing Portrait '{speech.PortraitId}' on Speaker '{speech.SpeakerId}'."));
                    }
                }

                bool hasChoices = GetChoicesFrom(speech).Any();
                if (speech.NextNode == null && !hasChoices)
                {
                    validationMessages.Add(new DialogueGraphValidationMessage($"{GetDisplayName(speech)} has no next node or Choice branch."));
                }

                if (speech.NextNode != null && hasChoices)
                {
                    validationMessages.Add(new DialogueGraphValidationMessage($"{GetDisplayName(speech)} has both a linear next node and Choice branches."));
                }
            }

            foreach (DialogueChoiceNode choice in Graph.EnumerateNodes().OfType<DialogueChoiceNode>())
            {
                if (!IsChoiceOwned(choice))
                {
                    validationMessages.Add(new DialogueGraphValidationMessage($"{GetDisplayName(choice)} has no source Speech node."));
                }

                if (choice.TargetNode == null)
                {
                    validationMessages.Add(new DialogueGraphValidationMessage($"{GetDisplayName(choice)} has no target node."));
                }
            }

            ValidateSpeakerDataList();

            ValidationChanged?.Invoke();
        }

        private void ValidateSpeakerDataList()
        {
            int dataListCount = DialogueSpeakerDataListLocator.GetDataListCount();
            if (dataListCount == 0)
            {
                validationMessages.Add(new DialogueGraphValidationMessage("Missing DialogueSpeakerDataList_SO asset."));
                return;
            }

            if (dataListCount > 1)
            {
                validationMessages.Add(new DialogueGraphValidationMessage("Multiple DialogueSpeakerDataList_SO assets found. The editor uses the first one."));
            }

            DialogueSpeakerDataList_SO dataList = DialogueSpeakerDataListLocator.GetDataList();
            if (dataList?.items == null)
            {
                return;
            }

            foreach (IGrouping<string, DialogueSpeakerData> duplicateGroup in dataList.items
                         .Where(item => item != null && !string.IsNullOrWhiteSpace(item.speakerId))
                         .GroupBy(item => item.speakerId)
                         .Where(group => group.Count() > 1))
            {
                validationMessages.Add(new DialogueGraphValidationMessage($"Duplicate SpeakerId '{duplicateGroup.Key}' in DialogueSpeakerDataList_SO."));
            }

            foreach (DialogueSpeakerData speaker in dataList.items.Where(item => item != null))
            {
                if (string.IsNullOrWhiteSpace(speaker.speakerId))
                {
                    validationMessages.Add(new DialogueGraphValidationMessage("DialogueSpeakerDataList_SO contains a Speaker with empty speakerId."));
                }

                if (string.IsNullOrWhiteSpace(speaker.portraitAtlasAddress) && speaker.portraitIds != null && speaker.portraitIds.Any(item => !string.IsNullOrWhiteSpace(item)))
                {
                    validationMessages.Add(new DialogueGraphValidationMessage($"{speaker.speakerId} has portraitIds but no portraitAtlasAddress."));
                }

                if (speaker.portraitIds == null)
                {
                    continue;
                }

                if (speaker.portraitIds.Any(string.IsNullOrWhiteSpace))
                {
                    validationMessages.Add(new DialogueGraphValidationMessage($"{speaker.speakerId} has empty portraitId."));
                }

                foreach (IGrouping<string, string> duplicateGroup in speaker.portraitIds
                             .Where(item => !string.IsNullOrWhiteSpace(item))
                             .GroupBy(item => item)
                             .Where(group => group.Count() > 1))
                {
                    validationMessages.Add(new DialogueGraphValidationMessage($"{speaker.speakerId} has duplicate portraitId '{duplicateGroup.Key}'."));
                }

                if (!string.IsNullOrWhiteSpace(speaker.defaultPortraitId) && !SpeakerHasPortraitId(speaker, speaker.defaultPortraitId))
                {
                    validationMessages.Add(new DialogueGraphValidationMessage($"{speaker.speakerId} defaultPortraitId '{speaker.defaultPortraitId}' is not in portraitIds."));
                }
            }
        }
        #endregion

        #region 数据维护
        private void EnsureGraphIdentity()
        {
            if (string.IsNullOrEmpty(Graph.GraphGuid))
            {
                Graph.GraphGuid = GUID.Generate().ToString();
            }

            if (string.IsNullOrEmpty(Graph.DisplayName))
            {
                Graph.DisplayName = Graph.name;
            }
        }

        private void ApplyDefaultNodeValues(DialogueNode node)
        {
            switch (node)
            {
                case DialogueStartNode start:
                    start.EditorTitle = "Start";
                    start.name = "Start";
                    break;
                case DialogueSpeechNode speech:
                    speech.EditorTitle = "Speech";
                    speech.SpeakerId = DialogueSpeakerDataListLocator.GetFirstSpeakerId();
                    speech.PortraitId = DialogueSpeakerDataListLocator.GetFirstPortraitId(speech.SpeakerId);
                    speech.Text = "New dialogue line.";
                    break;
                case DialogueChoiceNode choice:
                    choice.EditorTitle = "Choice";
                    choice.ChoiceText = "New choice";
                    break;
                case DialogueEndNode end:
                    end.EditorTitle = "End";
                    break;
            }
        }

        private void RecordReferencesTo(DialogueNode target, string undoName)
        {
            if (Graph == null || target == null)
            {
                return;
            }

            if (Graph.StartNode != null && Graph.StartNode.NextNode == target)
            {
                Undo.RecordObject(Graph.StartNode, undoName);
            }

            foreach (DialogueSpeechNode speech in Graph.EnumerateNodes().OfType<DialogueSpeechNode>())
            {
                if (speech.NextNode == target || OwnsChoice(speech, target as DialogueChoiceNode))
                {
                    Undo.RecordObject(speech, undoName);
                }
            }

            foreach (DialogueChoiceNode choice in Graph.EnumerateNodes().OfType<DialogueChoiceNode>())
            {
                if (choice.TargetNode == target)
                {
                    Undo.RecordObject(choice, undoName);
                }
            }
        }

        private void MoveChoiceToSpeech(DialogueSpeechNode source, DialogueChoiceNode choice, string undoName)
        {
            if (source == null || choice == null)
            {
                return;
            }

            foreach (DialogueSpeechNode speech in Graph.EnumerateNodes().OfType<DialogueSpeechNode>())
            {
                if (speech == null || !OwnsChoice(speech, choice))
                {
                    continue;
                }

                Undo.RecordObject(speech, undoName);
                speech.RemoveChoice(choice);
                MarkNodeDirty(speech);
            }

            Undo.RecordObject(source, undoName);
            source.AddChoice(choice);
            MarkNodeDirty(source);
        }

        private bool IsChoiceOwned(DialogueChoiceNode choice)
        {
            return choice != null &&
                   Graph != null &&
                   Graph.EnumerateNodes().OfType<DialogueSpeechNode>().Any(speech => OwnsChoice(speech, choice));
        }

        private static bool OwnsChoice(DialogueSpeechNode speech, DialogueChoiceNode choice)
        {
            return speech != null && choice != null && speech.Choices.Contains(choice);
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
            MarkGraphDirty();
            RefreshValidation();
            NodeDataChanged?.Invoke(node);
        }

        private void RefreshSelectedNodeDataAndDetails(DialogueNode node)
        {
            RefreshNodeDataOnly(node);
            SelectionChanged?.Invoke();
        }
        #endregion

        #region 工具方法
        private static int FindSplitIndex(string text)
        {
            int middle = text.Length / 2;
            char[] preferred = { '。', '！', '？', '.', '!', '?', '\n', '，', ',', ' ' };

            foreach (char separator in preferred)
            {
                int right = text.IndexOf(separator, middle);
                if (right > 0 && right < text.Length - 1)
                {
                    return right + 1;
                }

                int left = text.LastIndexOf(separator, middle);
                if (left > 0 && left < text.Length - 1)
                {
                    return left + 1;
                }
            }

            return middle;
        }

        private static string GetDisplayName(DialogueNode node)
        {
            if (node == null)
            {
                return "Missing node";
            }

            if (!string.IsNullOrWhiteSpace(node.EditorTitle))
            {
                return node.EditorTitle;
            }

            return node.name;
        }

        private static bool SpeakerHasPortraitId(DialogueSpeakerData speaker, string portraitId)
        {
            return speaker?.portraitIds != null &&
                   !string.IsNullOrWhiteSpace(portraitId) &&
                   speaker.portraitIds.Contains(portraitId);
        }

        private void MarkGraphDirty()
        {
            if (Graph != null)
            {
                EditorUtility.SetDirty(Graph);
            }
        }

        private static void MarkNodeDirty(DialogueNode node)
        {
            if (node != null)
            {
                EditorUtility.SetDirty(node);
            }
        }
        #endregion
    }
}
