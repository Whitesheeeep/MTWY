using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WS_Modules.SceneModule;

namespace WS_Modules
{
    /// <summary>
    /// SceneTransitionTrigger2D 的自定义 Inspector，用于按目标场景分层选择 Route。
    /// </summary>
    [CustomEditor(typeof(SceneTransitionTrigger2D))]
    internal sealed class SceneTransitionTrigger2DEditor : Editor
    {
        private SerializedProperty travelerLayerMaskProperty;
        private SerializedProperty transitionConfigProperty;
        private SerializedProperty routeIdProperty;

        private void OnEnable()
        {
            travelerLayerMaskProperty = serializedObject.FindProperty("travelerLayerMask");
            transitionConfigProperty = serializedObject.FindProperty("transitionConfig");
            routeIdProperty = serializedObject.FindProperty("routeId");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(travelerLayerMaskProperty);
            EditorGUILayout.PropertyField(transitionConfigProperty);
            DrawRouteSelector();

            serializedObject.ApplyModifiedProperties();
        }

        // 绘制 Route 选择按钮和缺失提示。
        private void DrawRouteSelector()
        {
            SceneTransitionConfig config = transitionConfigProperty.objectReferenceValue as SceneTransitionConfig;
            using (new EditorGUI.DisabledScope(config == null))
            {
                string buttonText = GetRouteButtonText(config, routeIdProperty.stringValue);
                if (GUILayout.Button(buttonText, EditorStyles.popup))
                {
                    ShowRouteMenu(config);
                }
            }

            if (config == null)
            {
                EditorGUILayout.HelpBox("Assign a SceneTransitionConfig to select a route.", MessageType.Info);
                return;
            }

            if (string.IsNullOrWhiteSpace(routeIdProperty.stringValue))
            {
                EditorGUILayout.HelpBox("No route selected.", MessageType.Warning);
                return;
            }

            if (!config.TryGetRoute(routeIdProperty.stringValue, out _))
            {
                EditorGUILayout.HelpBox(
                    $"Route id '{routeIdProperty.stringValue}' was not found in the selected config.",
                    MessageType.Warning);
            }
        }

        // 显示按目标场景分层的 Route 菜单。
        private void ShowRouteMenu(SceneTransitionConfig config)
        {
            var menu = new GenericMenu();
            IReadOnlyList<SceneTransitionRoute> routes = config.Routes;
            if (routes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No Routes"));
                menu.ShowAsContext();
                return;
            }

            int menuItemCount = 0;
            for (int i = 0; i < routes.Count; i++)
            {
                SceneTransitionRoute route = routes[i];
                if (route == null || string.IsNullOrWhiteSpace(route.RouteId))
                {
                    continue;
                }

                string menuPath = CreateRouteMenuPath(route);
                bool selected = route.RouteId == routeIdProperty.stringValue;
                menuItemCount++;
                menu.AddItem(
                    new GUIContent(menuPath),
                    selected,
                    selectedRouteId =>
                    {
                        serializedObject.Update();
                        routeIdProperty.stringValue = (string)selectedRouteId;
                        serializedObject.ApplyModifiedProperties();
                    },
                    route.RouteId);
            }

            if (menuItemCount == 0)
            {
                menu.AddDisabledItem(new GUIContent("No Valid Routes"));
            }

            menu.ShowAsContext();
        }

        // 获取 Route 按钮显示文本。
        private static string GetRouteButtonText(SceneTransitionConfig config, string routeId)
        {
            if (config == null)
            {
                return "Route: Missing Config";
            }

            if (string.IsNullOrWhiteSpace(routeId))
            {
                return "Route: None";
            }

            return config.TryGetRoute(routeId, out SceneTransitionRoute route)
                ? $"Route: {CreateRouteMenuPath(route)}"
                : $"Route: Missing ({routeId})";
        }

        // 生成目标场景分层菜单路径。
        private static string CreateRouteMenuPath(SceneTransitionRoute route)
        {
            string targetScene = string.IsNullOrWhiteSpace(route.TargetSceneName)
                ? "Missing Target Scene"
                : route.TargetSceneName;
            string routeName = !string.IsNullOrWhiteSpace(route.DisplayName)
                ? route.DisplayName
                : route.TargetSpawnId;

            if (string.IsNullOrWhiteSpace(routeName))
            {
                routeName = route.RouteId;
            }

            return $"{SanitizeMenuSegment(targetScene)}/{SanitizeMenuSegment(routeName)}";
        }

        // 避免配置文本中的斜杠被 GenericMenu 当成额外层级。
        private static string SanitizeMenuSegment(string value)
        {
            return value.Replace("/", "／").Replace("\\", "＼");
        }
    }
}
