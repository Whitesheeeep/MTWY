namespace GameData.CharacterSchedule
{
    /// <summary>
    /// 日程条件读取外部运行时 flag 的服务。
    /// </summary>
    public interface ICharacterScheduleFlagService
    {
        /// <summary>
        /// 获取 flag 当前值。未设置的 flag 默认视为 false。
        /// </summary>
        bool GetFlag(string flagId);
    }
}
