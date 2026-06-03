/// <summary>
/// Hand 部位静止状态节点。
/// </summary>
public sealed class HandIdleState : PlayerPartStateNodeBase
{
    protected override string ACStateName => PlayerACStateNames.Idle;

    /// <summary>
    /// 创建 Hand 静止状态节点。
    /// </summary>
    public HandIdleState(PlayerPartACController ac) : base(PlayerPartState.Idle, ac)
    {
    }
}
