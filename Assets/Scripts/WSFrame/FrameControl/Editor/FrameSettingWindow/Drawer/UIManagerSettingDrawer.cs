using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace WS_Modules
{
    /// <summary>
    /// UIManagerSetting 的统一属性绘制器，用于保持 FrameSetting Inspector 与 FrameSettingWindow 显示一致。
    /// </summary>
    [CustomPropertyDrawer(typeof(WSFrameSetting.UIManagerSetting))]
    internal sealed class UIManagerSettingDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 2f;
        private readonly HashSet<string> initializedExpandedProperties = new HashSet<string>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnsureDefaultExpanded(property);
            EditorGUI.BeginProperty(position, label, property);

            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float currentY = foldoutRect.yMax + VerticalSpacing;

                DrawProperty(ref currentY, position, property, "uiRootPath", "UI 根节点存储路径");
                DrawProperty(ref currentY, position, property, "uiCameraPrefabPath", "UI Camera 预制体加载路径");
                DrawProperty(ref currentY, position, property, "uiEventSystemPrefabPath", "UI EventSystem 预制体加载路径");
                DrawProperty(ref currentY, position, property, "windowConfig", "窗口预制体加载路径");
                DrawProperty(ref currentY, position, property, "isSingleMask", "是否单遮");
                DrawProperty(ref currentY, position, property, "BindComponentGeneratorPath", "组件绑定脚本生成路径");
                DrawProperty(ref currentY, position, property, "BindComponentNameSpace", "组件脚本命名空间");
                DrawProperty(ref currentY, position, property, "WindowGeneratorPath", "窗口交互脚本生成路径");
                DrawProperty(ref currentY, position, property, "ItemScriptsGeneratorPath", "Item 脚本生成路径");
                DrawProperty(ref currentY, position, property, "WindowPrefabFolderPathArr", "窗口预制体存放路径");
                DrawProperty(ref currentY, position, property, "UsingNameSpaceArr", "命名空间配置");

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            EnsureDefaultExpanded(property);
            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
            {
                return height;
            }

            height += VerticalSpacing;
            height += GetPropertyHeight(property, "uiRootPath", "UI 根节点存储路径");
            height += GetPropertyHeight(property, "uiCameraPrefabPath", "UI Camera 预制体加载路径");
            height += GetPropertyHeight(property, "uiEventSystemPrefabPath", "UI EventSystem 预制体加载路径");
            height += GetPropertyHeight(property, "windowConfig", "窗口预制体加载路径");
            height += GetPropertyHeight(property, "isSingleMask", "是否单遮");
            height += GetPropertyHeight(property, "BindComponentGeneratorPath", "组件绑定脚本生成路径");
            height += GetPropertyHeight(property, "BindComponentNameSpace", "组件脚本命名空间");
            height += GetPropertyHeight(property, "WindowGeneratorPath", "窗口交互脚本生成路径");
            height += GetPropertyHeight(property, "ItemScriptsGeneratorPath", "Item 脚本生成路径");
            height += GetPropertyHeight(property, "WindowPrefabFolderPathArr", "窗口预制体存放路径");
            height += GetPropertyHeight(property, "UsingNameSpaceArr", "命名空间配置");
            return height;
        }

        private void EnsureDefaultExpanded(SerializedProperty property)
        {
            string key = GetPropertyKey(property);
            if (!initializedExpandedProperties.Add(key))
            {
                return;
            }

            property.isExpanded = true;
        }

        private static string GetPropertyKey(SerializedProperty property)
        {
            UnityEngine.Object targetObject = property.serializedObject.targetObject;
            int targetId = targetObject != null ? targetObject.GetInstanceID() : 0;
            return $"{targetId}:{property.propertyPath}";
        }

        private static void DrawProperty(
            ref float currentY,
            Rect position,
            SerializedProperty rootProperty,
            string relativePropertyName,
            string labelText)
        {
            SerializedProperty childProperty = rootProperty.FindPropertyRelative(relativePropertyName);
            if (childProperty == null)
            {
                return;
            }

            GUIContent childLabel = CreateLabel(relativePropertyName, labelText);
            float height = EditorGUI.GetPropertyHeight(childProperty, childLabel, true);
            Rect propertyRect = new Rect(position.x, currentY, position.width, height);
            EditorGUI.PropertyField(propertyRect, childProperty, childLabel, true);
            currentY += height + VerticalSpacing;
        }

        private static float GetPropertyHeight(SerializedProperty rootProperty, string relativePropertyName, string labelText)
        {
            SerializedProperty childProperty = rootProperty.FindPropertyRelative(relativePropertyName);
            if (childProperty == null)
            {
                return 0f;
            }

            return EditorGUI.GetPropertyHeight(childProperty, CreateLabel(relativePropertyName, labelText), true) + VerticalSpacing;
        }

        private static GUIContent CreateLabel(string relativePropertyName, string labelText)
        {
            FieldInfo field = typeof(WSFrameSetting.UIManagerSetting).GetField(
                relativePropertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            TooltipAttribute tooltipAttribute = field?.GetCustomAttribute<TooltipAttribute>();
            return new GUIContent(labelText, tooltipAttribute?.tooltip);
        }
    }
}
