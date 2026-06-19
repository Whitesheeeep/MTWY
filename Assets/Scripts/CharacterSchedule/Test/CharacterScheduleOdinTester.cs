#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Gameplay.TimeSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameData.CharacterSchedule
{
    /// <summary>
    /// Character Schedule 的 Odin 手动测试入口。
    /// 这个组件只负责调用真实运行时 API 触发状态变化，并打印当前状态，不承担完整自动化验收。
    /// </summary>
    public sealed class CharacterScheduleOdinTester : MonoBehaviour
    {
        [Title("Trigger")]
        [SerializeField] private string testCharacterId = "npc_old_man";
        [SerializeField] private string testFlagId = "schedule.oldman.go_square";
        [SerializeField] private bool testFlagValue;

        [Title("Offline Time")]
        [SerializeField, Min(1)] private int testAdvanceMinutes = 1;

        [Title("Last Result")]
        [ShowInInspector, ReadOnly, MultiLineProperty(24)]
        private string lastResult = "Ready.";

        /// <summary>
        /// 打印当前地图、角色状态和场景 Agent 状态。
        /// </summary>
        [Button("打印角色状态", ButtonSizes.Large)]
        public void PrintCurrentState()
        {
            ReportInfo("打印角色状态", BuildRuntimeText());
        }

        /// <summary>
        /// 只调用 CharacterScheduleManager.SetFlag，让 Schedule 自己评估和驱动状态。
        /// </summary>
        [Button("设置 Flag", ButtonSizes.Large)]
        public void SetScheduleFlag()
        {
            if (string.IsNullOrWhiteSpace(testFlagId))
            {
                ReportWarning("设置 Flag", "testFlagId 为空，未执行。");
                return;
            }

            CharacterScheduleManager.Instance.SetFlag(testFlagId, testFlagValue);
            ReportInfo("设置 Flag", $"{testFlagId} = {testFlagValue}\n\n{BuildRuntimeText()}");
        }

        // [Button("输出所有运行时的角色状态")]
        // public void Debug

        /// <summary>
        /// OldMan 去 Home：设置 schedule.oldman.go_square 为 true。
        /// </summary>
        [Button("OldMan 去 Home", ButtonSizes.Medium)]
        public void SetOldManGoHomeFlag()
        {
            testCharacterId = "npc_old_man";
            testFlagId = "schedule.oldman.go_square";
            testFlagValue = true;
            SetScheduleFlag();
        }

        /// <summary>
        /// OldMan 回 MainScene：设置 schedule.oldman.go_square 为 false。
        /// </summary>
        [Button("OldMan 回 MainScene", ButtonSizes.Medium)]
        public void SetOldManGoMainSceneFlag()
        {
            testCharacterId = "npc_old_man";
            testFlagId = "schedule.oldman.go_square";
            testFlagValue = false;
            SetScheduleFlag();
        }

        /// <summary>
        /// 使用 GameTimeManager 推进游戏分钟，验证离线角色后台移动。
        /// </summary>
        [Button("推进游戏分钟", ButtonSizes.Large)]
        public void AdvanceGameMinutes()
        {
            if (GameTimeManager.Instance == null)
            {
                ReportWarning("推进游戏分钟", "当前运行时没有 GameTimeManager。");
                return;
            }

            GameTimeManager.Instance.AdvanceMinutes(testAdvanceMinutes);
            ReportInfo("推进游戏分钟", $"AdvanceMinutes({testAdvanceMinutes})\n\n{BuildRuntimeText()}");
        }

        /// <summary>
        /// 直接调用 ScheduleManager 的离线推进 API，方便不想改动全局时间时观察离线移动。
        /// </summary>
        [Button("只推进离线角色", ButtonSizes.Medium)]
        public void AdvanceOfflineCharactersOnly()
        {
            AdvanceOfflineCharactersOnlyAsync().Forget();
        }

        /// <summary>
        /// 刷新当前场景可见 Agent，用于验证进入地图后角色生成或回收。
        /// </summary>
        [Button("刷新当前场景 Agent", ButtonSizes.Large)]
        public void RefreshVisibleAgents()
        {
            RefreshVisibleAgentsAsync().Forget();
        }

        private async UniTaskVoid AdvanceOfflineCharactersOnlyAsync()
        {
            await CharacterScheduleManager.Instance.AdvanceOfflineMovingCharactersAsync(testAdvanceMinutes);
            ReportInfo("只推进离线角色", $"AdvanceOfflineMovingCharactersAsync({testAdvanceMinutes})\n\n{BuildRuntimeText()}");
        }

        private async UniTaskVoid RefreshVisibleAgentsAsync()
        {
            await CharacterAgentManager.Instance.RefreshVisibleAgentsAsync();
            ReportInfo("刷新当前场景 Agent", BuildRuntimeText());
        }

        private string BuildRuntimeText()
        {
            var builder = new StringBuilder();
            builder.AppendLine("MapGrid:");
            builder.AppendLine($"currentMapId={MapGridManager.Instance.CurrentMapId}");
            builder.AppendLine($"hasGrid={MapGridManager.Instance.HasCurrentGrid}");
            builder.AppendLine();
            builder.AppendLine("Input:");
            builder.AppendLine($"characterId={testCharacterId}");
            builder.AppendLine($"flag={testFlagId} -> {testFlagValue}");
            builder.AppendLine($"advanceMinutes={testAdvanceMinutes}");
            builder.AppendLine();

            if (CharacterScheduleManager.Instance.TryGetState(testCharacterId, out CharacterRuntimeState state))
            {
                builder.Append(FormatState(state));
                builder.AppendLine();
                builder.AppendLine(FormatAgent(testCharacterId));
            }
            else
            {
                builder.AppendLine($"Character state not found. characterId={testCharacterId}");
            }

            return builder.ToString();
        }

        private static string FormatState(CharacterRuntimeState state)
        {
            if (state == null)
            {
                return "Character state: null";
            }

            var builder = new StringBuilder();
            builder.AppendLine("Character state:");
            builder.AppendLine($"characterId={state.characterId}");
            builder.AppendLine($"currentMapId={state.currentMapId}");
            builder.AppendLine($"currentCell={state.currentCell}");
            builder.AppendLine($"activeEntryId={state.activeEntryId}");
            builder.AppendLine($"moveState={state.moveState}");
            builder.AppendLine($"targetMapId={state.targetMapId}");
            builder.AppendLine($"targetCell={state.targetCell}");
            builder.AppendLine($"remainingPath={FormatCells(state.remainingPath)}");
            builder.AppendLine($"pendingSegments={FormatSegments(state.pendingSegments)}");
            builder.AppendLine($"moveSpeed={state.moveSpeed}");
            builder.AppendLine($"offlineMoveDistanceCarry={state.offlineMoveDistanceCarry:0.0000}");
            builder.AppendLine($"blockedReason={state.blockedReason}");
            return builder.ToString();
        }

        private static string FormatAgent(string characterId)
        {
            return TryFindAgent(characterId, out CharacterAgent agent)
                ? $"Agent: active, name={agent.name}, isMoving={agent.IsMoving}, position={agent.transform.position}"
                : "Agent: not spawned";
        }

        private static bool TryFindAgent(string characterId, out CharacterAgent agent)
        {
            CharacterAgent[] agents = UnityEngine.Object.FindObjectsOfType<CharacterAgent>(true);
            for (int i = 0; i < agents.Length; i++)
            {
                if (agents[i] != null &&
                    string.Equals(agents[i].CharacterId, characterId, StringComparison.Ordinal))
                {
                    agent = agents[i];
                    return true;
                }
            }

            agent = null;
            return false;
        }

        private static string FormatSegments(IReadOnlyList<CharacterMoveSegment> segments)
        {
            if (segments == null || segments.Count == 0)
            {
                return "[]";
            }

            var builder = new StringBuilder();
            builder.Append('[');
            for (int i = 0; i < segments.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                CharacterMoveSegment segment = segments[i];
                builder.Append(segment.mapId);
                builder.Append(':');
                builder.Append(segment.startCell);
                builder.Append("->");
                builder.Append(segment.targetCell);
                builder.Append(" path=");
                builder.Append(segment.path != null ? segment.path.Count : 0);
            }

            builder.Append(']');
            return builder.ToString();
        }

        private static string FormatCells(IReadOnlyList<Vector3Int> cells)
        {
            if (cells == null || cells.Count == 0)
            {
                return "[]";
            }

            var builder = new StringBuilder();
            builder.Append('[');
            for (int i = 0; i < cells.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(cells[i]);
            }

            builder.Append(']');
            return builder.ToString();
        }

        private void ReportInfo(string title, string message)
        {
            lastResult = $"{title}\n{message}";
            Debug.Log($"[CharacterScheduleOdinTester] {lastResult}");
        }

        private void ReportWarning(string title, string message)
        {
            lastResult = $"{title}\n{message}";
            Debug.LogWarning($"[CharacterScheduleOdinTester] {lastResult}");
        }
    }
}
#endif
