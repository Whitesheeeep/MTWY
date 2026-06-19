namespace GameData.CharacterSchedule
{
    /// <summary>
    /// 当前正在评估的角色与 Entry 上下文。
    /// 该服务只在单个 Entry 条件评估期间有效。
    /// </summary>
    public interface ICharacterScheduleEvaluationService
    {
        /// <summary>
        /// 正在评估的角色 ID。
        /// </summary>
        string CharacterId { get; }

        /// <summary>
        /// 正在评估的角色运行时状态。
        /// </summary>
        CharacterRuntimeState State { get; }

        /// <summary>
        /// 正在评估的日程项。
        /// </summary>
        CharacterScheduleEntry Entry { get; }
    }
}
