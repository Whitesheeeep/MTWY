using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameData.SceneTransition
{
    /// <summary>
    /// 2D trigger that starts a configured scene transition edge for allowed traveler layers.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class SceneTransitionTrigger2D : MonoBehaviour
    {
        [SerializeField] private LayerMask travelerLayerMask;
        [SerializeField, FormerlySerializedAs("routeId")] private string edgeId;

        private Collider2D triggerCollider;

        public string EdgeId => edgeId;

        private void Awake()
        {
            ResolveTriggerCollider();
        }

        private void Reset()
        {
            EnsureTriggerCollider();
        }

        private void OnValidate()
        {
            EnsureTriggerCollider();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || !IsTravelerLayer(other.gameObject.layer))
            {
                return;
            }

            Transform traveler = ResolveTraveler(other);
            StartTransitionAsync(traveler).Forget();
        }

        private async UniTaskVoid StartTransitionAsync(Transform traveler)
        {
            try
            {
                await SceneTransitionSystem.TransitionAsync(traveler, edgeId);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private bool IsTravelerLayer(int layer)
        {
            return (travelerLayerMask.value & (1 << layer)) != 0;
        }

        private static Transform ResolveTraveler(Collider2D other)
        {
            return other.attachedRigidbody != null
                ? other.attachedRigidbody.transform
                : other.transform;
        }

        private void ResolveTriggerCollider()
        {
            if (triggerCollider == null)
            {
                triggerCollider = GetComponent<Collider2D>();
            }
        }

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
