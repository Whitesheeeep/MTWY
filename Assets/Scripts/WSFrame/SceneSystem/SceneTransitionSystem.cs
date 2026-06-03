using System;
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
        /// <summary>
        /// 当前是否正在执行场景转换。
        /// </summary>
        public static bool IsTransitioning { get; private set; }

        /// <summary>
        /// 根据 Route 异步切换场景，并把 traveler 移动到目标场景地点。
        /// </summary>
        /// <param name="traveler">需要移动到目标地点的对象。</param>
        /// <param name="route">场景转换 Route 配置。</param>
        public static async UniTask TransitionAsync(Transform traveler, SceneTransitionRoute route)
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
