using System.Collections.Generic;

/// <summary>
/// Player 的 FSM 总控，统一注册并驱动各部位 FSM。
/// </summary>
public sealed class PlayerFSMController
{
    private readonly Dictionary<PlayerPartType, PlayerPartFSMBase> partFSMs =
        new Dictionary<PlayerPartType, PlayerPartFSMBase>();

    /// <summary>
    /// 创建并注册 Player 各部位 FSM。
    /// </summary>
    public PlayerFSMController(Player owner)
    {
        RegisterPartFSM(PlayerPartType.Body, new BodyFSM(owner));
        RegisterPartFSM(PlayerPartType.Hair, new HairFSM(owner));
        RegisterPartFSM(PlayerPartType.Hand, new HandFSM(owner));
    }

    /// <summary>
    /// 尝试获取指定部位的 FSM。
    /// </summary>
    public bool TryGetPartFSM(PlayerPartType partType, out PlayerPartFSMBase fsm)
    {
        return partFSMs.TryGetValue(partType, out fsm);
    }

    /// <summary>
    /// 进入所有已初始化的部位 FSM。
    /// </summary>
    public void OnEnter()
    {
        foreach (PlayerPartFSMBase fsm in partFSMs.Values)
        {
            EnterFSM(fsm);
        }
    }

    /// <summary>
    /// 更新所有已初始化的部位 FSM。
    /// </summary>
    public void OnUpdate()
    {
        foreach (PlayerPartFSMBase fsm in partFSMs.Values)
        {
            UpdateFSM(fsm);
        }
    }

    /// <summary>
    /// 固定帧更新所有已初始化的部位 FSM。
    /// </summary>
    public void OnFixedUpdate()
    {
        foreach (PlayerPartFSMBase fsm in partFSMs.Values)
        {
            FixedUpdateFSM(fsm);
        }
    }

    private void RegisterPartFSM(PlayerPartType partType, PlayerPartFSMBase fsm)
    {
        partFSMs[partType] = fsm;
    }

    private static void EnterFSM(PlayerPartFSMBase fsm)
    {
        if (fsm != null && fsm.IsInitialized)
        {
            fsm.OnEnter();
        }
    }

    private static void UpdateFSM(PlayerPartFSMBase fsm)
    {
        if (fsm != null && fsm.IsInitialized)
        {
            fsm.OnUpdate();
        }
    }

    private static void FixedUpdateFSM(PlayerPartFSMBase fsm)
    {
        if (fsm != null && fsm.IsInitialized)
        {
            fsm.OnFixedUpdate();
        }
    }
}
