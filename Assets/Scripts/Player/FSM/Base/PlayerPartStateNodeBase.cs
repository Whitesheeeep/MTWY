using WS_Modules.FSM;

/// <summary>
/// Player 部位状态节点基类，负责进入状态时播放 AC 状态并持续更新方向参数。
/// </summary>
public abstract class PlayerPartStateNodeBase : StateBase<PlayerPartState, Player>
{
    protected readonly PlayerPartACController AC;

    /// <summary>
    /// 当前状态进入时要播放的 Animator Controller 状态名。
    /// </summary>
    protected abstract string ACStateName { get; }

    /// <summary>
    /// 创建部位状态节点。
    /// </summary>
    protected PlayerPartStateNodeBase(PlayerPartState stateId, PlayerPartACController ac)
        : base(stateId)
    {
        AC = ac;
    }

    /// <summary>
    /// 进入状态时同步方向并播放对应 AC 状态。
    /// </summary>
    public override void OnEnter()
    {
        AC.SetDirection(Owner.CurrentDirectionVector);
        AC.Play(ACStateName);
    }

    /// <summary>
    /// 状态保持期间持续同步方向参数。
    /// </summary>
    public override void OnUpdate()
    {
        AC.SetDirection(Owner.CurrentDirectionVector);
    }
}
