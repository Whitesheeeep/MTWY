using System;
using System.Collections.Generic;
using UnityEngine;
using WS_Modules;

namespace GameData.CharacterSchedule
{
    /// <summary>
    /// 一个候选日程目标。Condition 全部满足后，ScheduleManager 根据 priority 选择执行。
    /// </summary>
    [Serializable]
    public sealed class CharacterScheduleEntry
    {
        /// <summary>
        /// 日程项唯一 ID。用于运行时记录 activeEntryId。
        /// </summary>
        public string entryId;

        /// <summary>
        /// 优先级。多个 Entry 同时满足时，数值越大越优先。
        /// </summary>
        public int priority;

        /// <summary>
        /// 目标地图 ID。当前约定与场景名 / MapGrid mapId 对齐。
        /// </summary>
        [WSScene]
        public string targetMapId;

        /// <summary>
        /// 目标逻辑格子。移动和离线结算都以 cell 为权威位置。
        /// </summary>
        public Vector3Int targetCell;

        /// <summary>
        /// 大于 0 时覆盖角色默认移动速度。
        /// </summary>
        public float moveSpeedOverride;

        /// <summary>
        /// 日程条件列表。为空表示该 Entry 永远满足。
        /// </summary>
        public List<CharacterScheduleCondition> conditions = new List<CharacterScheduleCondition>();
    }
}
