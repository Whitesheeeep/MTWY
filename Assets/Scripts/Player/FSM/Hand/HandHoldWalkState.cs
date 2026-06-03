/// <summary>
/// Hand 部位持有且行走状态节点。
/// </summary>
public sealed class HandHoldWalkState : PlayerPartStateNodeBase
{
    protected override string ACStateName => PlayerACStateNames.HoldWalk;

    /// <summary>
    /// 创建 Hand 持有且行走状态节点。
    /// </summary>
    public HandHoldWalkState(PlayerPartACController ac) : base(PlayerPartState.HoldWalk, ac)
    {
    }
}
