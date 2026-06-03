namespace WS_Modules.FSM
{
    /// <summary>
    /// FSM/HFSM 共用的状态接口。
    /// 普通状态和子状态机都通过这个接口被状态机调度。
    /// </summary>
    public interface IState<TStateId, TOwner>
    {
        /// <summary>
        /// 状态唯一标识。推荐使用 enum，也可以使用 string/int。
        /// </summary>
        TStateId StateId { get; }

        /// <summary>
        /// 当前状态所属的状态机。状态内部主动切换时使用 Machine.ChangeState。
        /// </summary>
        IStateMachine<TStateId, TOwner> Machine { get; }

        /// <summary>
        /// 业务宿主或上下文对象，供状态和过渡条件读取业务数据。
        /// </summary>
        TOwner Owner { get; }

        /// <summary>
        /// 返回 true 时允许进入该状态。
        /// ChangeState 和自动过渡都会检查目标状态的 CanEnter。
        /// </summary>
        bool CanEnter();

        /// <summary>
        /// 状态加入状态机时，或状态机刷新 owner 上下文时调用。
        /// </summary>
        void Init(TOwner owner, IStateMachine<TStateId, TOwner> machine);

        /// <summary>
        /// 进入状态时调用一次。
        /// </summary>
        void OnEnter();

        /// <summary>
        /// 当前状态激活期间每帧调用。
        /// </summary>
        void OnUpdate();

        /// <summary>
        /// 当前状态激活期间每个物理帧调用。
        /// </summary>
        void OnFixedUpdate();

        /// <summary>
        /// 离开状态时调用一次。
        /// </summary>
        void OnExit();
    }
}
