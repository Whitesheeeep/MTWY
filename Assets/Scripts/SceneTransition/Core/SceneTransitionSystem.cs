using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameData;
using UnityEngine;
using UnityEngine.SceneManagement;
using WS_Modules.SceneModule;

namespace GameData.SceneTransition
{
    /// <summary>
    /// Game-layer scene transition service. It resolves configured edges, switches scenes,
    /// and places the traveler through the current MapGrid.
    /// </summary>
    public static class SceneTransitionSystem
    {
        private const int MaxGridReadyWaitFrames = 120;

        private static readonly Dictionary<string, SceneTransitionEdge> edgeMap =
            new Dictionary<string, SceneTransitionEdge>(StringComparer.Ordinal);

        private static bool isInitialized;
        private static bool hasGraph;

        public static bool IsTransitioning { get; private set; }

        public static void Initialize(SceneTransitionGraph_SO graph)
        {
            edgeMap.Clear();
            isInitialized = true;
            hasGraph = graph != null;

            if (graph == null)
            {
                Debug.LogWarning("[SceneTransitionSystem] Initialize called without a SceneTransitionGraph_SO. Transitions will fail until a graph is registered.");
                return;
            }

            List<SceneTransitionEdge> edges = graph.edges;
            if (edges == null)
            {
                return;
            }

            graph.RefreshGeneratedEdgeIds();
            for (int i = 0; i < edges.Count; i++)
            {
                SceneTransitionEdge edge = edges[i];
                if (!ValidateEdgeForRegistration(edge, i, graph))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(edge.edgeId))
                {
                    Debug.LogWarning($"[SceneTransitionSystem] Edge at index {i} has an empty edgeId.", graph);
                    continue;
                }

                if (edgeMap.ContainsKey(edge.edgeId))
                {
                    Debug.LogWarning($"[SceneTransitionSystem] Duplicate edgeId '{edge.edgeId}' found. The first edge will be used.", graph);
                    continue;
                }

                edgeMap.Add(edge.edgeId, edge);
            }
        }

        public static bool TryGetEdge(string edgeId, out SceneTransitionEdge edge)
        {
            edge = default;
            return !string.IsNullOrWhiteSpace(edgeId) && edgeMap.TryGetValue(edgeId, out edge);
        }

