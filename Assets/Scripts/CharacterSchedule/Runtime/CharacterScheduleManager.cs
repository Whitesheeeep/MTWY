using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Gameplay.TimeSystem;
using UnityEngine;
using WS_Modules.CustomEventSystem;
using WS_Modules.Singleton;

namespace GameData.CharacterSchedule
{
    /// <summary>
    /// 角色日程运行时中心。
    /// 负责维护所有角色的逻辑状态、评估日程、规划移动目标，并接收当前场景 Agent 的到达回报。
    /// </summary>
    public sealed class CharacterScheduleManager : SingletonBase<CharacterScheduleManager>
    {
        /// <summary>
        /// 所有角色的运行时状态。离线角色也只存在于这里。
        /// </summary>
        private readonly Dictionary<string, CharacterRuntimeState> states =
            new Dictionary<string, CharacterRuntimeState>(StringComparer.Ordinal);

        /// <summary>
        /// characterId 到日程配置的索引。
        /// </summary>
        private readonly Dictionary<string, CharacterSchedule_SO> schedules =
            new Dictionary<string, CharacterSchedule_SO>(StringComparer.Ordinal);

        /// <summary>
        /// 日程系统自己的运行时 flag。外部系统通过 SetFlag 写入。
        /// </summary>
        private readonly Dictionary<string, bool> flags =
            new Dictionary<string, bool>(StringComparer.Ordinal);

        private readonly CharacterScheduleServices services = new CharacterScheduleServices();
        private readonly CharacterScheduleEvaluationService evaluationService = new CharacterScheduleEvaluationService();
        private CharacterDefinitionDatabase definitionDatabase;
        private IUnRegister minuteChangedUnregister;
        private IUnRegister hourChangedUnregister;
        private IUnRegister dayStartedUnregister;
        private bool evaluateAllRunning;
        private bool evaluateAllRequested;
        private int evaluationGeneration;

        private CharacterScheduleManager()
        {
        }

        /// <summary>
        /// 角色状态变化事件。AgentManager 使用该事件同步当前场景实体。
        /// </summary>
        public event Action<CharacterRuntimeState> StateChanged;

        /// <summary>
        /// 初始化角色定义、日程配置和初始运行时状态。
        /// 通常由 CharacterScheduleRegisterNode 在 ConfigInstaller 流程中调用。
        /// </summary>
        public void Initialize(
            CharacterDefinitionDatabase definitions,
            IEnumerable<CharacterSchedule_SO> sourceSchedules)
        {
            evaluationGeneration++;
            definitionDatabase = definitions ?? new CharacterDefinitionDatabase(null);
            states.Clear();
            schedules.Clear();
            flags.Clear();

            RegisterServices();
            LoadSchedules(sourceSchedules);
            CreateDefaultStates();
            SubscribeGameTimeEvents();
            EvaluateAllAsync().Forget();
        }

        /// <summary>
        /// 设置日程运行时 flag，并触发全量重新评估。
        /// </summary>
        public void SetFlag(string flagId, bool value)
        {
            if (string.IsNullOrWhiteSpace(flagId))
            {
                return;
            }

            flags[flagId] = value;
            EvaluateAllAsync().Forget();
        }

        /// <summary>
        /// 查询指定角色的运行时状态。
        /// </summary>
        public bool TryGetState(string characterId, out CharacterRuntimeState state)
        {
            state = null;
            return !string.IsNullOrWhiteSpace(characterId) && states.TryGetValue(characterId, out state);
        }

        /// <summary>
        /// 获取当前位于指定地图的角色状态集合。AgentManager 用它生成当前场景实体。
        /// </summary>
        public IEnumerable<CharacterRuntimeState> GetCharactersInMap(string mapId)
        {
            if (string.IsNullOrWhiteSpace(mapId))
            {
                yield break;
            }

            foreach (CharacterRuntimeState state in states.Values)
            {
                if (state.IsInMap(mapId))
                {
                    yield return state;
                }
            }
        }

        /// <summary>
        /// Agent 到达路径中的一个 cell 后回报，用于推进逻辑位置和剩余路径。
        /// </summary>
        public void ReportAgentReachedCell(string characterId, Vector3Int cell)
        {
            if (!TryGetState(characterId, out CharacterRuntimeState state))
            {
                return;
            }

            state.currentCell = cell;
            if (state.remainingPath.Count > 0 && state.remainingPath[0] == cell)
            {
                state.remainingPath.RemoveAt(0);
            }

            NotifyStateChanged(state);
        }

