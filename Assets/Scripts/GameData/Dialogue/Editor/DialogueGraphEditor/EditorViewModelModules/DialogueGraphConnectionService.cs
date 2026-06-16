using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameData.Editor
{
    /// <summary>
    /// 对话图连接关系服务，负责连接规则、连线数据写入和 Choice 归属关系维护。
    /// </summary>
    internal sealed class DialogueGraphConnectionService
    {
        #region 连接规则
        /// <summary>
        /// 判断两个节点是否允许建立从 output 到 input 的连接。
        /// </summary>
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
        #endregion

        #region 连接写入
        /// <summary>
        /// 建立节点连接，并写入对应节点引用。
        /// </summary>
        public bool Connect(DialogueGraph_SO graph, DialogueNode outputNode, DialogueNode inputNode)
        {
            if (graph == null || !CanConnect(outputNode, inputNode))
            {
                return false;
            }

            switch (outputNode)
            {
                case DialogueStartNode start when inputNode is DialogueSpeechNode speech:
                    Undo.RecordObject(start, "Connect Dialogue Edge");
                    start.NextNode = speech;
                    MarkNodeDirty(start);
                    return true;
                case DialogueSpeechNode source when inputNode is DialogueChoiceNode choice:
                    MoveChoiceToSpeech(graph, source, choice, "Connect Dialogue Edge");
                    return true;
                case DialogueSpeechNode speech:
                    Undo.RecordObject(speech, "Connect Dialogue Edge");
                    speech.NextNode = inputNode;
                    MarkNodeDirty(speech);
                    return true;
                case DialogueChoiceNode choice:
                    Undo.RecordObject(choice, "Connect Dialogue Edge");
                    choice.TargetNode = inputNode;
                    MarkNodeDirty(choice);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 断开节点连接，并清空对应节点引用。
        /// </summary>
        public bool Disconnect(DialogueGraph_SO graph, DialogueNode outputNode, DialogueNode inputNode)
        {
            if (graph == null || outputNode == null || inputNode == null)
            {
                return false;
            }

            switch (outputNode)
            {
                case DialogueStartNode start when start.NextNode == inputNode:
                    Undo.RecordObject(start, "Disconnect Dialogue Edge");
                    start.NextNode = null;
                    MarkNodeDirty(start);
                    return true;
                case DialogueSpeechNode source when inputNode is DialogueChoiceNode choice && OwnsChoice(source, choice):
                    Undo.RecordObject(source, "Disconnect Dialogue Edge");
                    source.RemoveChoice(choice);
                    MarkNodeDirty(source);
                    return true;
                case DialogueSpeechNode speech when speech.NextNode == inputNode:
                    Undo.RecordObject(speech, "Disconnect Dialogue Edge");
                    speech.NextNode = null;
                    MarkNodeDirty(speech);
                    return true;
                case DialogueChoiceNode choice when choice.TargetNode == inputNode:
                    Undo.RecordObject(choice, "Disconnect Dialogue Edge");
                    choice.TargetNode = null;
                    MarkNodeDirty(choice);
                    return true;
                default:
                    return false;
            }
        }
        #endregion

        #region 查询
        /// <summary>
        /// 获取某个节点当前指向的所有目标节点。
        /// </summary>
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
        public IEnumerable<DialogueChoiceNode> GetChoicesFrom(DialogueSpeechNode speech)
        {
            if (speech == null)
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

        /// <summary>
        /// 判断 Choice 是否已经归属于任意 Speech。
        /// </summary>
        public bool IsChoiceOwned(DialogueGraph_SO graph, DialogueChoiceNode choice)
        {
            return choice != null &&
                   graph != null &&
                   graph.EnumerateNodes().OfType<DialogueSpeechNode>().Any(speech => OwnsChoice(speech, choice));
        }
        #endregion

        #region 删除辅助
        /// <summary>
        /// 在删除节点前记录所有指向目标节点的引用，确保 Undo 可以完整恢复。
        /// </summary>
        public void RecordReferencesTo(DialogueGraph_SO graph, DialogueNode target, string undoName)
        {
            if (graph == null || target == null)
            {
                return;
            }

            if (graph.StartNode != null && graph.StartNode.NextNode == target)
            {
                Undo.RecordObject(graph.StartNode, undoName);
            }

            foreach (DialogueSpeechNode speech in graph.EnumerateNodes().OfType<DialogueSpeechNode>())
            {
                if (speech.NextNode == target || OwnsChoice(speech, target as DialogueChoiceNode))
                {
                    Undo.RecordObject(speech, undoName);
                }
            }

            foreach (DialogueChoiceNode choice in graph.EnumerateNodes().OfType<DialogueChoiceNode>())
            {
                if (choice.TargetNode == target)
                {
                    Undo.RecordObject(choice, undoName);
                }
            }
        }
        #endregion

        #region Choice 归属
        private void MoveChoiceToSpeech(DialogueGraph_SO graph, DialogueSpeechNode source, DialogueChoiceNode choice, string undoName)
        {
            if (graph == null || source == null || choice == null)
            {
                return;
            }

            foreach (DialogueSpeechNode speech in graph.EnumerateNodes().OfType<DialogueSpeechNode>())
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

        private static bool OwnsChoice(DialogueSpeechNode speech, DialogueChoiceNode choice)
        {
            return speech != null && choice != null && speech.Choices.Contains(choice);
        }
        #endregion

        #region 工具方法
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
