using WS_Modules.FSM;

/// <summary>
/// Player 单个部位 FSM 的公共基类，封装通用移动状态切换。
/// </summary>
public abstract class PlayerPartFSMBase : StateMachine<PlayerPartState, Player>
{
    protected readonly PlayerPartACController AC;

    /// <summary>
    /// 当前部位 FSM 是否成功拿到对应的 AC Controller。
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// 使用已解析的 AC Controller 创建部位 FSM。
    /// </summary>
    protected PlayerPartFSMBase(Player owner, PlayerPartACController ac)
        : base(PlayerPartState.Idle, owner)
    {
        AC = ac;
        IsInitialized = AC != null;
    }

    /// <summary>
    /// 注册 Idle、Walk、Run 之间的通用移动状态切换。
    /// </summary>
    protected void RegisterMotionTransitions()
    {
        AddTransition(new Transition<PlayerPartState, Player>(PlayerPartState.Idle, PlayerPartState.Walk)
            .AddCondition(HasMoveInput)
            .AddCondition(owner => !owner.IsRunPressed));

        AddTransition(new Transition<PlayerPartState, Player>(PlayerPartState.Idle, PlayerPartState.Run)
            .AddCondition(HasMoveInput)
            .AddCondition(owner => owner.IsRunPressed));

        AddTransition(new Transition<PlayerPartState, Player>(PlayerPartState.Walk, PlayerPartState.Run)
            .AddCondition(HasMoveInput)
            .AddCondition(owner => owner.IsRunPressed));

        AddTransition(new Transition<PlayerPartState, Player>(PlayerPartState.Run, PlayerPartState.Walk)
            .AddCondition(HasMoveInput)
            .AddCondition(owner => !owner.IsRunPressed));

        AddTransition(new Transition<PlayerPartState, Player>(PlayerPartState.Walk, PlayerPartState.Idle)
            .AddCondition(owner => !HasMoveInput(owner)));

        AddTransition(new Transition<PlayerPartState, Player>(PlayerPartState.Run, PlayerPartState.Idle)
            .AddCondition(owner => !HasMoveInput(owner)));
    }

    /// <summary>
    /// 判断 Player 当前是否有有效移动输入。
    /// </summary>
    protected static bool HasMoveInput(Player owner)
    {
        return owner.MoveDir.sqrMagnitude > 0.0001f;
    }
}
