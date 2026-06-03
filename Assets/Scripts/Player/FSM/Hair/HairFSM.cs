/// <summary>
/// Hair 部位 FSM，注册头发的 Idle、Walk、Run 状态。
/// </summary>
public sealed class HairFSM : PlayerPartFSMBase
{
    /// <summary>
    /// 创建 Hair 部位 FSM。
    /// </summary>
    public HairFSM(Player owner) : base(owner, ResolveAC(owner))
    {
        if (!IsInitialized)
        {
            return;
        }

        AddState(new HairIdleState(AC));
        AddState(new HairWalkState(AC));
        AddState(new HairRunState(AC));
        SetDefaultState(PlayerPartState.Idle);
        RegisterMotionTransitions();
    }

    private static PlayerPartACController ResolveAC(Player owner)
    {
        return owner.TryGetPartAC(PlayerPartType.Hair, out PlayerPartACController ac) ? ac : null;
    }
}
