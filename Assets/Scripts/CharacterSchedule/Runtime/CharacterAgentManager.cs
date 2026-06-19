using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameData;
using UnityEngine;
using WS_Modules.CustomEventSystem;
using WS_Modules.Pooling;
using WS_Modules.SceneModule;
using WS_Modules.Singleton;

namespace GameData.CharacterSchedule
{
    /// <summary>
    /// 管理当前地图内角色 Agent 的生成、回收和表现同步。
    /// 它不保存角色逻辑状态，只根据 CharacterScheduleManager 的状态创建或回收场景实体。
    /// </summary>
    public sealed class CharacterAgentManager : AutoSingletonMonoBase<CharacterAgentManager>
    {
        /// <summary>
        /// 当前场景角色实体的父节点。为空时运行时自动创建。
        /// </summary>
        [SerializeField] private Transform agentRoot;

        /// <summary>
        /// 场景加载后等待 MapGridRuntimeLoader 完成当前 Grid 绑定的最大帧数。
        /// </summary>
        [SerializeField] private int gridReadyWaitFrames = 120;

        /// <summary>
        /// 当前场景内已经生成的 Agent，key 为 characterId。
        /// </summary>
        private readonly Dictionary<string, CharacterAgent> activeAgents =
            new Dictionary<string, CharacterAgent>(System.StringComparer.Ordinal);

        private IUnRegister sceneLoadStartedUnregister;
        private IUnRegister sceneLoadSucceededUnregister;
        private bool stateEventRegistered;
        private int refreshVersion;

        /// <summary>
        /// 初始化事件订阅，并尝试刷新当前场景角色实体。
        /// </summary>
        public override void Init()
        {
            sceneLoadStartedUnregister?.UnRegister();
            sceneLoadSucceededUnregister?.UnRegister();

            sceneLoadStartedUnregister = SceneSystem.RegisterLoadStarted(_ => OnSceneLoadStarted());
            sceneLoadSucceededUnregister = SceneSystem.RegisterLoadSucceeded(_ => RefreshVisibleAgentsAsync().Forget());

            if (!stateEventRegistered)
            {
                CharacterScheduleManager.Instance.StateChanged += OnStateChanged;
                stateEventRegistered = true;
            }

            RefreshVisibleAgentsAsync().Forget();
        }

        protected override void OnDestroy()
        {
            sceneLoadStartedUnregister?.UnRegister();
            sceneLoadSucceededUnregister?.UnRegister();
            sceneLoadStartedUnregister = null;
            sceneLoadSucceededUnregister = null;

            if (stateEventRegistered)
            {
                CharacterScheduleManager.Instance.StateChanged -= OnStateChanged;
                stateEventRegistered = false;
            }

            refreshVersion++;
            base.OnDestroy();
        }

        private void OnSceneLoadStarted()
        {
            refreshVersion++;
            RecycleAllAgents();
        }

        /// <summary>
        /// 根据当前 MapGridManager.CurrentMapId 重建当前场景可见 Agent 集合。
        /// </summary>
        public async UniTask RefreshVisibleAgentsAsync()
        {
            int currentRefreshVersion = ++refreshVersion;
            if (!await WaitForCurrentGridAsync())
            {
                if (currentRefreshVersion == refreshVersion)
                {
                    RecycleAllAgents();
                }

                return;
            }

            if (currentRefreshVersion != refreshVersion)
            {
                return;
            }

            string currentMapId = MapGridManager.Instance.CurrentMapId;
            RecycleAgentsOutsideMap(currentMapId);

            foreach (CharacterRuntimeState state in CharacterScheduleManager.Instance.GetCharactersInMap(currentMapId))
            {
                if (currentRefreshVersion != refreshVersion)
                {
                    return;
                }

                EnsureAgentForState(state);
            }
        }

        private async UniTask<bool> WaitForCurrentGridAsync()
        {
            for (int i = 0; i < gridReadyWaitFrames; i++)
            {
                if (MapGridManager.Instance.HasCurrentGrid &&
                    !string.IsNullOrWhiteSpace(MapGridManager.Instance.CurrentMapId))
                {
                    return true;
                }

                await UniTask.Yield();
            }

            return false;
        }

