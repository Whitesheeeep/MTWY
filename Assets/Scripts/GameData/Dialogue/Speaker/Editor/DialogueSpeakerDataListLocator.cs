using System.Linq;
using UnityEditor;

namespace GameData.Editor
{
    internal static class DialogueSpeakerDataListLocator
    {
        // Speaker DateList SO 的 GUID，当前版本中只允许存在一个 DataList SO 时，可以通过固定 GUID 来快速定位资源。
        // 如果发生改变，请替换 GUID
        private const string DataListGuid = "260460f961f4a114eb53fead4c9c335e";

        private static DialogueSpeakerDataList_SO cachedDataList;

        public static DialogueSpeakerDataList_SO GetDataList()
        {
            if (cachedDataList != null)
            {
                return cachedDataList;
            }

            string path = AssetDatabase.GUIDToAssetPath(DataListGuid);
            cachedDataList = string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<DialogueSpeakerDataList_SO>(path);

            return cachedDataList;
        }

        public static int GetDataListCount()
        {
            return GetDataList() == null ? 0 : 1;
        }

        public static void ClearCache()
        {
            cachedDataList = null;
        }

        public static DialogueSpeakerData FindSpeaker(string speakerId)
        {
            DialogueSpeakerDataList_SO dataList = GetDataList();
            return dataList?.items?.FirstOrDefault(item => item != null && item.speakerId == speakerId);
        }

        public static string GetSpeakerDisplayName(string speakerId)
        {
            DialogueSpeakerData speaker = FindSpeaker(speakerId);
            if (speaker == null)
            {
                return string.IsNullOrWhiteSpace(speakerId) ? "No Speaker" : speakerId;
            }

            return string.IsNullOrWhiteSpace(speaker.speakerName) ? speaker.speakerId : speaker.speakerName;
        }

        public static string GetFirstSpeakerId()
        {
            DialogueSpeakerDataList_SO dataList = GetDataList();
            DialogueSpeakerData firstSpeaker = dataList?.items?.FirstOrDefault(item => item != null && !string.IsNullOrWhiteSpace(item.speakerId));
            return firstSpeaker?.speakerId ?? string.Empty;
        }

        public static string GetFirstPortraitId(string speakerId)
        {
            DialogueSpeakerData speaker = FindSpeaker(speakerId);
            if (speaker == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(speaker.defaultPortraitId) &&
                speaker.portraitIds != null &&
                speaker.portraitIds.Contains(speaker.defaultPortraitId))
            {
                return speaker.defaultPortraitId;
            }

            return speaker.portraitIds?.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item)) ?? string.Empty;
        }
    }
}
