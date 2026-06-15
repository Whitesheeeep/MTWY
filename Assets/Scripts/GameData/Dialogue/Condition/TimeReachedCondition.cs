using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 判断当前游戏时间是否已经到达指定天数和小时的对话条件。
    /// </summary>
    [CreateAssetMenu(fileName = "TimeReachedCondition", menuName = "GameData/Dialogue/Condition/Time Reached", order = 1)]
    public sealed class TimeReachedCondition : DialogueCondition
    {
        #region 字段
        [SerializeField] private int requiredDay;
        [SerializeField, Range(0, 23)] private int requiredHour;
        #endregion

        #region 条件判断
        /// <inheritdoc />
        public override bool IsMet(IDialogueServices services, out string failedReason)
        {
            failedReason = string.Empty;

            if (services == null || !services.TryGet(out IGameTimeService timeService))
            {
                failedReason = "Game time service is not registered.";
                return false;
            }

            if (timeService.CurrentDay > requiredDay)
            {
                return true;
            }

            if (timeService.CurrentDay == requiredDay && timeService.CurrentHour >= requiredHour)
            {
                return true;
            }

            failedReason = $"Need Day {requiredDay}, Hour {requiredHour}.";
            return false;
        }
        #endregion
    }
}
