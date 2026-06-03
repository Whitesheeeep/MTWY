/// <summary>
/// Hand 部位持有且奔跑状态节点。
/// </summary>
public sealed class HandHoldRunState : PlayerPartStateNodeBase
{
    protected override string ACStateName => PlayerACStateNames.HoldRun;

    /// <summary>
    /// 创建 Hand 持有且奔跑状态节点。
    /// </summary>
    public HandHoldRunState(PlayerPartACController ac) : base(PlayerPartState.HoldRun, ac)
    {
    }
}
