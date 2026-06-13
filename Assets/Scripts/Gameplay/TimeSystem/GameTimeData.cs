using System;

namespace Gameplay.TimeSystem
{
    /// <summary>
    /// 游戏内时间快照。该类型只描述当前时间状态，不负责推进、存档或事件派发。
    /// </summary>
    [Serializable]
    public readonly struct GameTimeData
    {
        /// <summary>
        /// 年份，从 1 开始。
        /// </summary>
        public int Year { get; }

        /// <summary>
        /// 月份，范围 1-12。
        /// </summary>
        public int Month { get; }

        /// <summary>
        /// 当月日期，范围 1-DaysPerMonth。
        /// </summary>
        public int Day { get; }

        /// <summary>
        /// 小时，范围 0-23。
        /// </summary>
        public int Hour { get; }

        /// <summary>
        /// 分钟，范围 0-59。
        /// </summary>
        public int Minute { get; }

        /// <summary>
        /// 当前季节，由月份推导而来。
        /// </summary>
        public GameSeason Season { get; }

        /// <summary>
        /// 从游戏纪元开始累计经过的游戏分钟，用于存档、排序和时间差计算。
        /// </summary>
        public long TotalMinutes { get; }

        public GameTimeData(
            int year,
            int month,
            int day,
            int hour,
            int minute,
            GameSeason season,
            long totalMinutes)
        {
            Year = year;
            Month = month;
            Day = day;
            Hour = hour;
            Minute = minute;
            Season = season;
            TotalMinutes = totalMinutes;
        }

        public override string ToString()
        {
            return $"Y{Year:D2}-{Month:D2}-{Day:D2} {Hour:D2}:{Minute:D2} ({Season})";
        }
    }
}
