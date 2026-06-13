using System;

namespace Gameplay.TimeSystem
{
    /// <summary>
    /// JsonMgr 使用的游戏时间存档数据。Season 可由 month 推导，因此不保存。
    /// </summary>
    [Serializable]
    public sealed class GameTimeSaveData
    {
        public int year;
        public int month;
        public int day;
        public int hour;
        public int minute;
        public long totalMinutes;
    }
}
