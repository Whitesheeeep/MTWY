using Cysharp.Threading.Tasks;
using UnityEngine;

namespace WS_Modules.SceneModule
{
    /// <summary>
    /// 2D 场景转换触发器，命中指定层后按 Route 执行场景转换。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class SceneTransitionTrigger2D : MonoBehaviour
    {
        [SerializeField] private LayerMask travelerLayerMask;
        [SerializeField] private SceneTransitionConfig transitionConfig;
        [SerializeField] private string routeId;

        private Collider2D triggerCollider;

        /// <summary>
        /// 场景转换 Route 配置资产。
        /// </summary>
        public SceneTransitionConfig TransitionConfig => transitionConfig;

        /// <summary>
        /// 当前触发器选择的 RouteId。
        /// </summary>
        public string RouteId => routeId;

        // 初始化触发器碰撞体引用。
        private void Awake()
        {
            ResolveTriggerCollider();
        }

        // 自动把碰撞体设置为 Trigger。
        private void Reset()
        {
            EnsureTriggerCollider();
        }

        // Inspector 修改后保持碰撞体为 Trigger。
        private void OnValidate()
        {
            EnsureTriggerCollider();
        }

        // 命中允许层后启动场景转换。
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || !IsTravelerLayer(other.gameObject.layer))
            {
                return;
            }

            Transform traveler = ResolveTraveler(other);
            StartTransitionAsync(traveler).Forget();
        }

        // 异步执行转场，并把异常输出到 Console。
        private async UniTaskVoid StartTransitionAsync(Transform traveler)
        {
            try
            {
                SceneTransitionRoute route = ResolveRoute();
                await SceneTransitionSystem.TransitionAsync(traveler, route);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        // 从配置中解析当前 RouteId 对应的 Route。
        private SceneTransitionRoute ResolveRoute()
        {
            if (transitionConfig == null)
            {
                throw new System.InvalidOperationException(
                    $"{nameof(SceneTransitionTrigger2D)} has no {nameof(SceneTransitionConfig)}.");
            }

            if (string.IsNullOrWhiteSpace(routeId))
            {
                throw new System.InvalidOperationException(
                    $"{nameof(SceneTransitionTrigger2D)} has no route id.");
            }

            if (!transitionConfig.TryGetRoute(routeId, out SceneTransitionRoute route))
            {
                throw new System.InvalidOperationException(
                    $"{nameof(SceneTransitionConfig)} does not contain route id '{routeId}'.");
            }

            return route;
        }

        // 判断碰撞对象是否位于允许转场的层。
        private bool IsTravelerLayer(int layer)
        {
            return (travelerLayerMask.value & (1 << layer)) != 0;
        }

        // 从 Collider2D 解析需要移动的 traveler Transform。
        private static Transform ResolveTraveler(Collider2D other)
        {
            return other.attachedRigidbody != null
                ? other.attachedRigidbody.transform
                : other.transform;
        }

        // 获取触发器碰撞体引用。
        private void ResolveTriggerCollider()
        {
            if (triggerCollider == null)
            {
                triggerCollider = GetComponent<Collider2D>();
            }
        }

        // 确保触发器碰撞体存在并设置为 Trigger。
        private void EnsureTriggerCollider()
        {
            ResolveTriggerCollider();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }
    }
}
