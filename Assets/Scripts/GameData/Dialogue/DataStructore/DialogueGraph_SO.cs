using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

#if UNITY_EDITOR
        public TNode CreateNode<TNode>(Vector2 position)
            where TNode : DialogueNode
        {
            return (TNode)CreateNode(typeof(TNode), position);
        }

        public DialogueNode CreateNode(Type nodeType, Vector2 position)
        {
            if (nodeType == null)
            {
                throw new ArgumentNullException(nameof(nodeType));
            }

            if (!typeof(DialogueNode).IsAssignableFrom(nodeType))
            {
                throw new ArgumentException($"{nodeType.Name} does not inherit DialogueNode.", nameof(nodeType));
            }

            EnsureNodeList();

            DialogueNode node = CreateInstance(nodeType) as DialogueNode;
            node.Guid = GUID.Generate().ToString();
            node.EditorTitle = ObjectNames.NicifyVariableName(nodeType.Name);
            node.Position = position;
            node.name = $"{nodeType.Name}_{node.Guid[..8]}";

            Undo.RecordObject(this, "Create Dialogue Node");
            nodes.Add(node);

            AssetDatabase.AddObjectToAsset(node, this);
            Undo.RegisterCreatedObjectUndo(node, "Create Dialogue Node");

            if (node is DialogueStartNode createdStart)
            {
                startNode = createdStart;
            }

            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(node);
            return node;
        }

        public DialogueStartNode EnsureStartNode()
        {
            RemoveNullNodes();

            if (startNode != null && nodes.Contains(startNode))
            {
                return startNode;
            }

            foreach (DialogueNode node in nodes)
            {
                if (node is DialogueStartNode existingStart)
                {
                    Undo.RecordObject(this, "Assign Dialogue Start Node");
                    startNode = existingStart;
                    EditorUtility.SetDirty(this);
                    return startNode;
                }
            }

            DialogueStartNode created = CreateNode<DialogueStartNode>(new Vector2(80f, 180f));
            created.EditorTitle = "Start";
            created.name = "Start";
            return created;
        }

        public void DeleteNode(DialogueNode node)
        {
            if (node == null || node == startNode)
            {
                return;
            }

            Undo.RecordObject(this, "Delete Dialogue Node");
            ClearReferencesTo(node);
            nodes.Remove(node);
            EditorUtility.SetDirty(this);

            Undo.DestroyObjectImmediate(node);
        }
#endif
    }
}
