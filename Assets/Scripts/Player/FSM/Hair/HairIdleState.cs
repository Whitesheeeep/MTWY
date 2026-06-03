/// <summary>
/// Hair 部位静止状态节点。
/// </summary>
public sealed class HairIdleState : PlayerPartStateNodeBase
{
    protected override string ACStateName => PlayerACStateNames.Idle;

    /// <summary>
    /// 创建 Hair 静止状态节点。
    /// </summary>
    public HairIdleState(PlayerPartACController ac) : base(PlayerPartState.Idle, ac)
    {
    }
}
