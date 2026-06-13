namespace Gameplay.TimeSystem
{
    /// <summary>
    /// 游戏时间变化事件参数，包含变化前后的时间快照。
    /// </summary>
    public readonly struct GameTimeChangedEventArgs
    {
        public GameTimeData Previous { get; }
        public GameTimeData Current { get; }

        public GameTimeChangedEventArgs(GameTimeData previous, GameTimeData current)
        {
            Previous = previous;
            Current = current;
        }
    }

    /// <summary>
    /// 游戏时间倍率变化事件参数。
    /// </summary>
    public readonly struct GameTimeScaleChangedEventArgs
    {
        public float Previous { get; }
        public float Current { get; }

        public GameTimeScaleChangedEventArgs(float previous, float current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
