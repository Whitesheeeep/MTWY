using System.Collections.Generic;
using UnityEngine;

namespace GameData
{
    [CreateAssetMenu(fileName = "DialogueSpeakerDataList", menuName = "GameData/Dialogue/Speaker Data List", order = 1)]
    public sealed class DialogueSpeakerDataList_SO : ScriptableObject
    {
        public List<DialogueSpeakerData> items = new();
    }
}
