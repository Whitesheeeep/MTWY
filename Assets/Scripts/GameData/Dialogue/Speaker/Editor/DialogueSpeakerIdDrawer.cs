using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameData.Editor
{
    [CustomPropertyDrawer(typeof(DialogueSpeakerIdAttribute))]
    internal sealed class DialogueSpeakerIdDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            DialogueSpeakerDataList_SO dataList = DialogueSpeakerDataListLocator.GetDataList();
            List<DialogueSpeakerData> speakers = dataList?.items?
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.speakerId))
                .ToList() ?? new List<DialogueSpeakerData>();

            if (speakers.Count == 0)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            List<string> ids = speakers.Select(item => item.speakerId).ToList();
            string currentId = property.stringValue ?? string.Empty;
            int currentIndex = ids.IndexOf(currentId);
            bool hasMissingValue = !string.IsNullOrWhiteSpace(currentId) && currentIndex < 0;

            List<string> labels = speakers
                .Select(item => string.IsNullOrWhiteSpace(item.speakerName)
                    ? item.speakerId
                    : $"{item.speakerName} ({item.speakerId})")
                .ToList();

            ids.Insert(0, string.Empty);
            labels.Insert(0, "No Speaker");
            currentIndex++;

            if (hasMissingValue)
            {
                ids.Insert(1, currentId);
                labels.Insert(1, $"Missing: {currentId}");
                currentIndex = 1;
            }

            int selectedIndex = EditorGUI.Popup(position, label.text, currentIndex, labels.ToArray());
            if (selectedIndex >= 0 && selectedIndex < ids.Count)
            {
                property.stringValue = ids[selectedIndex];
            }
        }
    }
}