        private void OnStateChanged(CharacterRuntimeState state)
        {
            if (state == null)
            {
                return;
            }

            if (!MapGridManager.Instance.HasCurrentGrid)
            {
                RecycleAgent(state.characterId);
                return;
            }

            string currentMapId = MapGridManager.Instance.CurrentMapId;
            if (!state.IsInMap(currentMapId))
            {
                RecycleAgent(state.characterId);
                return;
            }

            EnsureAgentForState(state);
        }

        private void EnsureAgentForState(CharacterRuntimeState state)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.characterId))
            {
                return;
            }

            bool created = false;
            if (!activeAgents.TryGetValue(state.characterId, out CharacterAgent agent) || agent == null)
            {
                if (!TryCreateAgent(state.characterId, out agent))
                {
                    return;
                }

                activeAgents[state.characterId] = agent;
                created = true;
            }

            agent.Bind(state.characterId);
            ApplyStateToAgent(agent, state, created);
        }

        private bool TryCreateAgent(string characterId, out CharacterAgent agent)
        {
            agent = null;
            if (!TryGetDefinition(characterId, out CharacterDefinition_SO definition) ||
                string.IsNullOrWhiteSpace(definition.prefabKey))
            {
                Debug.LogWarning($"[CharacterAgentManager] Cannot create agent. Missing prefabKey for character '{characterId}'.");
                return false;
            }

            EnsureAgentRoot();
            GameObject instance = PoolManager.Instance.Get(definition.prefabKey, agentRoot);
            if (instance == null)
            {
                Debug.LogWarning($"[CharacterAgentManager] PoolManager returned null for character prefabKey '{definition.prefabKey}'.");
                return false;
            }

            NormalizeAgentInstanceZ(instance);

            agent = instance.GetComponent<CharacterAgent>();
            if (agent == null)
            {
                agent = instance.AddComponent<CharacterAgent>();
            }

            return true;
        }

        // created: 表示刚创建
        private void ApplyStateToAgent(CharacterAgent agent, CharacterRuntimeState state, bool created)
        {
            switch (state.moveState)
            {
                case CharacterMoveState.Moving:
                    if (created)
                    {
                        agent.SnapToCell(state.currentCell);
                    }

                    // 已经在移动的 Agent 由自身 FixedUpdate 推进，避免每次状态回报都重置路径。
                    if (!agent.IsMoving && state.remainingPath.Count > 0)
                    {
                        agent.SnapToCell(state.currentCell);
                        agent.MoveAlongCells(state.remainingPath, state.moveSpeed);
                    }
                    break;

                case CharacterMoveState.Idle:
                case CharacterMoveState.Arrived:
                case CharacterMoveState.Blocked:
                default:
                    agent.SnapToCell(state.currentCell);
                    break;
            }
        }

        private void RecycleAgentsOutsideMap(string currentMapId)
        {
            List<string> recycleIds = new List<string>();
            foreach (KeyValuePair<string, CharacterAgent> pair in activeAgents)
            {
                if (!CharacterScheduleManager.Instance.TryGetState(pair.Key, out CharacterRuntimeState state) ||
                    !state.IsInMap(currentMapId))
                {
                    recycleIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < recycleIds.Count; i++)
            {
                RecycleAgent(recycleIds[i]);
            }
        }

        private void RecycleAllAgents()
        {
            List<string> ids = new List<string>(activeAgents.Keys);
            for (int i = 0; i < ids.Count; i++)
            {
                RecycleAgent(ids[i]);
            }
        }

        private void RecycleAgent(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId) ||
                !activeAgents.TryGetValue(characterId, out CharacterAgent agent))
            {
                return;
            }

            activeAgents.Remove(characterId);
            if (agent == null)
            {
                return;
            }

            agent.StopMove();
            PoolManager.Instance.Recycle(agent.gameObject);
        }

        private static bool TryGetDefinition(string characterId, out CharacterDefinition_SO definition)
        {
            definition = null;
            return GameDatabase.TryGet(out ICharacterDefinitionDatabase database) &&
                   database.TryGet(characterId, out definition);
        }

        private void EnsureAgentRoot()
        {
            if (agentRoot != null)
            {
                return;
            }

            GameObject rootObject = new GameObject("CharacterAgents");
            rootObject.transform.SetParent(transform);
            agentRoot = rootObject.transform;
        }

        private static void NormalizeAgentInstanceZ(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            Vector3 position = instance.transform.position;
            position.z = 0f;
            instance.transform.position = position;
        }
    }
}
