/// <summary>
/// Hand 部位奔跑状态节点。
/// </summary>
public sealed class HandRunState : PlayerPartStateNodeBase
{
    protected override string ACStateName => PlayerACStateNames.Run;

    /// <summary>
    /// 创建 Hand 奔跑状态节点。
    /// </summary>
    public HandRunState(PlayerPartACController ac) : base(PlayerPartState.Run, ac)
    {
    }
}
