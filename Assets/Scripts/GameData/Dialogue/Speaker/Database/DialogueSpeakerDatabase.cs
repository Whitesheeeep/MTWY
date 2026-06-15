using System.Collections.Generic;
using UnityEngine;

namespace GameData
{
    public sealed class DialogueSpeakerDatabase : IDialogueSpeakerDatabase
    {
        private readonly Dictionary<string, DialogueSpeakerData> speakerMap = new Dictionary<string, DialogueSpeakerData>();
        private readonly List<DialogueSpeakerData> speakers = new List<DialogueSpeakerData>();

        public DialogueSpeakerDatabase(DialogueSpeakerDataList_SO dataList)
        {
            Initialize(dataList);
        }

        public bool TryGet(string speakerId, out DialogueSpeakerData speaker)
        {
            if (string.IsNullOrWhiteSpace(speakerId))
            {
                speaker = null;
                return false;
            }

            return speakerMap.TryGetValue(speakerId, out speaker);
        }

        public DialogueSpeakerData Get(string speakerId)
        {
            if (TryGet(speakerId, out DialogueSpeakerData speaker))
            {
                return speaker;
            }

            throw new KeyNotFoundException($"[DialogueSpeakerDatabase] Speaker id not found: {speakerId}");
        }

        public IReadOnlyList<DialogueSpeakerData> GetAll()
        {
            return speakers;
        }

        public void Clear()
        {
            speakerMap.Clear();
            speakers.Clear();
        }

        private void Initialize(DialogueSpeakerDataList_SO dataList)
        {
            Clear();

            if (dataList == null)
            {
                Debug.LogError("[DialogueSpeakerDatabase] DialogueSpeakerDataList_SO is null.");
                return;
            }

            if (dataList.items == null)
            {
                Debug.LogWarning($"[DialogueSpeakerDatabase] Speaker list is null: {dataList.name}");
                return;
            }

            foreach (DialogueSpeakerData speaker in dataList.items)
            {
                if (speaker == null)
                {
                    Debug.LogWarning($"[DialogueSpeakerDatabase] Null speaker skipped in {dataList.name}.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(speaker.speakerId))
                {
                    Debug.LogError($"[DialogueSpeakerDatabase] Empty speaker id skipped in {dataList.name}.");
                    continue;
                }

                if (speakerMap.ContainsKey(speaker.speakerId))
                {
                    Debug.LogError($"[DialogueSpeakerDatabase] Duplicate speaker id skipped: {speaker.speakerId}, name: {speaker.speakerName}");
                    continue;
                }

                speakerMap.Add(speaker.speakerId, speaker);
                speakers.Add(speaker);
            }
        }
    }
}