        public static IEnumerable<SceneTransitionEdge> GetEdgesFromScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                yield break;
            }

            foreach (SceneTransitionEdge edge in edgeMap.Values)
            {
                if (edge.fromPoint != null &&
                    string.Equals(edge.fromPoint.sceneName, sceneName, StringComparison.Ordinal))
                {
                    yield return edge;
                }
            }
        }

        public static async UniTask TransitionAsync(Transform traveler, string edgeId)
        {
            SceneTransitionEdge edge = ResolveEdge(edgeId);
            ValidateTransitionRequest(traveler, edge);

            if (IsTransitioning)
            {
                throw new InvalidOperationException("SceneTransitionSystem is already transitioning.");
            }

            if (SceneSystem.IsLoading)
            {
                throw new InvalidOperationException($"SceneSystem is already loading '{SceneSystem.CurrentLoadingTarget}'.");
            }

            IsTransitioning = true;
            try
            {
                string currentSceneName = SceneSystem.CurrentSceneName;
                if (!string.IsNullOrWhiteSpace(currentSceneName))
                {
                    await SceneSystem.UnloadSceneAsync(currentSceneName);
                }

                await SceneSystem.LoadSceneAsync(edge.toPoint.sceneName, mode: LoadSceneMode.Additive);
                SceneSystem.SetActiveScene(edge.toPoint.sceneName);

                await WaitForTargetGridAsync(edge);
                MoveTraveler(traveler, edge);
            }
            finally
            {
                IsTransitioning = false;
            }
        }

        private static SceneTransitionEdge ResolveEdge(string edgeId)
        {
            if (!isInitialized)
            {
                throw new InvalidOperationException("SceneTransitionSystem has not been initialized.");
            }

            if (!hasGraph)
            {
                throw new InvalidOperationException("SceneTransitionSystem has no SceneTransitionGraph_SO.");
            }

            if (string.IsNullOrWhiteSpace(edgeId))
            {
                throw new InvalidOperationException("SceneTransitionTrigger2D has no edge id.");
            }

            if (!edgeMap.TryGetValue(edgeId, out SceneTransitionEdge edge))
            {
                throw new InvalidOperationException($"SceneTransitionGraph_SO does not contain edge id '{edgeId}'.");
            }

            return edge;
        }

        private static void ValidateTransitionRequest(Transform traveler, SceneTransitionEdge edge)
        {
            if (traveler == null)
            {
                throw new ArgumentNullException(nameof(traveler));
            }

            if (edge.fromPoint == null)
            {
                throw new InvalidOperationException($"SceneTransitionEdge '{edge.edgeId}' has no fromPoint.");
            }

            if (edge.toPoint == null)
            {
                throw new InvalidOperationException($"SceneTransitionEdge '{edge.edgeId}' has no toPoint.");
            }

            if (string.IsNullOrWhiteSpace(edge.toPoint.sceneName))
            {
                throw new InvalidOperationException($"SceneTransitionEdge '{edge.edgeId}' target point has no sceneName.");
            }
        }

        private static async UniTask WaitForTargetGridAsync(SceneTransitionEdge edge)
        {
            for (int i = 0; i < MaxGridReadyWaitFrames; i++)
            {
                MapGridManager mapGridManager = MapGridManager.Instance;
                if (mapGridManager.HasCurrentGrid &&
                    string.Equals(mapGridManager.CurrentMapId, edge.toPoint.sceneName, StringComparison.Ordinal))
                {
                    return;
                }

                await UniTask.Yield();
            }

            throw new InvalidOperationException(
                $"SceneTransitionSystem could not resolve MapGrid for target scene '{edge.toPoint.sceneName}'. " +
                "Ensure the target scene has a MapGridRuntimeLoader with MapGridData_SO and Grid assigned.");
        }

        private static void MoveTraveler(Transform traveler, SceneTransitionEdge edge)
        {
            Vector3 targetPosition = MapGridManager.Instance.GetCellCenterWorld(edge.toPoint.cell) + edge.toPoint.worldOffset;
            traveler.position = targetPosition;

            if (edge.applySpawnRotation)
            {
                traveler.rotation = Quaternion.Euler(edge.spawnEulerAngles);
            }

            if (edge.resetRigidbodyVelocity && traveler.TryGetComponent(out Rigidbody2D rigidbody2D))
            {
                rigidbody2D.velocity = Vector2.zero;
                rigidbody2D.angularVelocity = 0f;
            }
        }

        private static bool ValidateEdgeForRegistration(SceneTransitionEdge edge, int index, UnityEngine.Object context)
        {
            if (edge.fromPoint == null)
            {
                Debug.LogWarning($"[SceneTransitionSystem] Edge at index {index} has no fromPoint.", context);
                return false;
            }

            if (edge.toPoint == null)
            {
                Debug.LogWarning($"[SceneTransitionSystem] Edge at index {index} has no toPoint.", context);
                return false;
            }

            if (string.IsNullOrWhiteSpace(edge.fromPoint.displayName))
            {
                Debug.LogWarning($"[SceneTransitionSystem] Edge at index {index} has a fromPoint with empty displayName.", context);
                return false;
            }

            if (string.IsNullOrWhiteSpace(edge.toPoint.displayName))
            {
                Debug.LogWarning($"[SceneTransitionSystem] Edge at index {index} has a toPoint with empty displayName.", context);
                return false;
            }

            if (string.IsNullOrWhiteSpace(edge.fromPoint.sceneName))
            {
                Debug.LogWarning($"[SceneTransitionSystem] Edge '{edge.edgeId}' fromPoint has empty sceneName.", context);
                return false;
            }

            if (string.IsNullOrWhiteSpace(edge.toPoint.sceneName))
            {
                Debug.LogWarning($"[SceneTransitionSystem] Edge '{edge.edgeId}' toPoint has empty sceneName.", context);
                return false;
            }

            return true;
        }
    }
}
