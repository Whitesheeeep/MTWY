namespace Gameplay.TimeSystem
{
    /// <summary>
    /// 表示对象可以根据游戏时间推进自身状态。
    /// 适用于作物生长、生物成长、机器加工等“经过若干游戏分钟后变化”的对象。
    /// </summary>
    public interface IGameTimeProgressable
    {
        /// <summary>
        /// 当前对象使用的游戏时间管理器。
        /// 实现者通过它注册、取消或恢复自己的游戏时间任务。
        /// </summary>
        GameTimeManager GameTimeManager { get; set; }

        /// <summary>
        /// 开始接入游戏时间，并由对象自行安排下一次状态变化。
        /// </summary>
        void StartGameTimeProgress();

        /// <summary>
        /// 停止接入游戏时间，并取消当前对象已经预约的运行时任务。
        /// </summary>
        void StopGameTimeProgress();

        /// <summary>
        /// 根据当前游戏时间恢复进度。
        /// 读档或重建对象后，应该用当前时间计算剩余时间并重新预约。
        /// </summary>
        void RestoreGameTimeProgress(GameTimeData currentTime);
    }

    /// <summary>
    /// 可存档的游戏时间进度状态。
    /// TimeWheelHandle 只用于运行时取消，不应写入存档。
    /// </summary>
    public interface IGameTimeProgressState
    {
        /// <summary>
        /// 是否还有等待中的时间变化。
        /// </summary>
        bool HasPendingProgress { get; }

        /// <summary>
        /// 下一次变化发生时的累计游戏分钟。
        /// </summary>
        long NextProgressTotalMinutes { get; }
    }
}
