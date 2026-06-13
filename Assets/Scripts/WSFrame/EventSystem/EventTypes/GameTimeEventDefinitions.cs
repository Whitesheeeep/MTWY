namespace WS_Modules.CustomEventSystem
{
    /// <summary>
    /// 游戏时间系统事件。只包含时间本身的事件，不包含作物、动物、机器等业务事件。
    /// </summary>
    public enum E_GameTimeEvent
    {
        start = EventIdRange.GameTimeStart,
        MinuteChanged = start + 1,
        HourChanged,
        DayStarted,
        MonthChanged,
        SeasonChanged,
        YearChanged,
        TimeScaleChanged,
        end,
    }
}
