using System.Collections.Generic;
using UnityEngine;

namespace GameData.CharacterSchedule
{
    /// <summary>
    /// 单个角色的日程配置。第一版使用 List 顺序遍历所有 Entry。
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterSchedule", menuName = "GameData/Character/Schedule")]
    public sealed class CharacterSchedule_SO : ScriptableObject
    {
        /// <summary>
        /// 该日程所属角色 ID，需要匹配 CharacterDefinition_SO.characterId。
        /// </summary>
        public string characterId;

        /// <summary>
        /// 候选日程列表。评估时满足条件且 priority 最高的 Entry 会成为当前目标。
        /// </summary>
        public List<CharacterScheduleEntry> entries = new List<CharacterScheduleEntry>();
    }
}
