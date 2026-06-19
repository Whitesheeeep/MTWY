using UnityEngine;

namespace GameData.CharacterSchedule
{
    /// <summary>
    /// 角色日程条件基类。Condition 只读取 Services，不保存运行时状态。
    /// </summary>
    public abstract class CharacterScheduleCondition : ScriptableObject
    {
        /// <summary>
        /// 判断条件是否满足。
        /// </summary>
        /// <param name="services">ScheduleManager 注入的只读服务集合。</param>
        /// <param name="failedReason">不满足时输出调试原因。</param>
        public abstract bool IsMet(ICharacterScheduleServices services, out string failedReason);
    }
}
