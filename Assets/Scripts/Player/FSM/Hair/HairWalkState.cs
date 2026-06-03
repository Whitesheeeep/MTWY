/// <summary>
/// Hair 部位行走状态节点。
/// </summary>
public sealed class HairWalkState : PlayerPartStateNodeBase
{
    protected override string ACStateName => PlayerACStateNames.Walk;

    /// <summary>
    /// 创建 Hair 行走状态节点。
    /// </summary>
    public HairWalkState(PlayerPartACController ac) : base(PlayerPartState.Walk, ac)
    {
    }
}
