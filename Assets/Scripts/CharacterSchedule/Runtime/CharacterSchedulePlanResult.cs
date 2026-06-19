using System.Collections.Generic;
using UnityEngine;

namespace GameData.CharacterSchedule
{
    /// <summary>
    /// Result of schedule path planning. Cross-map movement is represented as map-local segments.
    /// </summary>
    public sealed class CharacterSchedulePlanResult
    {
        public bool Success { get; private set; }
        public bool IsCrossMap { get; private set; }
        public string FailureReason { get; private set; }
        public List<CharacterMoveSegment> Segments { get; } = new List<CharacterMoveSegment>();
        public CharacterMoveSegment FirstSegment => Segments.Count > 0 ? Segments[0] : null;

        public static CharacterSchedulePlanResult Failed(string reason)
        {
            return new CharacterSchedulePlanResult
            {
                Success = false,
                FailureReason = reason
            };
        }

        public static CharacterSchedulePlanResult SameMap(
            string mapId,
            Vector3Int startCell,
            Vector3Int targetCell,
            List<Vector3Int> path)
        {
            var result = new CharacterSchedulePlanResult
            {
                Success = true,
                IsCrossMap = false
            };

            result.Segments.Add(CreateSegment(mapId, startCell, targetCell, path, null, null));
            return result;
        }

        public static CharacterSchedulePlanResult CrossMap(
            CharacterMoveSegment currentMapSegment,
            CharacterMoveSegment targetMapSegment)
        {
            var result = new CharacterSchedulePlanResult
            {
                Success = true,
                IsCrossMap = true
            };

            if (currentMapSegment != null)
            {
                result.Segments.Add(currentMapSegment);
            }

            if (targetMapSegment != null)
            {
                result.Segments.Add(targetMapSegment);
            }

            return result;
        }

        public static CharacterMoveSegment CreateSegment(
            string mapId,
            Vector3Int startCell,
            Vector3Int targetCell,
            List<Vector3Int> path,
            string enterEdgeId,
            string exitEdgeId)
        {
            var segment = new CharacterMoveSegment
            {
                mapId = mapId,
                startCell = startCell,
                targetCell = targetCell,
                enterEdgeId = enterEdgeId,
                exitEdgeId = exitEdgeId
            };

            if (path != null)
            {
                segment.path.AddRange(path);
            }

            return segment;
        }
    }
}
