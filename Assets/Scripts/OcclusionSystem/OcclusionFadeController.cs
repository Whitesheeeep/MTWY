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
        private readonly HashSet<OcclusionTrigger2D> activeTriggers = new();
        private SpriteOcclusionFadeView fadeView;
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
            activeTriggers.Clear();
            fadeView?.ResetImmediate();
        }

        private void Enter(OcclusionTrigger2D trigger)
        {
            if (trigger == null)
            {
                return;
            }

            bool wasEmpty = !HasActiveContacts();
            activeTriggers.Add(trigger);

            if (wasEmpty && HasActiveContacts())
            {
                fadeView?.FadeToConfiguredAlpha();
            }
        }

        private void Exit(OcclusionTrigger2D trigger)
        {
            if (trigger == null)
            {
                return;
            }

            activeTriggers.Remove(trigger);

            if (!HasActiveContacts())
            {
                fadeView?.Restore();
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
            return activeTriggers.Count > 0;
        }

        private int GetActiveContactCount()
        {
            return activeTriggers.Count;
        }

        #if UNITY_EDITOR
        [Button]
        private void Debug()
        {
            WSLog.Log($"Active Triggers: {GetActiveContactCount()}");
        }
        #endif
    }
}
