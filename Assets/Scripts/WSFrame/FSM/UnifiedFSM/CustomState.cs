using System;

namespace WS_Modules.FSM
{
    /// <summary>
    /// 链式状态，保留类似 QFramework CustomState 的便捷写法。
    /// 复杂业务推荐继承 StateBase；简单状态、示例或临时组合可以使用该类。
    /// </summary>
    public class CustomState<TStateId, TOwner> : StateBase<TStateId, TOwner>
    {
        private Func<TOwner, bool> mCanEnter;
        private Action<CustomState<TStateId, TOwner>> mOnEnter;
        private Action<CustomState<TStateId, TOwner>> mOnUpdate;
        private Action<CustomState<TStateId, TOwner>> mOnFixedUpdate;
        private Action<CustomState<TStateId, TOwner>> mOnExit;

        public CustomState(TStateId stateId) : base(stateId)
        {
        }

        public CustomState<TStateId, TOwner> OnCanEnter(Func<TOwner, bool> canEnter)
        {
            mCanEnter = canEnter;
            return this;
        }

        public CustomState<TStateId, TOwner> OnEnter(Action<CustomState<TStateId, TOwner>> onEnter)
        {
            mOnEnter = onEnter;
            return this;
        }

        public CustomState<TStateId, TOwner> OnUpdate(Action<CustomState<TStateId, TOwner>> onUpdate)
        {
            mOnUpdate = onUpdate;
            return this;
        }

        public CustomState<TStateId, TOwner> OnFixedUpdate(Action<CustomState<TStateId, TOwner>> onFixedUpdate)
        {
            mOnFixedUpdate = onFixedUpdate;
            return this;
        }

        public CustomState<TStateId, TOwner> OnExit(Action<CustomState<TStateId, TOwner>> onExit)
        {
            mOnExit = onExit;
            return this;
        }

        public override bool CanEnter()
        {
            // 未设置进入条件时，默认允许进入。
            return mCanEnter == null || mCanEnter(Owner);
        }

        public override void OnEnter()
        {
            mOnEnter?.Invoke(this);
        }

        public override void OnUpdate()
        {
            mOnUpdate?.Invoke(this);
        }

        public override void OnFixedUpdate()
        {
            mOnFixedUpdate?.Invoke(this);
        }

        public override void OnExit()
        {
            mOnExit?.Invoke(this);
        }
    }
}
