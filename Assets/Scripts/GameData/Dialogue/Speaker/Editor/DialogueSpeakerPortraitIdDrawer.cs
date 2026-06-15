using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameData.Editor
{
    /// <summary>
    /// 根据同一序列化对象上的 speakerId，把字符串字段绘制为该 Speaker 的头像 Id 下拉框。
    /// </summary>
    [CustomPropertyDrawer(typeof(DialogueSpeakerPortraitIdAttribute))]
    internal sealed class DialogueSpeakerPortraitIdDrawer : PropertyDrawer
    {
        #region 绘制
        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            SerializedProperty speakerIdProperty = property.serializedObject.FindProperty("speakerId");
            string speakerId = speakerIdProperty?.stringValue ?? string.Empty;
            DialogueSpeakerData speaker = DialogueSpeakerDataListLocator.FindSpeaker(speakerId);
            List<string> portraitIds = speaker?.portraitIds?
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct()
                .ToList() ?? new List<string>();

            if (portraitIds.Count == 0)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            string currentId = property.stringValue ?? string.Empty;
            bool hasMissingValue = !string.IsNullOrWhiteSpace(currentId) && !portraitIds.Contains(currentId);

            portraitIds.Insert(0, string.Empty);
            List<string> labels = portraitIds
                .Select(item => string.IsNullOrWhiteSpace(item) ? "Default Portrait" : item)
                .ToList();

            int currentIndex = portraitIds.IndexOf(currentId);
            if (hasMissingValue)
            {
                portraitIds.Insert(1, currentId);
                labels.Insert(1, $"Missing: {currentId}");
                currentIndex = 1;
            }

            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            int selectedIndex = EditorGUI.Popup(position, label.text, currentIndex, labels.ToArray());
            if (selectedIndex >= 0 && selectedIndex < portraitIds.Count)
            {
                property.stringValue = portraitIds[selectedIndex];
            }
        }
        #endregion
    }
}
