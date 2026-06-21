using Gameplay.TimeSystem;

namespace FarmSystem
{
    /// <summary>
    /// Farm 系统常用运行时设置。后续如果改为配置资产，可优先从这里收口读取入口。
    /// </summary>
    public static class FarmSetting
    {
        public const int DefaultWaterRetentionDays = 2;

        public static int MinutesPerDay => GameTimeManager.MinutesPerDay;

        public static int NormalizeWaterRetentionDays(int days)
        {
            return days < 1 ? 1 : days;
        }

        public static int GetWaterRetentionMinutes()
        {
            return NormalizeWaterRetentionDays(DefaultWaterRetentionDays) * MinutesPerDay;
        }
    }
}