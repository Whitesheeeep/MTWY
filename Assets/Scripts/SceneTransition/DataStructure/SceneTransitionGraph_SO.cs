using System.Collections.Generic;
using UnityEngine;

namespace GameData.SceneTransition
{
    [CreateAssetMenu(fileName = "SceneTransitionGraph", menuName = "GameData/Scene/Transition Graph")]
    public sealed class SceneTransitionGraph_SO : ScriptableObject
    {
        public List<SceneTransitionEdge> edges = new List<SceneTransitionEdge>();

        private void OnValidate()
        {
            RefreshGeneratedEdgeIds();
        }

        public void RefreshGeneratedEdgeIds()
        {
            if (edges == null)
            {
                return;
            }

            for (int i = 0; i < edges.Count; i++)
            {
                SceneTransitionEdge edge = edges[i];
                if (!TryCreateEdgeId(edge.fromPoint, edge.toPoint, out string edgeId))
                {
                    edges[i] = edge;
                    continue;
                }

                edge.edgeId = edgeId;

                edges[i] = edge;
            }
        }

        public static bool TryCreateEdgeId(
            SceneTransitionPoint_SO fromPoint,
            SceneTransitionPoint_SO toPoint,
            out string edgeId)
        {
            edgeId = string.Empty;
            if (fromPoint == null || toPoint == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(fromPoint.displayName) ||
                string.IsNullOrWhiteSpace(toPoint.displayName))
            {
                return false;
            }

            edgeId = $"{fromPoint.displayName}_to_{toPoint.displayName}";
            return true;
        }
    }
}
