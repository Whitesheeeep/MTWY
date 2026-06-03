/// <summary>
/// Hair 部位奔跑状态节点。
/// </summary>
public sealed class HairRunState : PlayerPartStateNodeBase
{
    protected override string ACStateName => PlayerACStateNames.Run;

    /// <summary>
    /// 创建 Hair 奔跑状态节点。
    /// </summary>
    public HairRunState(PlayerPartACController ac) : base(PlayerPartState.Run, ac)
    {
    }
}
