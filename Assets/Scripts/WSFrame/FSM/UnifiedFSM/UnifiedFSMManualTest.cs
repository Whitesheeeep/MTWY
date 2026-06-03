#if UNITY_EDITOR
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using WS_Modules.FSM;

namespace WS_Modules.Tests
{
    public class UnifiedFSMManualTest : MonoBehaviour
    {
        private enum TestState
        {
            Root,
            Idle,
            Run,
            Alert,
            Move,
            Walk,
            Sprint
        }

        private class TestOwner
        {
            public bool ShouldRun;
            public bool Alert;
            public bool UseHighPriority;
            public readonly List<string> Logs = new List<string>();
        }

        private class LogState : StateBase<TestState, TestOwner>
        {
            public LogState(TestState stateId) : base(stateId)
            {
            }

            public override void OnEnter()
            {
                Owner.Logs.Add(StateId + ".Enter");
            }

            public override void OnUpdate()
            {
                Owner.Logs.Add(StateId + ".Update");
            }

            public override void OnFixedUpdate()
            {
                Owner.Logs.Add(StateId + ".FixedUpdate");
            }

            public override void OnExit()
            {
                Owner.Logs.Add(StateId + ".Exit");
            }
        }

        [ShowInInspector, ReadOnly]
        private string mCurrentState;

        [ShowInInspector, ReadOnly]
        private string mLog;

        // Inspector 中直接显示 mRoot.ToDebugString() 的状态树结果，方便手动测试 HFSM 结构。
        [ShowInInspector, ReadOnly, MultiLineProperty(10)]
        private string mStateTree;

        private TestOwner mOwner;
        private StateMachine<TestState, TestOwner> mRoot;

        [Button("Build FSM", ButtonSizes.Large)]
        private void BuildFSM()
        {
            mOwner = new TestOwner();
            mRoot = new StateMachine<TestState, TestOwner>(TestState.Root, mOwner);

            mRoot.AddState(new LogState(TestState.Idle));
            mRoot.AddState(new LogState(TestState.Run));
            mRoot.AddState(new LogState(TestState.Alert));
            mRoot.AddState(CreateMoveMachine());
            mRoot.SetDefaultState(TestState.Idle);

            mRoot.AddTransition(new Transition<TestState, TestOwner>(TestState.Idle, TestState.Run)
                .AddCondition(owner => owner.ShouldRun));
            mRoot.AddTransition(new Transition<TestState, TestOwner>(TestState.Idle, TestState.Move, 10)
                .AddCondition(owner => owner.UseHighPriority));
            mRoot.AddAnyTransition(new Transition<TestState, TestOwner>(TestState.Idle, TestState.Alert, 100)
                .AddCondition(owner => owner.Alert));

            mRoot.OnEnter();
            RefreshView();
        }

        [Button("Active Change: Idle To Run", ButtonSizes.Medium)]
        private void ActiveChangeToRun()
        {
            EnsureBuilt();
            mRoot.ChangeState(TestState.Run);
            RefreshView();
        }

        [Button("Transition: Idle To Run", ButtonSizes.Medium)]
        private void TransitionIdleToRun()
        {
            EnsureBuilt();
            mRoot.ChangeState(TestState.Idle);
            mOwner.ShouldRun = true;
            mRoot.OnUpdate();
            mOwner.ShouldRun = false;
            RefreshView();
        }

        [Button("Any Transition To Alert", ButtonSizes.Medium)]
        private void AnyTransitionToAlert()
        {
            EnsureBuilt();
            mRoot.ChangeState(TestState.Run);
            mOwner.Alert = true;
            mRoot.OnUpdate();
            mOwner.Alert = false;
            RefreshView();
        }

        [Button("Priority Transition To Move", ButtonSizes.Medium)]
        private void PriorityTransitionToMove()
        {
            EnsureBuilt();
            mRoot.ChangeState(TestState.Idle);
            mOwner.ShouldRun = true;
            mOwner.UseHighPriority = true;
            mRoot.OnUpdate();
            mOwner.ShouldRun = false;
            mOwner.UseHighPriority = false;
            RefreshView();
        }

        [Button("HFSM Enter And Exit", ButtonSizes.Medium)]
        private void HFSMEnterAndExit()
        {
            EnsureBuilt();
            mRoot.ChangeState(TestState.Move);
            mRoot.OnUpdate();
            mRoot.ChangeState(TestState.Idle);
            RefreshView();
        }

        [Button("Missing State Returns False", ButtonSizes.Medium)]
        private void MissingStateReturnsFalse()
        {
            EnsureBuilt();
            bool result = mRoot.ChangeState((TestState)999);
            mOwner.Logs.Add("MissingStateResult=" + result);
            RefreshView();
        }

        // 手动触发 ToDebugString，确认状态树输出包含当前状态、默认状态和子状态机层级。
        [Button("Test ToDebugString State Tree", ButtonSizes.Medium)]
        private void TestToDebugStringStateTree()
        {
            EnsureBuilt();
            mStateTree = mRoot.ToDebugString();
            mOwner.Logs.Add("ToDebugStringResult:\n" + mStateTree);
            Debug.Log("[UnifiedFSMManualTest] State Tree\n" + mStateTree);
            RefreshView();
        }

        [Button("Clear Log", ButtonSizes.Small)]
        private void ClearLog()
        {
            EnsureBuilt();
            mOwner.Logs.Clear();
            RefreshView();
        }

        private StateMachine<TestState, TestOwner> CreateMoveMachine()
        {
            var move = new StateMachine<TestState, TestOwner>(TestState.Move);
            move.AddState(new LogState(TestState.Walk));
            move.AddState(new LogState(TestState.Sprint));
            move.SetDefaultState(TestState.Walk);
            move.AddTransition(new Transition<TestState, TestOwner>(TestState.Walk, TestState.Sprint)
                .AddCondition(owner => owner.ShouldRun));
            return move;
        }

        private void EnsureBuilt()
        {
            if (mRoot == null)
            {
                BuildFSM();
            }
        }

        private void RefreshView()
        {
            mCurrentState = mRoot != null && mRoot.CurrentState != null
                ? mRoot.CurrentState.StateId.ToString()
                : "None";
            mStateTree = mRoot != null ? mRoot.ToDebugString() : string.Empty;
            mLog = mOwner != null ? string.Join("\n", mOwner.Logs) : string.Empty;
            Debug.Log("[UnifiedFSMManualTest]\n" + mLog);
        }
    }
}
#endif
