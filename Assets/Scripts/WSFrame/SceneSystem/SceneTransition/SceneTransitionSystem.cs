using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WS_Modules.SceneModule
{
    /// <summary>
    /// 基于 SceneTransitionRoute 的场景转换执行入口。
    /// </summary>
    public static class SceneTransitionSystem
    {
        private static readonly Dictionary<string, SceneTransitionRoute> routeMap =
            new Dictionary<string, SceneTransitionRoute>(StringComparer.Ordinal);

        private static bool isInitialized;
        private static bool hasConfig;

        /// <summary>
        /// 当前是否正在执行场景转换。
        /// </summary>
        public static bool IsTransitioning { get; private set; }

        /// <summary>
        /// 使用全局场景转换配置初始化运行时 Route 查找表。
        /// </summary>
        /// <param name="config">全局场景转换配置资产。</param>
        public static void Initialize(SceneTransitionConfig config)
        {
            routeMap.Clear();
            isInitialized = true;
            hasConfig = config != null;
            if (config == null)
            {
                return;
            }

            IReadOnlyList<SceneTransitionRoute> routes = config.Routes;
            for (int i = 0; i < routes.Count; i++)
            {
                SceneTransitionRoute route = routes[i];
                if (route == null || string.IsNullOrWhiteSpace(route.RouteId))
                {
                    continue;
                }

                if (routeMap.ContainsKey(route.RouteId))
                {
                    Debug.LogWarning(
                        $"{nameof(SceneTransitionConfig)} contains duplicate RouteId '{route.RouteId}'. The first route will be used.",
                        config);
                    continue;
                }

                routeMap.Add(route.RouteId, route);
            }
        }

        /// <summary>
        /// 根据 RouteId 异步切换场景，并把 traveler 移动到目标场景地点。
        /// </summary>
        /// <param name="traveler">需要移动到目标地点的对象。</param>
        /// <param name="routeId">全局场景转换配置中的 RouteId。</param>
        public static async UniTask TransitionAsync(Transform traveler, string routeId)
        {
            SceneTransitionRoute route = ResolveRoute(routeId);
            await TransitionAsync(traveler, route);
        }

        // 根据 Route 异步切换场景，并把 traveler 移动到目标场景地点。
        private static async UniTask TransitionAsync(Transform traveler, SceneTransitionRoute route)
        {
            ValidateTransitionRequest(traveler, route);
            if (IsTransitioning)
            {
                throw new InvalidOperationException("SceneTransitionSystem is already transitioning.");
            }

            if (SceneSystem.IsLoading)
            {
                throw new InvalidOperationException(
                    $"SceneSystem is already loading '{SceneSystem.CurrentLoadingTarget}'.");
            }

            IsTransitioning = true;
            try
            {
                await SceneSystem.LoadSceneAsync(route.TargetSceneName, mode: LoadSceneMode.Single);
                SceneSpawnRoot spawnRoot = FindSceneSpawnRoot();
                if (!spawnRoot.TryGetSpawnPoint(route.TargetSpawnId, out Transform spawnPoint))
                {
                    throw new InvalidOperationException(
                        $"SceneSpawnRoot in scene '{SceneSystem.CurrentSceneName}' does not contain TargetSpawnId '{route.TargetSpawnId}'.");
                }

                MoveTraveler(traveler, spawnPoint, route);
            }
            finally
            {
                IsTransitioning = false;
            }
        }

        // 从运行时查找表解析 RouteId 对应的 Route。
        private static SceneTransitionRoute ResolveRoute(string routeId)
        {
            if (!isInitialized)
            {
                throw new InvalidOperationException(
                    $"{nameof(SceneTransitionSystem)} has not been initialized.");
            }

            if (!hasConfig)
            {
                throw new InvalidOperationException(
                    $"{nameof(SceneTransitionSystem)} has no {nameof(SceneTransitionConfig)}.");
            }

            if (string.IsNullOrWhiteSpace(routeId))
            {
                throw new InvalidOperationException(
                    $"{nameof(SceneTransitionTrigger2D)} has no route id.");
            }

            if (!routeMap.TryGetValue(routeId, out SceneTransitionRoute route))
            {
                throw new InvalidOperationException(
                    $"{nameof(SceneTransitionConfig)} does not contain route id '{routeId}'.");
            }

            return route;
        }

        // 校验场景转换请求的必要参数。
        private static void ValidateTransitionRequest(Transform traveler, SceneTransitionRoute route)
        {
            if (traveler == null)
            {
                throw new ArgumentNullException(nameof(traveler));
            }

            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            if (string.IsNullOrWhiteSpace(route.TargetSceneName))
            {
                throw new ArgumentException("Route target scene name cannot be null, empty, or whitespace.", nameof(route));
            }

            if (string.IsNullOrWhiteSpace(route.TargetSpawnId))
            {
                throw new ArgumentException("Route target spawn id cannot be null, empty, or whitespace.", nameof(route));
            }
        }

        // 查找当前目标场景中的 SceneSpawnRoot。
        private static SceneSpawnRoot FindSceneSpawnRoot()
        {
            SceneSpawnRoot[] roots = UnityEngine.Object.FindObjectsOfType<SceneSpawnRoot>(true);
            if (roots.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Scene '{SceneSystem.CurrentSceneName}' does not contain a {nameof(SceneSpawnRoot)}.");
            }

            if (roots.Length > 1)
            {
                Debug.LogWarning(
                    $"Scene '{SceneSystem.CurrentSceneName}' contains multiple {nameof(SceneSpawnRoot)} components. The first one will be used.",
                    roots[0]);
            }

            return roots[0];
        }

        // 将 traveler 移动到目标出生点，并按 Route 设置处理 Rigidbody2D。
        private static void MoveTraveler(
            Transform traveler,
            Transform spawnPoint,
            SceneTransitionRoute route)
        {
            Rigidbody2D rigidbody2D = traveler.GetComponent<Rigidbody2D>();
            if (rigidbody2D != null)
            {
                if (route.ResetRigidbodyVelocity)
                {
                    rigidbody2D.velocity = Vector2.zero;
                    rigidbody2D.angularVelocity = 0f;
                }

                rigidbody2D.position = spawnPoint.position;
                if (route.ApplySpawnRotation)
                {
                    rigidbody2D.rotation = spawnPoint.eulerAngles.z;
                }

                return;
            }

            traveler.position = spawnPoint.position;
            if (route.ApplySpawnRotation)
            {
                traveler.rotation = spawnPoint.rotation;
            }
        }
    }
}
