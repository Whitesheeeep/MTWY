using WS_Modules.FSM;

/// <summary>
/// Hand 部位 FSM，注册普通移动状态和持有移动状态。
/// </summary>
public sealed class HandFSM : PlayerPartFSMBase
{
    /// <summary>
    /// 创建 Hand 部位 FSM。
    /// </summary>
    public HandFSM(Player owner) : base(owner, ResolveAC(owner))
    {
        if (!IsInitialized)
        {
            return;
        }

        AddState(new HandIdleState(AC));
        AddState(new HandWalkState(AC));
        AddState(new HandRunState(AC));
        AddState(new HandHoldIdleState(AC));
        AddState(new HandHoldWalkState(AC));
        AddState(new HandHoldRunState(AC));
        SetDefaultState(PlayerPartState.Idle);
        RegisterMotionTransitions();
        RegisterHoldTransitions();
    }

    private static PlayerPartACController ResolveAC(Player owner)
    {
        return owner.TryGetPartAC(PlayerPartType.Hand, out PlayerPartACController ac) ? ac : null;
    }

    private void RegisterHoldTransitions()
    {
        AddAnyTransition(new Transition<PlayerPartState, Player>(PlayerPartState.Idle, PlayerPartState.HoldRun, 120)
            .AddCondition(owner => owner.IsHandHolding)
            .AddCondition(HasMoveInput)
            .AddCondition(owner => owner.IsRunPressed));

        AddAnyTransition(new Transition<PlayerPartState, Player>(PlayerPartState.Idle, PlayerPartState.HoldWalk, 110)
            .AddCondition(owner => owner.IsHandHolding)
            .AddCondition(HasMoveInput)
            .AddCondition(owner => !owner.IsRunPressed));

        AddAnyTransition(new Transition<PlayerPartState, Player>(PlayerPartState.Idle, PlayerPartState.HoldIdle, 100)
            .AddCondition(owner => owner.IsHandHolding)
            .AddCondition(owner => !HasMoveInput(owner)));

        RegisterReleaseTransitions(PlayerPartState.HoldIdle);
        RegisterReleaseTransitions(PlayerPartState.HoldWalk);
        RegisterReleaseTransitions(PlayerPartState.HoldRun);
    }

    private void RegisterReleaseTransitions(PlayerPartState holdState)
    {
        AddTransition(new Transition<PlayerPartState, Player>(holdState, PlayerPartState.Idle)
            .AddCondition(owner => !owner.IsHandHolding)
            .AddCondition(owner => !HasMoveInput(owner)));

        AddTransition(new Transition<PlayerPartState, Player>(holdState, PlayerPartState.Walk)
            .AddCondition(owner => !owner.IsHandHolding)
            .AddCondition(HasMoveInput)
            .AddCondition(owner => !owner.IsRunPressed));

        AddTransition(new Transition<PlayerPartState, Player>(holdState, PlayerPartState.Run)
            .AddCondition(owner => !owner.IsHandHolding)
            .AddCondition(HasMoveInput)
            .AddCondition(owner => owner.IsRunPressed));
    }
}
