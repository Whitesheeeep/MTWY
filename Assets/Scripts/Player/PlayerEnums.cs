/// <summary>
/// Player 可独立播放动画的部位类型。
/// </summary>
public enum PlayerPartType
{
    /// <summary>
    /// 身体部位。
    /// </summary>
    Body,

    /// <summary>
    /// 头发部位。
    /// </summary>
    Hair,

    /// <summary>
    /// 手部部位，当前场景资源仍可命名为 Arm。
    /// </summary>
    Hand
}

/// <summary>
/// Player 部位 FSM 使用的逻辑状态。
/// </summary>
public enum PlayerPartState
{
    /// <summary>
    /// 静止。
    /// </summary>
    Idle,

    /// <summary>
    /// 行走。
    /// </summary>
    Walk,

    /// <summary>
    /// 奔跑。
    /// </summary>
    Run,

    /// <summary>
    /// 手部持有且静止。
    /// </summary>
    HoldIdle,

    /// <summary>
    /// 手部持有且行走。
    /// </summary>
    HoldWalk,

    /// <summary>
    /// 手部持有且奔跑。
    /// </summary>
    HoldRun
}

/// <summary>
/// Player 最近一次有效移动方向。
/// </summary>
public enum PlayerDirection
{
    /// <summary>
    /// 朝下。
    /// </summary>
    Down,

    /// <summary>
    /// 朝上。
    /// </summary>
    Up,

    /// <summary>
    /// 朝左。
    /// </summary>
    Left,

    /// <summary>
    /// 朝右。
    /// </summary>
    Right
}