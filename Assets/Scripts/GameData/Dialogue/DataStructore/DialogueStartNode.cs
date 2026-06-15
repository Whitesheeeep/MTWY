using UnityEngine;

namespace GameData
{
    public sealed class DialogueStartNode : DialogueNode
    {
        [SerializeField] private DialogueSpeechNode nextNode;

        public DialogueSpeechNode NextNode
        {
            get => nextNode;
            set => nextNode = value;
        }
    }
}
