/// <summary>
/// Body 部位奔跑状态节点。
/// </summary>
public sealed class BodyRunState : PlayerPartStateNodeBase
{
    protected override string ACStateName => PlayerACStateNames.Run;

    /// <summary>
    /// 创建 Body 奔跑状态节点。
    /// </summary>
    public BodyRunState(PlayerPartACController ac) : base(PlayerPartState.Run, ac)
    {
    }
}
