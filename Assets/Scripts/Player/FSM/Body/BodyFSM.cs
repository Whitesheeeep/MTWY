/// <summary>
/// Body 部位 FSM，注册身体的 Idle、Walk、Run 状态。
/// </summary>
public sealed class BodyFSM : PlayerPartFSMBase
{
    /// <summary>
    /// 创建 Body 部位 FSM。
    /// </summary>
    public BodyFSM(Player owner) : base(owner, ResolveAC(owner))
    {
        if (!IsInitialized)
        {
            return;
        }

        AddState(new BodyIdleState(AC));
        AddState(new BodyWalkState(AC));
        AddState(new BodyRunState(AC));
        SetDefaultState(PlayerPartState.Idle);
        RegisterMotionTransitions();
    }

    private static PlayerPartACController ResolveAC(Player owner)
    {
        return owner.TryGetPartAC(PlayerPartType.Body, out PlayerPartACController ac) ? ac : null;
    }
}
