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
        private SerializedProperty routeIdProperty;

        private void OnEnable()
        {
            travelerLayerMaskProperty = serializedObject.FindProperty("travelerLayerMask");
            routeIdProperty = serializedObject.FindProperty("routeId");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(travelerLayerMaskProperty);
            DrawRouteSelector();

            serializedObject.ApplyModifiedProperties();
        }

        // 绘制 Route 选择按钮和缺失提示。
        private void DrawRouteSelector()
        {
            SceneTransitionConfig config = ResolveGlobalTransitionConfig(out string configMessage);
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
                EditorGUILayout.HelpBox(configMessage, MessageType.Info);
                return;
            }

            if (string.IsNullOrWhiteSpace(routeIdProperty.stringValue))
            {
                EditorGUILayout.HelpBox("No route selected.", MessageType.Warning);
                return;
            }

            if (!ContainsRoute(config, routeIdProperty.stringValue))
            {
                EditorGUILayout.HelpBox(
                    $"Route id '{routeIdProperty.stringValue}' was not found in the global SceneTransitionConfig.",
                    MessageType.Warning);
            }
        }

        // 从 WSFrameSetting 中解析全局 SceneTransitionConfig。
        private static SceneTransitionConfig ResolveGlobalTransitionConfig(out string message)
        {
            WSFrameSetting frameSetting = ResolveFrameSetting();
            if (frameSetting == null)
            {
                message = "Assign a WSFrameSetting with SceneTransitionSettings to select a route.";
                return null;
            }

            SceneTransitionConfig config = frameSetting.SceneTransitionSettings.TransitionConfig;
            if (config == null)
            {
                message = "Assign SceneTransitionSettings.TransitionConfig in WSFrameSetting to select a route.";
                return null;
            }

            message = string.Empty;
            return config;
        }

        // 优先使用场景中 WSFrameRoot 的设置，否则使用项目中的第一个 WSFrameSetting 资产。
        private static WSFrameSetting ResolveFrameSetting()
        {
            WSFrameRoot[] roots = Resources.FindObjectsOfTypeAll<WSFrameRoot>();
            for (int i = 0; i < roots.Length; i++)
            {
                WSFrameRoot root = roots[i];
                if (root != null && root.FrameSetting != null)
                {
                    return root.FrameSetting;
                }
            }

            string[] settingGuids = AssetDatabase.FindAssets("t:WSFrameSetting");
            if (settingGuids.Length == 0)
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(settingGuids[0]);
            return AssetDatabase.LoadAssetAtPath<WSFrameSetting>(path);
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

            return TryGetRoute(config, routeId, out SceneTransitionRoute route)
                ? $"Route: {CreateRouteMenuPath(route)}"
                : $"Route: Missing ({routeId})";
        }

        // 判断配置中是否包含指定 RouteId。
        private static bool ContainsRoute(SceneTransitionConfig config, string routeId)
        {
            return TryGetRoute(config, routeId, out _);
        }

        // 从配置数据中按 RouteId 查找 Route。
        private static bool TryGetRoute(
            SceneTransitionConfig config,
            string routeId,
            out SceneTransitionRoute route)
        {
            IReadOnlyList<SceneTransitionRoute> routes = config.Routes;
            for (int i = 0; i < routes.Count; i++)
            {
                route = routes[i];
                if (route != null && route.RouteId == routeId)
                {
                    return true;
                }
            }

            route = null;
            return false;
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
