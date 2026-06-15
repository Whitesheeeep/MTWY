using Gameplay.TimeSystem;

namespace GameData
{
    /// <summary>
    /// 将 Gameplay.TimeSystem.GameTimeManager 适配为对话条件可读取的时间服务。
    /// </summary>
    public sealed class GameTimeDialogueService : IGameTimeService
    {
        #region 字段
        private readonly GameTimeManager timeManager;
        #endregion

        #region 初始化
        /// <summary>
        /// 创建一个绑定指定时间管理器的对话时间服务。
        /// </summary>
        /// <param name="timeManager">游戏时间管理器。</param>
        public GameTimeDialogueService(GameTimeManager timeManager)
        {
            this.timeManager = timeManager;
        }
        #endregion

        #region 属性
        /// <inheritdoc />
        public int CurrentDay => CurrentTime.Day;

        /// <inheritdoc />
        public int CurrentHour => CurrentTime.Hour;
        #endregion

        #region 工具方法
        private GameTimeData CurrentTime => timeManager == null ? default : timeManager.CurrentTime.Value;
        #endregion
    }
}
