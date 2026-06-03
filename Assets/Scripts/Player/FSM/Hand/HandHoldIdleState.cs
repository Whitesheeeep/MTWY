/// <summary>
/// Hand 部位持有且静止状态节点。
/// </summary>
public sealed class HandHoldIdleState : PlayerPartStateNodeBase
{
    protected override string ACStateName => PlayerACStateNames.HoldIdle;

    /// <summary>
    /// 创建 Hand 持有且静止状态节点。
    /// </summary>
    public HandHoldIdleState(PlayerPartACController ac) : base(PlayerPartState.HoldIdle, ac)
    {
    }
}
