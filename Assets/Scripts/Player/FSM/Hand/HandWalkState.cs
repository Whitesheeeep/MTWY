/// <summary>
/// Hand 部位行走状态节点。
/// </summary>
public sealed class HandWalkState : PlayerPartStateNodeBase
{
    protected override string ACStateName => PlayerACStateNames.Walk;

    /// <summary>
    /// 创建 Hand 行走状态节点。
    /// </summary>
    public HandWalkState(PlayerPartACController ac) : base(PlayerPartState.Walk, ac)
    {
    }
}
