using System.Collections;
using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using WS_Modules.CustomEventSystem;
using WS_Modules.SceneModule;

namespace WS_Modules.CameraSystem
{
    [DisallowMultipleComponent]
    public sealed class CinemachineConfiner2DBinder : MonoBehaviour
    {
        [SerializeField] private CinemachineConfiner confiner;
        [SerializeField] private string persistenceSceneName = "PersistenceScene";
        [SerializeField] private string boundsTag = "BoundPrefiner";
        [SerializeField] private string boundsObjectName = "Bounds";
        [SerializeField] private bool bindOnStart = true;
        [SerializeField] private bool clearWhenNotFound;

        private Coroutine bindCoroutine;
        private IUnRegister sceneLoadSucceededUnregister;

        private void Awake()
        {
            if (confiner == null)
            {
                confiner = GetComponent<CinemachineConfiner>();
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            sceneLoadSucceededUnregister = SceneSystem.RegisterLoadSucceeded(OnSceneLoadSucceeded);
        }

        private void Start()
        {
            if (bindOnStart)
            {
                QueueBind();
            }
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            sceneLoadSucceededUnregister?.UnRegister();
            sceneLoadSucceededUnregister = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            QueueBind();
        }

        private void OnSceneLoadSucceeded(SceneLoadSucceededEventArgs _)
        {
            QueueBind();
        }

        private void QueueBind()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (bindCoroutine != null)
            {
                StopCoroutine(bindCoroutine);
            }

            bindCoroutine = StartCoroutine(BindNextFrame());
        }

        private IEnumerator BindNextFrame()
        {
            yield return null;
            bindCoroutine = null;
            BindNow();
        }

        [ContextMenu("Bind Confiner Bounds")]
        public void BindNow()
        {
            if (confiner == null)
            {
                confiner = GetComponent<CinemachineConfiner>();
            }

            if (confiner == null)
            {
                Debug.LogWarning($"{nameof(CinemachineConfiner2DBinder)} requires a CinemachineConfiner.", this);
                return;
            }

            Collider2D bounds = FindBoundsCollider();
            if (bounds == null)
            {
                if (clearWhenNotFound)
                {
                    confiner.m_BoundingShape2D = null;
                    confiner.InvalidatePathCache();
                }

                Debug.LogWarning(
                    $"{nameof(CinemachineConfiner2DBinder)} could not find a Collider2D using tag '{boundsTag}' or name '{boundsObjectName}'.",
                    this);
                return;
            }

            if (confiner.m_BoundingShape2D == bounds)
            {
                return;
            }

            confiner.m_BoundingShape2D = bounds;
            confiner.InvalidatePathCache();
        }

        private Collider2D FindBoundsCollider()
        {
            Collider2D byTag = FindCollider(MatchesBoundsTag);
            if (byTag != null)
            {
                return byTag;
            }

            return FindCollider(MatchesBoundsName);
        }

        private Collider2D FindCollider(System.Func<GameObject, bool> predicate)
        {
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || IsPersistenceScene(scene))
                {
                    continue;
                }

                GameObject[] roots = scene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    Collider2D result = FindColliderInHierarchy(roots[rootIndex], predicate);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        private Collider2D FindColliderInHierarchy(GameObject root, System.Func<GameObject, bool> predicate)
        {
            Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                GameObject candidate = colliders[i].gameObject;
                if (predicate(candidate))
                {
                    return colliders[i];
                }
            }

            return null;
        }

        private bool MatchesBoundsTag(GameObject candidate)
        {
            if (string.IsNullOrWhiteSpace(boundsTag))
            {
                return false;
            }

            try
            {
                return candidate.CompareTag(boundsTag);
            }
            catch (UnityException)
            {
                return false;
            }
        }

        private bool MatchesBoundsName(GameObject candidate)
        {
            return !string.IsNullOrWhiteSpace(boundsObjectName) && candidate.name == boundsObjectName;
        }

        private bool IsPersistenceScene(Scene scene)
        {
            return !string.IsNullOrWhiteSpace(persistenceSceneName) && scene.name == persistenceSceneName;
        }
    }
}
