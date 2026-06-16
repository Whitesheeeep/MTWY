using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameData.Editor
{
    /// <summary>
    /// 对话图编辑器命令服务，负责资产初始化、节点创建删除、字段编辑、拆分和保存。
    /// </summary>
    internal sealed class DialogueGraphEditorGraphCommands
    {
        #region 图资源
        /// <summary>
        /// 初始化对话图的基础编辑数据。
        /// </summary>
        public void InitializeGraph(DialogueGraph_SO graph)
        {
            if (graph == null)
            {
                return;
            }

            Undo.RecordObject(graph, "Open Dialogue Graph");
            EnsureGraphIdentity(graph);
            EnsureStartNode(graph);
            graph.RemoveNullNodes();
            MarkGraphDirty(graph);
        }

        /// <summary>
        /// 设置图资源显示名称。
        /// </summary>
        public bool SetGraphDisplayName(DialogueGraph_SO graph, string value)
        {
            if (graph == null)
            {
                return false;
            }

            Undo.RecordObject(graph, "Edit Dialogue Graph Name");
            graph.DisplayName = value ?? string.Empty;
            MarkGraphDirty(graph);
            return true;
        }

        /// <summary>
        /// 复制整张对话图资源，并重映射复制后节点之间的内部引用。
        /// </summary>
        /// <param name="source">要复制的源对话图资源。</param>
        /// <returns>复制出的新对话图资源。</returns>
        public DialogueGraph_SO DuplicateGraph(DialogueGraph_SO source)
        {
            if (source == null)
            {
                return null;
            }

            string sourcePath = AssetDatabase.GetAssetPath(source);
            if (string.IsNullOrEmpty(sourcePath))
            {
                Debug.LogWarning("[DialogueGraph] Cannot duplicate a graph that is not saved as an asset.");
                return null;
            }

            string targetPath = CreateDuplicateAssetPath(sourcePath, source.name);
            DialogueGraph_SO duplicate = ScriptableObject.CreateInstance<DialogueGraph_SO>();
            EditorUtility.CopySerialized(source, duplicate);
            duplicate.name = $"{source.name}_Copy";
            duplicate.GraphGuid = GUID.Generate().ToString();
            duplicate.DisplayName = string.IsNullOrWhiteSpace(source.DisplayName)
                ? duplicate.name
                : $"{source.DisplayName} Copy";
            duplicate.EnsureNodeList();
            duplicate.Nodes.Clear();
            duplicate.StartNode = null;

            AssetDatabase.CreateAsset(duplicate, targetPath);

            Dictionary<DialogueNode, DialogueNode> nodeMap = DuplicateNodes(source, duplicate);
            RemapDuplicatedReferences(source, duplicate, nodeMap);

            MarkGraphDirty(duplicate);
            foreach (DialogueNode node in duplicate.EnumerateNodes())
            {
                MarkNodeDirty(node);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return duplicate;
        }
        #endregion

        #region 节点创建与删除
        /// <summary>
        /// 创建指定类型的对话节点。
        /// </summary>
        public DialogueNode CreateNode(DialogueGraph_SO graph, Type nodeType, Vector2 position)
        {
            if (graph == null)
            {
                return null;
            }

            DialogueNode node = CreateNodeAsset(graph, nodeType, position);
            ApplyDefaultNodeValues(node);
            MarkNodeDirty(node);
            MarkGraphDirty(graph);
            AssetDatabase.SaveAssets();
            return node;
        }

        /// <summary>
        /// 删除节点并清理所有指向该节点的引用。
        /// </summary>
        public bool DeleteNode(DialogueGraph_SO graph, DialogueNode node, DialogueGraphConnectionService connectionService)
        {
            if (graph == null || node == null || node == graph.StartNode)
            {
                return false;
            }

            connectionService.RecordReferencesTo(graph, node, "Delete Dialogue Node");
            Undo.RecordObject(graph, "Delete Dialogue Node");
            graph.ClearReferencesTo(node);
            graph.Nodes.Remove(node);
            MarkGraphDirty(graph);
            Undo.DestroyObjectImmediate(node);
            AssetDatabase.SaveAssets();
            return true;
        }
        #endregion

        #region 节点编辑
        /// <summary>
        /// 记录 GraphView 拖拽产生的位置变化。
        /// </summary>
        public bool MoveNode(DialogueNode node, Vector2 position)
        {
            if (node == null)
            {
                return false;
            }

            Undo.RecordObject(node, "Move Dialogue Node");
            node.Position = position;
            MarkNodeDirty(node);
            return true;
        }

        /// <summary>
        /// 根据节点视图真实矩形写回节点位置。
        /// </summary>
        public bool SetNodePositionFromView(DialogueGraph_SO graph, DialogueNode node, Vector2 position, string undoName)
        {
            if (node == null)
            {
                return false;
            }

            Undo.RecordObject(node, undoName);
            node.Position = position;
            MarkNodeDirty(node);
            MarkGraphDirty(graph);
            return true;
        }

        /// <summary>
        /// 设置节点编辑器标题。
        /// </summary>
        public bool SetNodeTitle(DialogueNode node, string value)
        {
            if (node == null)
            {
                return false;
            }

            Undo.RecordObject(node, "Edit Dialogue Node Title");
            node.EditorTitle = value ?? string.Empty;
            MarkNodeDirty(node);
            return true;
        }

        /// <summary>
        /// 设置 Speech 节点 SpeakerId。
        /// </summary>
        public bool SetSpeakerId(DialogueSpeechNode node, string value)
        {
            if (node == null)
            {
                return false;
            }

            Undo.RecordObject(node, "Edit Dialogue Speaker");
            node.SpeakerId = value ?? string.Empty;
            node.PortraitId = string.Empty;
            MarkNodeDirty(node);
            return true;
        }

        /// <summary>
        /// 设置 Speech 节点头像 Id。
        /// </summary>
        public bool SetPortraitId(DialogueSpeechNode node, string value)
        {
            if (node == null)
            {
                return false;
            }

            Undo.RecordObject(node, "Edit Dialogue Portrait");
            node.PortraitId = value ?? string.Empty;
            MarkNodeDirty(node);
            return true;
        }

        /// <summary>
        /// 设置 Speech 节点文本。
        /// </summary>
        public bool SetSpeechText(DialogueSpeechNode node, string value)
        {
            if (node == null)
            {
                return false;
            }

            Undo.RecordObject(node, "Edit Dialogue Text");
            node.Text = value ?? string.Empty;
            MarkNodeDirty(node);
            return true;
        }

        /// <summary>
        /// 设置 Choice 节点文本。
        /// </summary>
        public bool SetChoiceText(DialogueChoiceNode node, string value)
        {
            if (node == null)
            {
                return false;
            }

            Undo.RecordObject(node, "Edit Dialogue Choice");
            node.ChoiceText = value ?? string.Empty;
            MarkNodeDirty(node);
            return true;
        }
        #endregion

        #region 长文本拆分
        /// <summary>
        /// 判断 Speech 节点是否可以拆分。
        /// </summary>
        public bool CanSplitSpeech(DialogueGraph_SO graph, DialogueSpeechNode node)
        {
            return graph != null && node != null && !string.IsNullOrWhiteSpace(node.Text) && node.Text.Trim().Length > 1;
        }

        /// <summary>
        /// 拆分 Speech 节点，并把原后续关系迁移到新节点。
        /// </summary>
        public DialogueSpeechNode SplitSpeechNode(
            DialogueGraph_SO graph,
            DialogueSpeechNode node,
            DialogueGraphConnectionService connectionService)
        {
            if (!CanSplitSpeech(graph, node))
            {
                return null;
            }

            string text = node.Text ?? string.Empty;
            int splitIndex = FindSplitIndex(text);
            string firstPart = text[..splitIndex].Trim();
            string secondPart = text[splitIndex..].Trim();

            if (string.IsNullOrEmpty(firstPart) || string.IsNullOrEmpty(secondPart))
            {
                return null;
            }

            Vector2 speechPosition = node.Position;
            DialogueSpeechNode newSpeech = CreateNodeAsset<DialogueSpeechNode>(graph, speechPosition + new Vector2(360f, 0f));
            DialogueNode originalNextNode = node.NextNode;
            List<DialogueChoiceNode> originalChoices = connectionService.GetChoicesFrom(node).ToList();

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
            MarkGraphDirty(graph);
            AssetDatabase.SaveAssets();
            return newSpeech;
        }
        #endregion

        #region 保存与 Dirty
        /// <summary>
        /// 标记节点数据已被外部绑定控件修改。
        /// </summary>
        public void MarkNodeDataChanged(DialogueGraph_SO graph, DialogueNode node)
        {
            MarkNodeDirty(node);
            MarkGraphDirty(graph);
        }

        /// <summary>
        /// 保存当前对话图资产。
        /// </summary>
        public void Save(DialogueGraph_SO graph)
        {
            MarkGraphDirty(graph);
            AssetDatabase.SaveAssets();
        }
        #endregion

        #region 默认值
        private static void ApplyDefaultNodeValues(DialogueNode node)
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
        #endregion

        #region 工具方法
        private static TNode CreateNodeAsset<TNode>(DialogueGraph_SO graph, Vector2 position)
            where TNode : DialogueNode
        {
            return (TNode)CreateNodeAsset(graph, typeof(TNode), position);
        }

        private static string CreateDuplicateAssetPath(string sourcePath, string sourceName)
        {
            string directory = System.IO.Path.GetDirectoryName(sourcePath)?.Replace('\\', '/') ?? "Assets";
            string fileName = string.IsNullOrWhiteSpace(sourceName) ? "DialogueGraph" : sourceName;
            string targetPath = $"{directory}/{fileName}_Copy.asset";
            return AssetDatabase.GenerateUniqueAssetPath(targetPath);
        }

        private static Dictionary<DialogueNode, DialogueNode> DuplicateNodes(DialogueGraph_SO source, DialogueGraph_SO duplicate)
        {
            Dictionary<DialogueNode, DialogueNode> nodeMap = new();
            source.RemoveNullNodes();
            duplicate.EnsureNodeList();

            foreach (DialogueNode sourceNode in source.EnumerateNodes())
            {
                DialogueNode duplicateNode = ScriptableObject.CreateInstance(sourceNode.GetType()) as DialogueNode;
                if (duplicateNode == null)
                {
                    Debug.LogError("Failed to create duplicate node of type " + sourceNode.GetType().Name);
                    continue;
                }
                EditorUtility.CopySerialized(sourceNode, duplicateNode);

                duplicateNode.Guid = GUID.Generate().ToString();
                duplicateNode.name = $"{sourceNode.name}_Copy";

                duplicate.Nodes.Add(duplicateNode);
                AssetDatabase.AddObjectToAsset(duplicateNode, duplicate);
                nodeMap.Add(sourceNode, duplicateNode);
            }

            return nodeMap;
        }

        private static void RemapDuplicatedReferences(
            DialogueGraph_SO source,
            DialogueGraph_SO duplicate,
            IReadOnlyDictionary<DialogueNode, DialogueNode> nodeMap)
        {
            duplicate.StartNode = TryGetMappedNode<DialogueStartNode>(source.StartNode, nodeMap);

            foreach (DialogueNode sourceNode in source.EnumerateNodes())
            {
                if (!nodeMap.TryGetValue(sourceNode, out DialogueNode duplicateNode))
                {
                    continue;
                }

                switch (sourceNode)
                {
                    case DialogueStartNode sourceStart when duplicateNode is DialogueStartNode duplicateStart:
                        duplicateStart.NextNode = TryGetMappedNode<DialogueSpeechNode>(sourceStart.NextNode, nodeMap);
                        break;
                    case DialogueSpeechNode sourceSpeech when duplicateNode is DialogueSpeechNode duplicateSpeech:
                        duplicateSpeech.NextNode = TryGetMappedNode<DialogueNode>(sourceSpeech.NextNode, nodeMap);
                        duplicateSpeech.SetChoices(sourceSpeech.Choices
                            .Select(choice => TryGetMappedNode<DialogueChoiceNode>(choice, nodeMap))
                            .Where(choice => choice != null));
                        break;
                    case DialogueChoiceNode sourceChoice when duplicateNode is DialogueChoiceNode duplicateChoice:
                        duplicateChoice.TargetNode = TryGetMappedNode<DialogueNode>(sourceChoice.TargetNode, nodeMap);
                        break;
                }
            }
        }

        private static TNode TryGetMappedNode<TNode>(
            DialogueNode sourceNode,
            IReadOnlyDictionary<DialogueNode, DialogueNode> nodeMap)
            where TNode : DialogueNode
        {
            if (sourceNode == null)
            {
                return null;
            }

            return nodeMap.TryGetValue(sourceNode, out DialogueNode duplicateNode)
                ? duplicateNode as TNode
                : null;
        }

        private static DialogueNode CreateNodeAsset(DialogueGraph_SO graph, Type nodeType, Vector2 position)
        {
            if (graph == null)
            {
                return null;
            }

            if (nodeType == null)
            {
                throw new ArgumentNullException(nameof(nodeType));
            }

            if (!typeof(DialogueNode).IsAssignableFrom(nodeType))
            {
                throw new ArgumentException($"{nodeType.Name} does not inherit DialogueNode.", nameof(nodeType));
            }

            graph.EnsureNodeList();

            DialogueNode node = ScriptableObject.CreateInstance(nodeType) as DialogueNode ?? throw new InvalidOperationException($"Failed to create instance of {nodeType.Name}.");
            node.Guid = GUID.Generate().ToString();
            node.EditorTitle = ObjectNames.NicifyVariableName(nodeType.Name);
            node.Position = position;
            node.name = $"{nodeType.Name}_{node.Guid[..8]}";

            Undo.RecordObject(graph, "Create Dialogue Node");
            graph.Nodes.Add(node);

            AssetDatabase.AddObjectToAsset(node, graph);
            Undo.RegisterCreatedObjectUndo(node, "Create Dialogue Node");

            if (node is DialogueStartNode createdStart)
            {
                graph.StartNode = createdStart;
            }

            MarkGraphDirty(graph);
            MarkNodeDirty(node);
            return node;
        }

        private DialogueStartNode EnsureStartNode(DialogueGraph_SO graph)
        {
            if (graph == null)
            {
                return null;
            }

            graph.RemoveNullNodes();

            if (graph.StartNode != null && graph.Nodes.Contains(graph.StartNode))
            {
                return graph.StartNode;
            }

            foreach (DialogueNode node in graph.Nodes)
            {
                if (node is DialogueStartNode existingStart)
                {
                    Undo.RecordObject(graph, "Assign Dialogue Start Node");
                    graph.StartNode = existingStart;
                    MarkGraphDirty(graph);
                    return graph.StartNode;
                }
            }

            DialogueStartNode created = CreateNodeAsset<DialogueStartNode>(graph, new Vector2(80f, 180f));
            created.EditorTitle = "Start";
            created.name = "Start";
            MarkNodeDirty(created);
            return created;
        }

        private static void EnsureGraphIdentity(DialogueGraph_SO graph)
        {
            if (string.IsNullOrEmpty(graph.GraphGuid))
            {
                graph.GraphGuid = GUID.Generate().ToString();
            }

            if (string.IsNullOrEmpty(graph.DisplayName))
            {
                graph.DisplayName = graph.name;
            }
        }

        private static int FindSplitIndex(string text)
        {
            int middle = text.Length / 2;
            char[] preferred = { '。', '；', '，', '.', '!', '?', '\n', ';', ',', ' ' };

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

        private static void MarkGraphDirty(DialogueGraph_SO graph)
        {
            if (graph != null)
            {
                EditorUtility.SetDirty(graph);
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
