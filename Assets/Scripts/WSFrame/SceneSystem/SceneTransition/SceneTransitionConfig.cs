using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using WS_Modules;

namespace WS_Modules.SceneModule
{
    /// <summary>
    /// 场景转换 Route 配置资产，描述从触发器去往哪个场景和哪个目标地点。
    /// </summary>
    [CreateAssetMenu(fileName = "SceneTransitionConfig", menuName = "WSFrame/SceneSystem/SceneTransitionConfig", order = 0)]
    public sealed class SceneTransitionConfig : ScriptableObject
    {
        [SerializeField]
        [LabelText("Routes")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
        private List<SceneTransitionRoute> routes = new List<SceneTransitionRoute>();

        private readonly Dictionary<string, SceneTransitionRoute> routeMap =
            new Dictionary<string, SceneTransitionRoute>(StringComparer.Ordinal);

        private bool mapDirty = true;

        /// <summary>
        /// 当前配置的场景转换 Route 列表。
        /// </summary>
        public IReadOnlyList<SceneTransitionRoute> Routes => routes;

        /// <summary>
        /// 尝试通过 RouteId 获取场景转换 Route。
        /// </summary>
        /// <param name="routeId">唯一 RouteId。</param>
        /// <param name="route">匹配到的场景转换 Route。</param>
        /// <returns>如果找到匹配 Route，则返回 true。</returns>
        public bool TryGetRoute(string routeId, out SceneTransitionRoute route)
        {
            EnsureRouteMap();
            return routeMap.TryGetValue(routeId, out route);
        }

        // 标记 Route 查找表需要重建。
        private void OnValidate()
        {
            mapDirty = true;
            ValidateRoutes();
        }

        // 标记 Route 查找表需要在运行时第一次查询前构建。
        private void OnEnable()
        {
            mapDirty = true;
        }

        // 手动校验 Route 配置，便于 Odin Inspector 中主动检查。
        [Button("Validate Routes")]
        private void ValidateRoutes()
        {
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < routes.Count; i++)
            {
                SceneTransitionRoute route = routes[i];
                if (route == null)
                {
                    Debug.LogWarning($"{nameof(SceneTransitionConfig)} has a null route at index {i}.", this);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(route.RouteId))
                {
                    Debug.LogWarning($"{nameof(SceneTransitionConfig)} has an empty RouteId at index {i}.", this);
                }
                else if (!seenIds.Add(route.RouteId))
                {
                    Debug.LogWarning(
                        $"{nameof(SceneTransitionConfig)} has duplicate RouteId '{route.RouteId}'.",
                        this);
                }

                if (string.IsNullOrWhiteSpace(route.TargetSceneName))
                {
                    Debug.LogWarning(
                        $"{nameof(SceneTransitionConfig)} route '{route.RouteId}' has no target scene.",
                        this);
                }

                if (string.IsNullOrWhiteSpace(route.TargetSpawnId))
                {
                    Debug.LogWarning(
                        $"{nameof(SceneTransitionConfig)} route '{route.RouteId}' has no target spawn id.",
                        this);
                }
            }
        }

        // 确保 Route 查找表已经按当前配置构建。
        private void EnsureRouteMap()
        {
            if (!mapDirty)
            {
                return;
            }

            routeMap.Clear();
            for (int i = 0; i < routes.Count; i++)
            {
                SceneTransitionRoute route = routes[i];
                if (route == null || string.IsNullOrWhiteSpace(route.RouteId))
                {
                    continue;
                }

                if (!routeMap.ContainsKey(route.RouteId))
                {
                    routeMap.Add(route.RouteId, route);
                }
            }

            mapDirty = false;
        }
    }

    /// <summary>
    /// 描述一条场景转换 Route 的配置数据。
    /// </summary>
    [Serializable]
    public sealed class SceneTransitionRoute
    {
        [SerializeField]
        [LabelText("Route Id")]
        private string routeId;

        [SerializeField]
        [LabelText("Display Name")]
        private string displayName;

        [SerializeField]
        [LabelText("Target Scene")]
        [WSScene]
        private string targetSceneName;

        [SerializeField]
        [LabelText("Target Spawn Id")]
        private string targetSpawnId;

        [SerializeField]
        [LabelText("Reset Rigidbody Velocity")]
        private bool resetRigidbodyVelocity = true;

        [SerializeField]
        [LabelText("Apply Spawn Rotation")]
        private bool applySpawnRotation;

        /// <summary>
        /// 唯一 RouteId。
        /// </summary>
        public string RouteId => routeId;

        /// <summary>
        /// Inspector 中展示的 Route 名称。
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// Route 指向的目标场景名称。
        /// </summary>
        public string TargetSceneName => targetSceneName;

        /// <summary>
        /// Route 指向的目标地点 Id。
        /// </summary>
        public string TargetSpawnId => targetSpawnId;

        /// <summary>
        /// 转场落位时是否清空 Rigidbody2D 速度。
        /// </summary>
        public bool ResetRigidbodyVelocity => resetRigidbodyVelocity;

        /// <summary>
        /// 转场落位时是否应用出生点旋转。
        /// </summary>
        public bool ApplySpawnRotation => applySpawnRotation;
    }
}
