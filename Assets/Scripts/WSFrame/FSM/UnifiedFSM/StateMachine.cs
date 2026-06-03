using System;
using System.Collections.Generic;
using System.Text;

namespace WS_Modules.FSM
{
    /// <summary>
    /// 统一 FSM/HFSM 状态机。
    /// 它既管理子状态，又继承 StateBase，因此可以嵌套到另一个 StateMachine 中。
    /// </summary>
    public class StateMachine<TStateId, TOwner> : StateBase<TStateId, TOwner>, IStateMachine<TStateId, TOwner>
    {
        // 子状态集合。子状态可以是普通状态，也可以是另一个 StateMachine。
        private readonly Dictionary<TStateId, IState<TStateId, TOwner>> mStates = new();

        // 普通过渡：只有 CurrentState.StateId 等于 FromStateId 时才检测。
        private readonly Dictionary<TStateId, List<Transition<TStateId, TOwner>>> mTransitions = new();

        // 任意状态过渡：优先于普通过渡检测。
        private readonly List<Transition<TStateId, TOwner>> mAnyTransitions = new();

        // 状态机进入时自动进入的默认子状态。
        private bool mHasDefaultState;
        private TStateId mDefaultStateId;

        public IState<TStateId, TOwner> CurrentState { get; private set; }
        public IState<TStateId, TOwner> PreviousState { get; private set; }
        public IReadOnlyDictionary<TStateId, IState<TStateId, TOwner>> States => mStates;

        public StateMachine(TStateId stateId) : base(stateId)
        {
        }

        public StateMachine(TStateId stateId, TOwner owner) : base(stateId)
        {
            Init(owner, null);
        }

        public override void Init(TOwner owner, IStateMachine<TStateId, TOwner> machine)
        {
            base.Init(owner, machine);

            // 当状态机 owner 上下文变化时，同步刷新所有已有子状态。
            foreach (var state in mStates.Values)
            {
                state.Init(owner, this);
            }
        }

        /// <summary>
        /// 添加子状态。第一个添加的子状态会自动成为默认状态。
        /// </summary>
        public void AddState(IState<TStateId, TOwner> state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            mStates.Add(state.StateId, state);
            state.Init(Owner, this);

            if (!mHasDefaultState)
            {
                SetDefaultState(state.StateId);
            }
        }

        /// <summary>
        /// 获取或创建链式 CustomState。
        /// 如果该 id 已经属于类状态或子状态机，则返回 null。
        /// </summary>
        public CustomState<TStateId, TOwner> State(TStateId stateId)
        {
            IState<TStateId, TOwner> state;
            if (mStates.TryGetValue(stateId, out state))
            {
                return state as CustomState<TStateId, TOwner>;
            }

            var customState = new CustomState<TStateId, TOwner>(stateId);
            AddState(customState);
            return customState;
        }

        public void SetDefaultState(TStateId stateId)
        {
            if (!mStates.ContainsKey(stateId))
            {
                throw new ArgumentException("Default state must be added before it can be selected.", nameof(stateId));
            }

            mDefaultStateId = stateId;
            mHasDefaultState = true;
        }

        public bool ChangeState(TStateId stateId)
        {
            IState<TStateId, TOwner> nextState;
            if (!mStates.TryGetValue(stateId, out nextState))
            {
                return false;
            }

            if (CurrentState != null && EqualityComparer<TStateId>.Default.Equals(CurrentState.StateId, stateId))
            {
                return false;
            }

            if (!nextState.CanEnter())
            {
                return false;
            }

            if (CurrentState != null)
            {
                // 如果 CurrentState 是子状态机，会在这里递归退出它当前激活的子状态。
                CurrentState.OnExit();
            }

            PreviousState = CurrentState;
            CurrentState = nextState;
            CurrentState.OnEnter();
            return true;
        }

        public void AddTransition(Transition<TStateId, TOwner> transition)
        {
            if (transition == null)
            {
                throw new ArgumentNullException(nameof(transition));
            }

            List<Transition<TStateId, TOwner>> transitions;
            if (!mTransitions.TryGetValue(transition.FromStateId, out transitions))
            {
                transitions = new List<Transition<TStateId, TOwner>>();
                mTransitions.Add(transition.FromStateId, transitions);
            }

            transitions.Add(transition);
            SortTransitions(transitions);
        }

        public void AddAnyTransition(Transition<TStateId, TOwner> transition)
        {
            if (transition == null)
            {
                throw new ArgumentNullException(nameof(transition));
            }

            mAnyTransitions.Add(transition);
            SortTransitions(mAnyTransitions);
        }

