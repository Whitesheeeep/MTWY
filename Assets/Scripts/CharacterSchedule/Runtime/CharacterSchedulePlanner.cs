using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameData.SceneTransition;
using Pathfinding;
using UnityEngine;

namespace GameData.CharacterSchedule
{
    /// <summary>
    /// 角色日程路径规划器。
    /// 第一版同地图走 MapPathfindingService，跨地图只支持一条直接 SceneTransition 边。
    /// </summary>
    public static class CharacterSchedulePlanner
    {
        /// <summary>
        /// 规划角色从当前位置到目标地图目标格的路径。
        /// 跨地图时返回当前地图内需要可见执行的这一段路径。
        /// </summary>
        public static async UniTask<CharacterSchedulePlanResult> PlanAsync(
            string fromMapId,
            Vector3Int fromCell,
            string targetMapId,
            Vector3Int targetCell)
        {
            if (string.IsNullOrWhiteSpace(fromMapId))
            {
                return CharacterSchedulePlanResult.Failed("Source mapId is empty.");
            }

            if (string.IsNullOrWhiteSpace(targetMapId))
            {
                return CharacterSchedulePlanResult.Failed("Target mapId is empty.");
            }

            if (string.Equals(fromMapId, targetMapId, System.StringComparison.Ordinal))
            {
                var path = new List<Vector3Int>();
                bool found = await MapPathfindingService.TryFindPathAsync(fromMapId, fromCell, targetCell, path);
                return found
                    ? CharacterSchedulePlanResult.SameMap(fromMapId, fromCell, targetCell, path)
                    : CharacterSchedulePlanResult.Failed($"No path on map '{fromMapId}' from {fromCell} to {targetCell}.");
            }

            SceneTransitionEdge? edge = FindDirectEdge(fromMapId, targetMapId);
            if (!edge.HasValue)
            {
                return CharacterSchedulePlanResult.Failed($"No direct transition edge from '{fromMapId}' to '{targetMapId}'.");
            }

            var currentMapPath = new List<Vector3Int>();
            bool currentSegmentFound = await MapPathfindingService.TryFindPathAsync(
                fromMapId,
                fromCell,
                edge.Value.fromPoint.cell,
                currentMapPath);

            if (!currentSegmentFound)
            {
                return CharacterSchedulePlanResult.Failed($"No path from {fromCell} to transition point {edge.Value.fromPoint.cell} on map '{fromMapId}'.");
            }

            var targetMapPath = new List<Vector3Int>();
            bool targetSegmentFound = await MapPathfindingService.TryFindPathAsync(
                targetMapId,
                edge.Value.toPoint.cell,
                targetCell,
                targetMapPath);

            if (!targetSegmentFound)
            {
                return CharacterSchedulePlanResult.Failed($"No path from transition point {edge.Value.toPoint.cell} to {targetCell} on map '{targetMapId}'.");
            }

            CharacterMoveSegment currentSegment = CharacterSchedulePlanResult.CreateSegment(
                fromMapId,
                fromCell,
                edge.Value.fromPoint.cell,
                currentMapPath,
                null,
                edge.Value.edgeId);

            CharacterMoveSegment targetSegment = CharacterSchedulePlanResult.CreateSegment(
                targetMapId,
                edge.Value.toPoint.cell,
                targetCell,
                targetMapPath,
                edge.Value.edgeId,
                null);

            return CharacterSchedulePlanResult.CrossMap(currentSegment, targetSegment);
        }

        private static SceneTransitionEdge? FindDirectEdge(string fromMapId, string targetMapId)
        {
            foreach (SceneTransitionEdge edge in SceneTransitionSystem.GetEdgesFromScene(fromMapId))
            {
                if (edge.toPoint != null &&
                    string.Equals(edge.toPoint.sceneName, targetMapId, System.StringComparison.Ordinal))
                {
                    return edge;
                }
            }

            return null;
        }
    }
}
