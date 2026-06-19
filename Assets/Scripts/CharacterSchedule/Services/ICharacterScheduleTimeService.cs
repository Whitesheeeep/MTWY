using Gameplay.TimeSystem;

namespace GameData.CharacterSchedule
{
    /// <summary>
    /// 日程条件读取游戏时间的服务。
    /// </summary>
    public interface ICharacterScheduleTimeService
    {
        /// <summary>
        /// 当前游戏时间。
        /// </summary>
        GameTimeData CurrentTime { get; }
    }
}
