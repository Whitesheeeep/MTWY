using System.Collections.Generic;
using UnityEngine;

namespace GameData.CharacterSchedule
{
    /// <summary>
    /// 角色运行时逻辑状态。离线角色只保留这里的数据，不持有场景实体。
    /// </summary>
    public sealed class CharacterRuntimeState
    {
        /// <summary>
        /// 角色唯一 ID。
        /// </summary>
        public string characterId;

        /// <summary>
        /// 角色当前所在地图 ID。
        /// </summary>
        public string currentMapId;

        /// <summary>
        /// 角色当前所在逻辑格子。
        /// </summary>
        public Vector3Int currentCell;

        /// <summary>
        /// 当前正在执行或最近到达的日程项 ID。
        /// </summary>
        public string activeEntryId;

        /// <summary>
        /// 当前移动状态。
        /// </summary>
        public CharacterMoveState moveState;

        /// <summary>
        /// 当前日程目标地图。
        /// </summary>
        public string targetMapId;

        /// <summary>
        /// 当前日程目标格子。
        /// </summary>
        public Vector3Int targetCell;

        /// <summary>
        /// 当前地图内尚未走完的路径。跨地图时只保存当前可见地图段。
        /// </summary>
        public List<Vector3Int> remainingPath = new List<Vector3Int>();
        public List<CharacterMoveSegment> pendingSegments = new List<CharacterMoveSegment>();

        /// <summary>
        /// 本次移动使用的速度。
        /// </summary>
        public float moveSpeed;
        public float offlineMoveDistanceCarry;

        /// <summary>
        /// 移动失败或日程阻塞原因，主要用于调试和后续 UI 展示。
        /// </summary>
        public string blockedReason;

        /// <summary>
        /// 判断角色当前是否位于指定地图。
        /// </summary>
        public bool IsInMap(string mapId)
        {
            return !string.IsNullOrWhiteSpace(mapId) && string.Equals(currentMapId, mapId, System.StringComparison.Ordinal);
        }
    }
}
