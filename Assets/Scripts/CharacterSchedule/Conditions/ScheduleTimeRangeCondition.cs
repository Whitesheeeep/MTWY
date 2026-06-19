using UnityEngine;

namespace GameData.CharacterSchedule
{
    /// <summary>
    /// 游戏时间范围条件。支持跨天范围，例如 22:00 到 02:00。
    /// </summary>
    [CreateAssetMenu(fileName = "ScheduleTimeRangeCondition", menuName = "GameData/Character/Schedule Condition/Time Range")]
    public sealed class ScheduleTimeRangeCondition : CharacterScheduleCondition
    {
        [SerializeField, Range(0, 23)] private int startHour;
        [SerializeField, Range(0, 59)] private int startMinute;
        [SerializeField, Range(0, 23)] private int endHour;
        [SerializeField, Range(0, 59)] private int endMinute;

        public override bool IsMet(ICharacterScheduleServices services, out string failedReason)
        {
            failedReason = string.Empty;
            if (services == null || !services.TryGet(out ICharacterScheduleTimeService timeService))
            {
                failedReason = "Character schedule time service is not registered.";
                return false;
            }

            int current = timeService.CurrentTime.Hour * 60 + timeService.CurrentTime.Minute;
            int start = startHour * 60 + startMinute;
            int end = endHour * 60 + endMinute;

            bool inRange = start <= end
                ? current >= start && current <= end
                : current >= start || current <= end;

            if (!inRange)
            {
                failedReason = $"Need time in range {startHour:D2}:{startMinute:D2}-{endHour:D2}:{endMinute:D2}.";
            }

            return inRange;
        }
    }
}
