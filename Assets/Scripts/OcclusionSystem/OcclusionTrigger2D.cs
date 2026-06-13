using System;
using System.Collections.Generic;
using UnityEngine;

namespace OcclusionSystem
{
    [DisallowMultipleComponent]
    public sealed class OcclusionTrigger2D : MonoBehaviour
    {
        [SerializeField] private LayerMask playerLayerMask;

        public event Action<OcclusionTrigger2D> PlayerEntered;
        public event Action<OcclusionTrigger2D> PlayerExited;

        private readonly List<Collider2D> triggerColliders = new();
        private int activeOverlapCount;

        private void Awake()
        {
            ResolveTriggerColliders();
        }

        private void Reset()
        {
            ResolveTriggerColliders();
            SetAllCollidersAsTriggers();
        }

        private void OnValidate()
        {
            ResolveTriggerColliders();
            SetAllCollidersAsTriggers();
        }

        private void OnDisable()
        {
            if (activeOverlapCount > 0)
            {
                PlayerExited?.Invoke(this);
            }

            activeOverlapCount = 0;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayerLayer(other.gameObject.layer))
            {
                return;
            }

            if (activeOverlapCount == 0)
            {
                PlayerEntered?.Invoke(this);
            }

            activeOverlapCount++;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayerLayer(other.gameObject.layer))
            {
                return;
            }

            if (activeOverlapCount <= 0)
            {
                return;
            }

            activeOverlapCount--;
            if (activeOverlapCount == 0)
            {
                PlayerExited?.Invoke(this);
            }
        }

        private bool IsPlayerLayer(int layer)
        {
            return (playerLayerMask.value & (1 << layer)) != 0;
        }

        private void ResolveTriggerColliders()
        {
            triggerColliders.Clear();
            GetComponents(triggerColliders);
            triggerColliders.RemoveAll(collider => collider == null);
        }

        private void SetAllCollidersAsTriggers()
        {
            foreach (var collider2D in triggerColliders)
            {
                collider2D.isTrigger = true;
            }
        }
    }
}
