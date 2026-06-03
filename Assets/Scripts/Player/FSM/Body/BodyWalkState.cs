/// <summary>
/// Body 部位行走状态节点。
/// </summary>
public sealed class BodyWalkState : PlayerPartStateNodeBase
{
    protected override string ACStateName => PlayerACStateNames.Walk;

    /// <summary>
    /// 创建 Body 行走状态节点。
    /// </summary>
    public BodyWalkState(PlayerPartACController ac) : base(PlayerPartState.Walk, ac)
    {
    }
}
