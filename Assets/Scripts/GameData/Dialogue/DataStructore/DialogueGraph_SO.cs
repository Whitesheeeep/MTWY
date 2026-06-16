using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameData
{
    [CreateAssetMenu(fileName = "DialogueGraph", menuName = "GameData/Dialogue/Dialogue Graph", order = 0)]
    public sealed class DialogueGraph_SO : ScriptableObject
    {
        [SerializeField] private string graphGuid;
        [SerializeField] private string displayName;
        [SerializeField] private DialogueStartNode startNode;
        [SerializeField] private List<DialogueNode> nodes = new List<DialogueNode>();

        public string GraphGuid
        {
            get => graphGuid;
            set => graphGuid = value;
        }

        public string DisplayName
        {
            get => displayName;
            set => displayName = value;
        }

        public DialogueStartNode StartNode
        {
            get => startNode;
            set => startNode = value;
        }

        public List<DialogueNode> Nodes => nodes;

        // 用于编辑器中枚举所有节点的便利方法，避免直接暴露 List<DialogueNode> 的枚举器可能带来的 null 引用问题。
        public IEnumerable<DialogueNode> EnumerateNodes()
        {
            EnsureNodeList();

            foreach (DialogueNode node in nodes)
            {
                if (node != null)
                {
                    yield return node;
                }
            }
        }

        public void EnsureNodeList()
        {
            nodes ??= new List<DialogueNode>();
        }

        public void RemoveNullNodes()
        {
            EnsureNodeList();
            nodes.RemoveAll(node => node == null);
        }

        public void ClearReferencesTo(DialogueNode target)
        {
            if (target == null)
            {
                return;
            }

            if (startNode != null && startNode.NextNode == target)
            {
                startNode.NextNode = null;
            }

            foreach (DialogueNode node in EnumerateNodes())
            {
                if (node is DialogueSpeechNode speech && speech.NextNode == target)
                {
                    speech.NextNode = null;
                }

                if (node is DialogueSpeechNode speechWithChoices)
                {
                    speechWithChoices.RemoveChoice(target as DialogueChoiceNode);
                    speechWithChoices.RemoveNullChoices();
                }

                if (node is DialogueChoiceNode choice)
                {
                    if (choice.TargetNode == target)
                    {
                        choice.TargetNode = null;
                    }
                }
            }
        }
    }
}