        /// <summary>
        /// Agent 完成本次路径后回报。跨地图第一版会在这里把逻辑位置结算到目标地图目标格。
        /// </summary>
        public void ReportMoveArrived(string characterId)
        {
            if (!TryGetState(characterId, out CharacterRuntimeState state))
            {
                return;
            }

            CompleteCurrentSegment(state);
        }

        /// <summary>
        /// 全量重新评估所有角色日程。第一版不做条件索引，优先保证行为正确。
        /// </summary>
        public async UniTask EvaluateAllAsync()
        {
            if (evaluateAllRunning)
            {
                evaluateAllRequested = true;
                return;
            }

            evaluateAllRunning = true;
            try
            {
                do
                {
                    evaluateAllRequested = false;
                    int runGeneration = evaluationGeneration;
                    List<CharacterRuntimeState> snapshot = new List<CharacterRuntimeState>(states.Values);
                    for (int i = 0; i < snapshot.Count; i++)
                    {
                        if (runGeneration != evaluationGeneration)
                        {
                            break;
                        }

                        await EvaluateCharacterAsync(snapshot[i]);
                    }
                }
                while (evaluateAllRequested);
            }
            finally
            {
                evaluateAllRunning = false;
            }
        }

        /// <summary>
        /// 推进不在当前场景中的移动角色。生产环境由游戏分钟事件调用，测试工具也可以手动调用同一条链路。
        /// </summary>
        public async UniTask AdvanceOfflineMovingCharactersAsync(int minutes = 1)
        {
            if (minutes <= 0)
            {
                return;
            }

            for (int i = 0; i < minutes; i++)
            {
                Debug.Log("Advance offline moving characters");
                await AdvanceOfflineMovingCharactersOnceAsync();
            }
        }

        private void RegisterServices()
        {
            services.Clear();
            services.Register<ICharacterScheduleEvaluationService>(evaluationService);
            services.Register<ICharacterScheduleTimeService>(new CharacterScheduleTimeService());
            services.Register<ICharacterScheduleFlagService>(new CharacterScheduleFlagService(flags));
            services.Register<ICharacterScheduleMapService>(new CharacterScheduleMapService());
        }

        private void LoadSchedules(IEnumerable<CharacterSchedule_SO> sourceSchedules)
        {
            if (sourceSchedules == null)
            {
                return;
            }

            foreach (CharacterSchedule_SO schedule in sourceSchedules)
            {
                if (schedule == null || string.IsNullOrWhiteSpace(schedule.characterId))
                {
                    continue;
                }

                schedules[schedule.characterId] = schedule;
            }
        }

        private void CreateDefaultStates()
        {
            if (definitionDatabase == null)
            {
                return;
            }

            foreach (CharacterDefinition_SO definition in GetDefinitions())
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.characterId))
                {
                    continue;
                }

