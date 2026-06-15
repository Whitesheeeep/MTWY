namespace GameData
{
    /// <summary>
    /// 对话系统读取游戏时间的最小服务接口。
    /// </summary>
    public interface IGameTimeService
    {
        #region 属性
        /// <summary>
        /// 当前游戏天数。
        /// </summary>
        int CurrentDay { get; }

        /// <summary>
        /// 当前游戏小时。
        /// </summary>
        int CurrentHour { get; }
        #endregion
    }
}
