using System.Collections.Generic;
using UnityEngine;

namespace GameData.CharacterSchedule
{
    /// <summary>
    /// A single map-local movement segment. Cross-map schedule movement is represented by
    /// multiple segments queued on the runtime state.
    /// </summary>
    public sealed class CharacterMoveSegment
    {
        public string mapId;
        public Vector3Int startCell;
        public Vector3Int targetCell;
        public List<Vector3Int> path = new List<Vector3Int>();
        public string enterEdgeId;
        public string exitEdgeId;
    }
}
