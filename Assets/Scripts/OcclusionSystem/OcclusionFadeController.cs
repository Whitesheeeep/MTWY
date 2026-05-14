#if UNITY_EDITOR
using Sirenix.OdinInspector;
using WS_Modules.LogModule;
#endif
using System.Collections.Generic;
using UnityEngine;

namespace OcclusionSystem
{
    [DisallowMultipleComponent]
    public sealed class OcclusionFadeController : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float targetAlpha = 0.45f;
        [SerializeField, Min(0f)] private float fadeDuration = 0.2f;

        private readonly Dictionary<Collider2D, HashSet<Collider2D>> activeContacts = new();
        private IOcclusionFadeView fadeView;
        private OcclusionTrigger2D[] triggers;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void OnEnable()
        {
            ResolveDependencies();
            RegisterTriggerEvents();
        }

        private void OnDisable()
        {
            UnregisterTriggerEvents();
            activeContacts.Clear();
            fadeView?.ResetImmediate();
        }

        private void Enter(Collider2D playerCollider, Collider2D triggerCollider)
        {
            if (playerCollider == null || triggerCollider == null)
            {
                return;
            }

            bool wasEmpty = !HasActiveContacts();
            if (!activeContacts.TryGetValue(playerCollider, out HashSet<Collider2D> triggerColliders))
            {
                triggerColliders = new HashSet<Collider2D>();
                activeContacts.Add(playerCollider, triggerColliders);
            }

            triggerColliders.Add(triggerCollider);

            if (wasEmpty && HasActiveContacts())
            {
                fadeView?.FadeTo(targetAlpha, fadeDuration);
            }
        }

        private void Exit(Collider2D playerCollider, Collider2D triggerCollider)
        {
            if (playerCollider == null || triggerCollider == null)
            {
                return;
            }

            if (!activeContacts.TryGetValue(playerCollider, out HashSet<Collider2D> triggerColliders))
            {
                return;
            }

            triggerColliders.Remove(triggerCollider);
            if (triggerColliders.Count == 0)
            {
                activeContacts.Remove(playerCollider);
            }

            if (!HasActiveContacts())
            {
                fadeView?.Restore(fadeDuration);
            }
        }

        private void ResolveDependencies()
        {
            fadeView = GetComponentInChildren<SpriteOcclusionFadeView>(true);
            triggers = GetComponentsInChildren<OcclusionTrigger2D>(true);
        }

        private void RegisterTriggerEvents()
        {
            if (triggers == null)
            {
                return;
            }

            UnregisterTriggerEvents();
            for (int i = 0; i < triggers.Length; i++)
            {
                if (triggers[i] == null)
                {
                    continue;
                }

                triggers[i].PlayerEntered += Enter;
                triggers[i].PlayerExited += Exit;
            }
        }

        private void UnregisterTriggerEvents()
        {
            if (triggers == null)
            {
                return;
            }

            for (int i = 0; i < triggers.Length; i++)
            {
                if (triggers[i] == null)
                {
                    continue;
                }

                triggers[i].PlayerEntered -= Enter;
                triggers[i].PlayerExited -= Exit;
            }
        }

        private bool HasActiveContacts()
        {
            return activeContacts.Count > 0;
        }

        private int GetActiveContactCount()
        {
            int count = 0;
            foreach (HashSet<Collider2D> triggerColliders in activeContacts.Values)
            {
                count += triggerColliders.Count;
            }

            return count;
        }

        #if UNITY_EDITOR
        [Button]
        private void Debug()
        {
            WSLog.Log($"Active Player Colliders: {activeContacts.Count}, Active Contacts: {GetActiveContactCount()}");
        }
        #endif
    }
}
