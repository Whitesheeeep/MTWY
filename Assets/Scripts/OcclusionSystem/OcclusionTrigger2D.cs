using System;
using UnityEngine;

namespace OcclusionSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class OcclusionTrigger2D : MonoBehaviour
    {
        [SerializeField] private LayerMask playerLayerMask;

        public event Action<Collider2D, Collider2D> PlayerEntered;
        public event Action<Collider2D, Collider2D> PlayerExited;

        private Collider2D triggerCollider;

        private void Awake()
        {
            ResolveTriggerCollider();
        }

        private void Reset()
        {
            ResolveTriggerCollider();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        private void OnValidate()
        {
            ResolveTriggerCollider();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayerLayer(other.gameObject.layer))
            {
                return;
            }

            ResolveTriggerCollider();
            PlayerEntered?.Invoke(other, triggerCollider);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayerLayer(other.gameObject.layer))
            {
                return;
            }

            ResolveTriggerCollider();
            PlayerExited?.Invoke(other, triggerCollider);
        }

        private bool IsPlayerLayer(int layer)
        {
            return (playerLayerMask.value & (1 << layer)) != 0;
        }

        private void ResolveTriggerCollider()
        {
            if (triggerCollider != null)
            {
                return;
            }

            triggerCollider = GetComponent<Collider2D>();
        }
    }
}