        public override void OnEnter()
        {
            base.OnEnter();

            // 作为 HFSM 子状态进入时，自动进入自己的默认子状态。
            if (mHasDefaultState)
            {
                ChangeState(mDefaultStateId);
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            // 每帧最多只允许一次自动过渡。
            // 如果本帧发生自动切换，新进入的状态会等到下一帧再执行 OnUpdate。
            if (!TryAutoTransition())
            {
                CurrentState?.OnUpdate();
            }
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            CurrentState?.OnFixedUpdate();
        }

        public override void OnExit()
        {
            if (CurrentState != null)
            {
                // 退出状态机时，先退出当前子状态，再退出状态机本身。
                CurrentState.OnExit();
                PreviousState = CurrentState;
                CurrentState = null;
            }

            base.OnExit();
        }

        private bool TryAutoTransition()
        {
            if (TryTransitions(mAnyTransitions))
            {
                return true;
            }

            if (CurrentState == null)
            {
                return false;
            }

            List<Transition<TStateId, TOwner>> transitions;
            if (!mTransitions.TryGetValue(CurrentState.StateId, out transitions))
            {
                return false;
            }

            return TryTransitions(transitions);
        }

        private bool TryTransitions(List<Transition<TStateId, TOwner>> transitions)
        {
            for (int i = 0; i < transitions.Count; i++)
            {
                var transition = transitions[i];
                if (CurrentState != null &&
                    EqualityComparer<TStateId>.Default.Equals(CurrentState.StateId, transition.ToStateId))
                {
                    continue;
                }

                if (transition.Tick(Owner) && ChangeState(transition.ToStateId))
                {
                    return true;
                }
            }

            return false;
        }

        private static void SortTransitions(List<Transition<TStateId, TOwner>> transitions)
        {
            transitions.Sort((left, right) => right.WeightOrder.CompareTo(left.WeightOrder));
        }

        /// <summary>
        /// C# 默认字符串入口。
        /// Debug.Log(fsm) 会走到这里，再转发给语义更明确的 ToDebugString()。
        /// </summary>
        public override string ToString() => ToDebugString();

        /// <summary>
        /// 外部调试入口，从当前状态机开始输出完整状态树。
        /// 根节点没有父节点，因此不输出树形连接符。
        /// </summary>
        public string ToDebugString()
        {
            var builder = new StringBuilder();
            builder.Append(StateId);
            builder.Append(BuildDebugTags(false, false, true));

            AppendChildrenDebugString(builder, string.Empty);
            return builder.ToString();
        }

        /// <summary>
        /// 组合模式递归输出入口。
        /// 当 StateMachine 作为另一个 StateMachine 的子状态时，由父节点调用该方法输出自身和内部子树。
        /// </summary>
        public override string ToDebugString(string indent, bool isLast, bool isCurrent, bool isDefault)
        {
            var builder = new StringBuilder();
            builder.Append(FormatDebugLine(indent, isLast, StateId, BuildDebugTags(isCurrent, isDefault, true)));

            AppendChildrenDebugString(builder, indent + (isLast ? "   " : "│  "));
            return builder.ToString();
        }

        /// <summary>
        /// 输出当前状态机持有的所有子状态。
        /// 每个子状态是否为 Current/Default，由当前状态机自身判断。
        /// </summary>
        private void AppendChildrenDebugString(StringBuilder builder, string childIndent)
        {
            if (mStates.Count == 0)
            {
                return;
            }

            var index = 0;

            foreach (var state in mStates.Values)
            {
                builder.AppendLine();

                var childIsLast = index == mStates.Count - 1;
                var childIsCurrent = CurrentState != null &&
                                     EqualityComparer<TStateId>.Default.Equals(CurrentState.StateId, state.StateId);
                var childIsDefault = mHasDefaultState &&
                                     EqualityComparer<TStateId>.Default.Equals(mDefaultStateId, state.StateId);

                builder.Append(ToChildDebugString(state, childIndent, childIsLast, childIsCurrent, childIsDefault));
                index++;
            }
        }

        /// <summary>
        /// 输出单个子状态。
        /// 继承 StateBase 的状态使用自己的 ToDebugString；直接实现 IState 的外部状态使用兜底格式。
        /// </summary>
        private string ToChildDebugString(
            IState<TStateId, TOwner> state,
            string indent,
            bool isLast,
            bool isCurrent,
            bool isDefault)
        {
            if (state is StateBase<TStateId, TOwner> stateBase)
            {
                return stateBase.ToDebugString(indent, isLast, isCurrent, isDefault);
            }

            // 作为了 StateMachine 的子状态，但它不是 StateBase，说明它是外部添加的 IState 实现类，不支持显示更多信息。
            return FormatDebugLine(indent, isLast, state.StateId, BuildDebugTags(isCurrent, isDefault, false));
        }
    }
}
