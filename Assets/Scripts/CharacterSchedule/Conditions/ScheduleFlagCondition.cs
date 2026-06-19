using UnityEngine;

namespace GameData.CharacterSchedule
{
    /// <summary>
    /// 运行时 flag 条件。用于外部事件驱动角色日程切换。
    /// </summary>
    [CreateAssetMenu(fileName = "ScheduleFlagCondition", menuName = "GameData/Character/Schedule Condition/Flag")]
    public sealed class ScheduleFlagCondition : CharacterScheduleCondition
    {
        /// <summary>
        /// 要读取的 flag ID。
        /// </summary>
        [SerializeField] private string flagId;

        /// <summary>
        /// 期望 flag 当前值。
        /// </summary>
        [SerializeField] private bool expectedValue = true;

        public override bool IsMet(ICharacterScheduleServices services, out string failedReason)
        {
            failedReason = string.Empty;
            if (services == null || !services.TryGet(out ICharacterScheduleFlagService flagService))
            {
                failedReason = "Character schedule flag service is not registered.";
                return false;
            }

            bool value = flagService.GetFlag(flagId);
            if (value == expectedValue)
            {
                return true;
            }

            failedReason = $"Need flag '{flagId}' to be {expectedValue}.";
            return false;
        }
    }
}
