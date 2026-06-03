namespace WS_Modules.FSM
{
    /// <summary>
    /// 类状态基类。
    /// 业务状态通常继承该类，只重写自己需要的生命周期方法。
    /// </summary>
    public abstract class StateBase<TStateId, TOwner> : IState<TStateId, TOwner>
    {
        public TStateId StateId { get; private set; }
        public IStateMachine<TStateId, TOwner> Machine { get; private set; }
        public TOwner Owner { get; private set; }

        protected StateBase(TStateId stateId)
        {
            StateId = stateId;
        }

        /// <summary>
        /// 目标状态进入守卫，默认允许进入。
        /// </summary>
        public virtual bool CanEnter()
        {
            return true;
        }

        /// <summary>
        /// 保存父状态机传入的 owner 和 machine 引用。
        /// </summary>
        public virtual void Init(TOwner owner, IStateMachine<TStateId, TOwner> machine)
        {
            Owner = owner;
            Machine = machine;
        }

        public virtual void OnEnter()
        {
        }

        public virtual void OnUpdate()
        {
        }

        public virtual void OnFixedUpdate()
        {
        }

        public virtual void OnExit()
        {
        }

        /// <summary>
        /// 输出当前状态节点的调试文本。
        /// 叶子状态默认只输出自身；子状态机可以重写该方法继续递归输出子树。
        /// </summary>
        /// <param name="indent">父节点传入的缩进。</param>
        /// <param name="isLast">当前节点是否是父节点下的最后一个子节点。</param>
        /// <param name="isCurrent">当前节点是否是父状态机的激活状态。</param>
        /// <param name="isDefault">当前节点是否是父状态机的默认状态。</param>
        public virtual string ToDebugString(string indent, bool isLast, bool isCurrent, bool isDefault)
        {
            return FormatDebugLine(indent, isLast, StateId, BuildDebugTags(isCurrent, isDefault, false));
        }

        /// <summary>
        /// 统一格式化单个树节点行。
        /// </summary>
        protected static string FormatDebugLine(string indent, bool isLast, object stateId, string tags)
        {
            return indent + (isLast ? "└─ " : "├─ ") + stateId + tags;
        }

        /// <summary>
        /// 根据节点身份生成调试标签，例如 [Current, Default, StateMachine]。
        /// </summary>
        protected static string BuildDebugTags(bool isCurrent, bool isDefault, bool isStateMachine)
        {
            string tags = string.Empty;

            if (isCurrent)
            {
                tags += tags.Length == 0 ? "Current" : ", Current";
            }

            if (isDefault)
            {
                tags += tags.Length == 0 ? "Default" : ", Default";
            }

            if (isStateMachine)
            {
                tags += tags.Length == 0 ? "StateMachine" : ", StateMachine";
            }

            return tags.Length == 0 ? string.Empty : " [" + tags + "]";
        }
    }
}