                states[definition.characterId] = new CharacterRuntimeState
                {
                    characterId = definition.characterId,
                    currentMapId = definition.defaultMapId,
                    currentCell = definition.defaultCell,
                    activeEntryId = string.Empty,
                    moveState = CharacterMoveState.Idle,
                    targetMapId = definition.defaultMapId,
                    targetCell = definition.defaultCell,
                    moveSpeed = definition.defaultMoveSpeed
                };
            }
        }

        private IEnumerable<CharacterDefinition_SO> GetDefinitions() => definitionDatabase != null ? definitionDatabase.GetAll() : Array.Empty<CharacterDefinition_SO>();
        private void SubscribeGameTimeEvents()
        {
            minuteChangedUnregister?.UnRegister();
            hourChangedUnregister?.UnRegister();
            dayStartedUnregister?.UnRegister();
            minuteChangedUnregister = null;
            hourChangedUnregister = null;
            dayStartedUnregister = null;

            GameTimeManager manager = GameTimeManager.Instance;
            if (manager == null)
            {
                Debug.Log("[CharacterScheduleManager] GameTimeManager instance not found. Game time events will not be subscribed.");
                return;
            }

            minuteChangedUnregister = manager.RegisterMinuteChanged(_ => OnMinuteChangedAsync().Forget());
            hourChangedUnregister = manager.RegisterHourChanged(_ => EvaluateAllAsync().Forget());
            dayStartedUnregister = manager.RegisterDayStarted(_ => EvaluateAllAsync().Forget());
        }

        private async UniTask OnMinuteChangedAsync()
        {
            Debug.Log("[CharacterScheduleManager] 推荐游戏时间变化，推进离线移动角色");
            await AdvanceOfflineMovingCharactersAsync();
            await EvaluateAllAsync();
        }

        private async UniTask EvaluateCharacterAsync(CharacterRuntimeState state)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.characterId))
            {
                return;
            }

            if (!schedules.TryGetValue(state.characterId, out CharacterSchedule_SO schedule) ||
                schedule.entries == null ||
                schedule.entries.Count == 0)
            {
                SetIdle(state);
                return;
            }

            CharacterScheduleEntry selected = SelectBestEntry(schedule, state);
            if (selected == null)
            {
                SetIdle(state);
                return;
            }

            if (string.Equals(state.activeEntryId, selected.entryId, StringComparison.Ordinal) &&
                state.moveState == CharacterMoveState.Moving)
            {
                return;
            }

            await StartEntryAsync(state, selected);
        }

        private CharacterScheduleEntry SelectBestEntry(CharacterSchedule_SO schedule, CharacterRuntimeState state)
        {
            CharacterScheduleEntry best = null;
            for (int i = 0; i < schedule.entries.Count; i++)
            {
                CharacterScheduleEntry entry = schedule.entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.entryId))
                {
                    continue;
                }

                if (!AreConditionsMet(state, entry))
                {
                    continue;
                }

                if (best == null ||
                    entry.priority > best.priority ||
                    (entry.priority == best.priority && string.Equals(entry.entryId, state.activeEntryId, StringComparison.Ordinal)))
                {
                    best = entry;
                }
            }

            return best;
        }

        private bool AreConditionsMet(CharacterRuntimeState state, CharacterScheduleEntry entry)
        {
            if (entry.conditions == null || entry.conditions.Count == 0)
            {
                return true;
            }

            // 设置当前进行评判的上下文角色
            evaluationService.Begin(state.characterId, state, entry);
            try
            {
                for (int i = 0; i < entry.conditions.Count; i++)
                {
                    CharacterScheduleCondition condition = entry.conditions[i];
                    if (condition == null)
                    {
                        continue;
                    }

                    if (!condition.IsMet(services, out _))
                    {
                        return false;
                    }
                }
            }
            finally
            {
                evaluationService.End();
            }
            return true;
        }

        private async UniTask StartEntryAsync(CharacterRuntimeState state, CharacterScheduleEntry entry)
        {
            if (!TryGetDefinition(state.characterId, out CharacterDefinition_SO definition))
            {
                SetBlocked(state, $"Missing character definition: {state.characterId}");
                return;
            }

            if (string.IsNullOrWhiteSpace(entry.targetMapId))
            {
                SetBlocked(state, $"Missing targetMapId for entry: {entry.entryId}");
                return;
            }

            state.activeEntryId = entry.entryId;
            state.targetMapId = entry.targetMapId;
            state.targetCell = entry.targetCell;
            state.moveSpeed = entry.moveSpeedOverride > 0f ? entry.moveSpeedOverride : definition.defaultMoveSpeed;
            state.blockedReason = string.Empty;

            if (string.Equals(state.currentMapId, state.targetMapId, StringComparison.Ordinal) &&
                state.currentCell == state.targetCell)
            {
                state.moveState = CharacterMoveState.Arrived;
                state.remainingPath.Clear();
                state.pendingSegments.Clear();
                state.offlineMoveDistanceCarry = 0f;
                NotifyStateChanged(state);
                return;
            }

            CharacterSchedulePlanResult result = await CharacterSchedulePlanner.PlanAsync(
                state.currentMapId,
                state.currentCell,
                state.targetMapId,
                state.targetCell);

            if (!result.Success)
            {
                SetBlocked(state, result.FailureReason);
                return;
            }

            ApplyPlanResult(state, result);
        }

        private void ApplyPlanResult(CharacterRuntimeState state, CharacterSchedulePlanResult result)
        {
            state.pendingSegments.Clear();
            state.offlineMoveDistanceCarry = 0f;

            if (result == null || !result.Success || result.FirstSegment == null)
            {
                SetBlocked(state, result != null ? result.FailureReason : "Schedule planner returned an empty result.");
                return;
            }

            for (int i = 1; i < result.Segments.Count; i++)
            {
                state.pendingSegments.Add(result.Segments[i]);
            }

            ApplySegment(state, result.FirstSegment);
            LogPlannedPath(state, result);

            if (state.remainingPath.Count == 0)
            {
                CompleteCurrentSegment(state);
                return;
            }

            state.moveState = CharacterMoveState.Moving;
            NotifyStateChanged(state);
        }

        private void CompleteCurrentSegment(CharacterRuntimeState state)
        {
            state.offlineMoveDistanceCarry = 0f;

            while (state.pendingSegments.Count > 0)
            {
                CharacterMoveSegment nextSegment = state.pendingSegments[0];
                state.pendingSegments.RemoveAt(0);
                ApplySegment(state, nextSegment);

                if (state.remainingPath.Count > 0)
                {
                    state.moveState = CharacterMoveState.Moving;
                    state.blockedReason = string.Empty;
                    NotifyStateChanged(state);
                    return;
                }
            }

            state.currentMapId = state.targetMapId;
            state.currentCell = state.targetCell;
            state.remainingPath.Clear();
            state.moveState = CharacterMoveState.Arrived;
            state.blockedReason = string.Empty;
            NotifyStateChanged(state);
        }

        private static void ApplySegment(CharacterRuntimeState state, CharacterMoveSegment segment)
        {
            state.currentMapId = segment.mapId;
            state.currentCell = segment.startCell;
            SetRemainingPath(state, segment.path);
            if (state.remainingPath.Count == 0)
            {
                state.currentCell = segment.targetCell;
            }
        }

        private async UniTask AdvanceOfflineMovingCharactersOnceAsync()
        {
            List<CharacterRuntimeState> snapshot = new List<CharacterRuntimeState>(states.Values);
            for (int i = 0; i < snapshot.Count; i++)
            {
                CharacterRuntimeState state = snapshot[i];
                if (state == null ||
                    state.moveState != CharacterMoveState.Moving ||
                    IsStateInCurrentScene(state))
                {
                    continue;
                }

                await AdvanceOfflineStateAsync(state);
            }
        }

        private async UniTask AdvanceOfflineStateAsync(CharacterRuntimeState state)
        {
            if (state.moveSpeed <= 0f)
            {
                SetBlocked(state, $"Move speed must be greater than 0. Character:{state.characterId}");
                return;
            }

            float availableDistance = state.moveSpeed + state.offlineMoveDistanceCarry;
            state.offlineMoveDistanceCarry = 0f;
            bool changed = false;

            while (availableDistance > 0f && state.moveState == CharacterMoveState.Moving)
            {
                if (IsStateInCurrentScene(state))
                {
                    break;
                }

                if (state.remainingPath.Count == 0)
                {
                    CompleteCurrentSegment(state);
                    changed = true;
                    continue;
                }

                Vector3Int nextCell = state.remainingPath[0];
                float stepDistance = await GetCellStepDistanceAsync(state.currentMapId, state.currentCell, nextCell);
                if (stepDistance < 0f)
                {
                    SetBlocked(state, $"Cannot resolve offline move distance. Map:{state.currentMapId}, From:{state.currentCell}, To:{nextCell}");
                    return;
                }

                if (availableDistance + 0.0001f < stepDistance)
                {
                    state.offlineMoveDistanceCarry = availableDistance;
                    break;
                }

                availableDistance -= stepDistance;
                state.currentCell = nextCell;
                state.remainingPath.RemoveAt(0);
                changed = true;
            }

            if (changed && state.moveState == CharacterMoveState.Moving)
            {
                NotifyStateChanged(state);
            }
        }

        private static async UniTask<float> GetCellStepDistanceAsync(
            string mapId,
            Vector3Int from,
            Vector3Int to)
        {
            MapGridManager mapGrid = MapGridManager.Instance;
            if (!mapGrid.TryGetMapCellSize(mapId, out Vector3 cellSize))
            {
                if (!await mapGrid.EnsureLoadedAsync(mapId) ||
                    !mapGrid.TryGetMapCellSize(mapId, out cellSize))
                {
                    return -1f;
                }
            }

            float dx = Mathf.Abs(to.x - from.x) * Mathf.Abs(cellSize.x);
            float dy = Mathf.Abs(to.y - from.y) * Mathf.Abs(cellSize.y);
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            if (distance <= 0f)
            {
                distance = Mathf.Max(Mathf.Abs(cellSize.x), Mathf.Abs(cellSize.y), 1f);
            }

            return distance;
        }

        private static void LogPlannedPath(CharacterRuntimeState state, CharacterSchedulePlanResult result)
        {
            if (state == null)
            {
                return;
            }

            Debug.Log(
                $"[CharacterScheduleManager] Planned path. " +
                $"characterId={state.characterId}, entry={state.activeEntryId}, " +
                $"from=({state.currentMapId}, {state.currentCell}), " +
                $"target=({state.targetMapId}, {state.targetCell}), " +
                $"segments={FormatSegments(result != null ? result.Segments : null)}, " +
                $"remainingPath={FormatPath(state.remainingPath)}, pendingSegments={state.pendingSegments.Count}");
        }

        private static string FormatSegments(IReadOnlyList<CharacterMoveSegment> segments)
        {
            if (segments == null || segments.Count == 0)
            {
                return "[]";
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append('[');
            for (int i = 0; i < segments.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                CharacterMoveSegment segment = segments[i];
                builder.Append(segment.mapId);
                builder.Append(':');
                builder.Append(segment.startCell);
                builder.Append("->");
                builder.Append(segment.targetCell);
                builder.Append(' ');
                builder.Append(FormatPath(segment.path));
            }

            builder.Append(']');
            return builder.ToString();
        }

        private static string FormatPath(IReadOnlyList<Vector3Int> path)
        {
            if (path == null || path.Count == 0)
            {
                return "[]";
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append('[');
            for (int i = 0; i < path.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" -> ");
                }

                builder.Append(path[i]);
            }

            builder.Append(']');
            return builder.ToString();
        }

        private bool TryGetDefinition(string characterId, out CharacterDefinition_SO definition)
        {
            definition = null;
            return definitionDatabase != null && definitionDatabase.TryGet(characterId, out definition);
        }

        private static bool IsStateInCurrentScene(CharacterRuntimeState state)
        {
            return MapGridManager.Instance.HasCurrentGrid &&
                   string.Equals(state.currentMapId, MapGridManager.Instance.CurrentMapId, StringComparison.Ordinal);
        }

        private static void SetRemainingPath(CharacterRuntimeState state, List<Vector3Int> path)
        {
            state.remainingPath.Clear();
            if (path == null || path.Count == 0)
            {
                return;
            }

            // Pathfinding 返回值通常包含起点，Agent 只需要后续要走到的格子。
            int startIndex = path[0] == state.currentCell ? 1 : 0;
            for (int i = startIndex; i < path.Count; i++)
            {
                state.remainingPath.Add(path[i]);
            }
        }

        private void SetIdle(CharacterRuntimeState state)
        {
            if (state.moveState == CharacterMoveState.Idle && string.IsNullOrWhiteSpace(state.activeEntryId))
            {
                return;
            }

            state.activeEntryId = string.Empty;
            state.moveState = CharacterMoveState.Idle;
            state.remainingPath.Clear();
            state.pendingSegments.Clear();
            state.offlineMoveDistanceCarry = 0f;
            state.blockedReason = string.Empty;
            NotifyStateChanged(state);
        }

        private void SetBlocked(CharacterRuntimeState state, string reason)
        {
            state.moveState = CharacterMoveState.Blocked;
            state.blockedReason = reason;
            state.remainingPath.Clear();
            state.pendingSegments.Clear();
            state.offlineMoveDistanceCarry = 0f;
            NotifyStateChanged(state);
        }

        private void SetOfflineArrived(CharacterRuntimeState state)
        {
            state.currentMapId = state.targetMapId;
            state.currentCell = state.targetCell;
            state.moveState = CharacterMoveState.Arrived;
            state.remainingPath.Clear();
            state.pendingSegments.Clear();
            state.offlineMoveDistanceCarry = 0f;
            NotifyStateChanged(state);
        }

        private void NotifyStateChanged(CharacterRuntimeState state)
        {
            StateChanged?.Invoke(state);
        }
    }
}
