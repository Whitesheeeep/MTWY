using UnityEngine;
using WS_Modules.ConfigInstaller;

namespace GameData
{
    [CreateAssetMenu(fileName = "DialogueSpeakerDatabaseRegisterNode", menuName = "GameData/Database/Dialogue Speaker Register Node", order = 3)]
    public sealed class DialogueSpeakerDatabaseRegisterNode : ConfigRegisterNodeBase
    {
        [SerializeField] private DialogueSpeakerDataList_SO speakerDataList;

        public override void Register()
        {
            if (speakerDataList == null)
            {
                Debug.LogError("[DialogueSpeakerDatabaseRegisterNode] DialogueSpeakerDataList_SO is not assigned.");
                return;
            }

            IDialogueSpeakerDatabase speakerDatabase = new DialogueSpeakerDatabase(speakerDataList);
            GameDatabase.Register<IDialogueSpeakerDatabase>(speakerDatabase);
            Debug.Log($"[DialogueSpeakerDatabaseRegisterNode] Registered DialogueSpeakerDatabase: {speakerDataList.items?.Count ?? 0} speakers.");
        }
    }
}
