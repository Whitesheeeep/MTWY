using System.Collections.Generic;

namespace GameData
{
    public interface IDialogueSpeakerDatabase : IGameSubDatabase
    {
        bool TryGet(string speakerId, out DialogueSpeakerData speaker);
        DialogueSpeakerData Get(string speakerId);
        IReadOnlyList<DialogueSpeakerData> GetAll();
    }
}
