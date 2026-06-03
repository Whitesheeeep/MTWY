/// <summary>
/// Body 部位静止状态节点。
/// </summary>
public sealed class BodyIdleState : PlayerPartStateNodeBase
{
    protected override string ACStateName => PlayerACStateNames.Idle;

    /// <summary>
    /// 创建 Body 静止状态节点。
    /// </summary>
    public BodyIdleState(PlayerPartACController ac) : base(PlayerPartState.Idle, ac)
    {
    }
}
