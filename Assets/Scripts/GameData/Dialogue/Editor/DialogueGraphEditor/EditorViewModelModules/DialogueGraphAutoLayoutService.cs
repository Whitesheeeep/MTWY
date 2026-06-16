using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameData.Editor
{
    /// <summary>
    /// 对话图自动排版服务，只负责根据连接关系计算节点层级。
    /// </summary>
    internal sealed class DialogueGraphAutoLayoutService
    {
        #region 层级计算
        /// <summary>
        /// 根据图连接关系计算自动排版层级。
        /// </summary>
        public Dictionary<DialogueNode, int> CalculateLayers(
            DialogueGraph_SO graph,
            DialogueGraphConnectionService connectionService,
            out bool stoppedEarly)
        {
            stoppedEarly = false;

            if (graph == null || graph.StartNode == null)
            {
                return new Dictionary<DialogueNode, int>();
            }

            IReadOnlyList<DialogueNode> allNodes = graph.EnumerateNodes().ToList();
            Dictionary<DialogueNode, int> layerByNode = new();
            Dictionary<DialogueNode, HashSet<DialogueNode>> pathByNode = new();
            Queue<DialogueNode> queue = new();

            layerByNode[graph.StartNode] = 0;
            pathByNode[graph.StartNode] = new HashSet<DialogueNode> { graph.StartNode };
            queue.Enqueue(graph.StartNode);

            int iterationLimit = Mathf.Max(1, allNodes.Count * allNodes.Count);
            int iterations = 0;

            while (queue.Count > 0 && iterations < iterationLimit)
            {
                iterations++;
                DialogueNode current = queue.Dequeue();
                int currentLayer = layerByNode[current];

                foreach (DialogueNode target in connectionService.GetTargetsFrom(current))
                {
                    if (target == null || !allNodes.Contains(target))
                    {
                        continue;
                    }

                    if (pathByNode.TryGetValue(current, out HashSet<DialogueNode> currentPath) && currentPath.Contains(target))
                    {
                        continue;
                    }

                    int targetLayer = currentLayer + 1;
                    if (!layerByNode.TryGetValue(target, out int existingLayer) || targetLayer > existingLayer)
                    {
                        layerByNode[target] = targetLayer;
                        pathByNode[target] = currentPath != null
                            ? new HashSet<DialogueNode>(currentPath) { target }
                            : new HashSet<DialogueNode> { target };
                        queue.Enqueue(target);
                    }
                }
            }

            stoppedEarly = iterations >= iterationLimit;
            return layerByNode;
        }
        #endregion
    }
}
