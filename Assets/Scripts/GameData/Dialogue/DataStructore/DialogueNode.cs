using UnityEngine;

namespace GameData
{
    public abstract class DialogueNode : ScriptableObject
    {
        [SerializeField] private string guid;
        [SerializeField] private string editorTitle;
        [SerializeField] private Vector2 position;

        public string Guid
        {
            get => guid;
            set => guid = value;
        }

        public string EditorTitle
        {
            get => editorTitle;
            set => editorTitle = value;
        }

        public Vector2 Position
        {
            get => position;
            set => position = value;
        }
    }
}
