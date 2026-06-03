using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using WS_Modules;

namespace WS_Modules.SceneModule
{
    /// <summary>
    /// 场景转换 Route 配置资产，描述从触发器去往哪个场景和哪个目标地点。
    /// </summary>
    [CreateAssetMenu(fileName = "SceneTransitionConfig", menuName = "WSFrame/SceneSystem/SceneTransitionConfig", order = 0)]
    public sealed class SceneTransitionConfig : ScriptableObject
    {
        [SerializeField]
        [LabelText("Routes")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
        private List<SceneTransitionRoute> routes = new List<SceneTransitionRoute>();

        [SerializeField]
        [LabelText("Collected Spawn Ids")]
        [InfoBox("TargetSpawnId options come from SceneSpawnRoot.SpawnEntries collected from currently open scenes. " +
                 "After changing SceneSpawnRoot in a scene, click Refresh Spawn Ids From Open Scenes again.")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = false, IsReadOnly = true)]
        private List<SceneSpawnIdCache> collectedSpawnIds = new List<SceneSpawnIdCache>();

        private readonly Dictionary<string, SceneTransitionRoute> routeMap =
            new Dictionary<string, SceneTransitionRoute>(StringComparer.Ordinal);

        private bool mapDirty = true;

        /// <summary>
        /// 当前配置的场景转换 Route 列表。
        /// </summary>
        public IReadOnlyList<SceneTransitionRoute> Routes => routes;

        /// <summary>
        /// 尝试通过 RouteId 获取场景转换 Route。
        /// </summary>
        /// <param name="routeId">唯一 RouteId。</param>
        /// <param name="route">匹配到的场景转换 Route。</param>
        /// <returns>如果找到匹配 Route，则返回 true。</returns>
        public bool TryGetRoute(string routeId, out SceneTransitionRoute route)
        {
            EnsureRouteMap();
            return routeMap.TryGetValue(routeId, out route);
        }

        // 标记 Route 查找表需要重建。
        private void OnValidate()
        {
            mapDirty = true;
            BindRouteOwners();
            ValidateRoutes();
        }

        // 标记 Route 查找表需要在运行时第一次查询前构建。
        private void OnEnable()
        {
            mapDirty = true;
            BindRouteOwners();
        }

        // 从当前已加载场景中的 SceneSpawnRoot 收集可选 TargetSpawnId。
        [Button("Refresh Spawn Ids From Open Scenes")]
        private void RefreshSpawnIdsFromOpenScenes()
        {
            collectedSpawnIds.Clear();
            var cacheByScene = new Dictionary<string, SceneSpawnIdCache>(StringComparer.Ordinal);

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }

                CollectSpawnIdsFromScene(scene, cacheByScene);
            }

            collectedSpawnIds.Sort((left, right) =>
                string.Compare(left.SceneName, right.SceneName, StringComparison.Ordinal));

            Debug.Log(
                $"{nameof(SceneTransitionConfig)} refreshed spawn ids from {collectedSpawnIds.Count} open scenes.",
                this);
        }

        // 手动校验 Route 配置，便于 Odin Inspector 中主动检查。
        [Button("Validate Routes")]
        private void ValidateRoutes()
        {
            BindRouteOwners();
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < routes.Count; i++)
            {
                SceneTransitionRoute route = routes[i];
                if (route == null)
                {
                    Debug.LogWarning($"{nameof(SceneTransitionConfig)} has a null route at index {i}.", this);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(route.RouteId))
                {
                    Debug.LogWarning($"{nameof(SceneTransitionConfig)} has an empty RouteId at index {i}.", this);
                }
                else if (!seenIds.Add(route.RouteId))
                {
                    Debug.LogWarning(
                        $"{nameof(SceneTransitionConfig)} has duplicate RouteId '{route.RouteId}'.",
                        this);
                }

                if (string.IsNullOrWhiteSpace(route.TargetSceneName))
                {
                    Debug.LogWarning(
                        $"{nameof(SceneTransitionConfig)} route '{route.RouteId}' has no target scene.",
                        this);
                }

                if (string.IsNullOrWhiteSpace(route.TargetSpawnId))
                {
                    Debug.LogWarning(
                        $"{nameof(SceneTransitionConfig)} route '{route.RouteId}' has no target spawn id.",
                        this);
                }
                else if (TryGetCollectedSpawnIds(route.TargetSceneName, out IReadOnlyList<string> spawnIds) &&
                         !ContainsSpawnId(spawnIds, route.TargetSpawnId))
                {
                    Debug.LogWarning(
                        $"{nameof(SceneTransitionConfig)} route '{route.RouteId}' target spawn id '{route.TargetSpawnId}' " +
                        $"was not collected from scene '{route.TargetSceneName}'.",
                        this);
                }
            }
        }

        // 绑定 Route 到当前 Config，供 Odin 下拉和提示读取缓存。
        private void BindRouteOwners()
        {
            for (int i = 0; i < routes.Count; i++)
            {
                routes[i]?.SetOwner(this);
            }
        }

        // 从指定场景的 Root 对象中收集 SpawnId。
        private void CollectSpawnIdsFromScene(Scene scene, Dictionary<string, SceneSpawnIdCache> cacheByScene)
        {
            if (!cacheByScene.TryGetValue(scene.name, out SceneSpawnIdCache sceneCache))
            {
                sceneCache = new SceneSpawnIdCache(scene.name);
                cacheByScene.Add(scene.name, sceneCache);
                collectedSpawnIds.Add(sceneCache);
            }

            GameObject[] rootObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                SceneSpawnRoot[] roots = rootObjects[i].GetComponentsInChildren<SceneSpawnRoot>(true);
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    CollectSpawnIdsFromRoot(scene.name, roots[rootIndex], sceneCache);
                }
            }
        }

        // 从单个 SceneSpawnRoot 中收集有效 SpawnId 并输出配置问题。
        private void CollectSpawnIdsFromRoot(string sceneName, SceneSpawnRoot root, SceneSpawnIdCache sceneCache)
        {
            IReadOnlyList<SceneSpawnEntry> spawnEntries = root.SpawnEntries;
            for (int i = 0; i < spawnEntries.Count; i++)
            {
                SceneSpawnEntry entry = spawnEntries[i];
                if (entry == null)
                {
                    Debug.LogWarning(
                        $"{nameof(SceneSpawnRoot)} in scene '{sceneName}' has a null spawn entry at index {i}.",
                        root);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.TargetSpawnId))
                {
                    Debug.LogWarning(
                        $"{nameof(SceneSpawnRoot)} in scene '{sceneName}' has an empty TargetSpawnId at index {i}.",
                        root);
                    continue;
                }

                if (entry.SpawnTransform == null)
                {
                    Debug.LogWarning(
                        $"{nameof(SceneSpawnRoot)} in scene '{sceneName}' spawn entry '{entry.TargetSpawnId}' has no Transform.",
                        root);
                    continue;
                }

                if (!sceneCache.TryAddSpawnId(entry.TargetSpawnId))
                {
                    Debug.LogWarning(
                        $"{nameof(SceneSpawnRoot)} in scene '{sceneName}' has duplicate TargetSpawnId '{entry.TargetSpawnId}'.",
                        root);
                }
            }
        }

        // 尝试获取指定场景已经收集到的 SpawnId 列表。
        internal bool TryGetCollectedSpawnIds(string sceneName, out IReadOnlyList<string> spawnIds)
        {
            for (int i = 0; i < collectedSpawnIds.Count; i++)
            {
                SceneSpawnIdCache cache = collectedSpawnIds[i];
                if (cache != null && string.Equals(cache.SceneName, sceneName, StringComparison.Ordinal))
                {
                    spawnIds = cache.SpawnIds;
                    return true;
                }
            }

            spawnIds = Array.Empty<string>();
            return false;
        }

        // 判断已收集列表中是否包含指定 SpawnId。
        internal static bool ContainsSpawnId(IReadOnlyList<string> spawnIds, string targetSpawnId)
        {
            for (int i = 0; i < spawnIds.Count; i++)
            {
                if (string.Equals(spawnIds[i], targetSpawnId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        // 确保 Route 查找表已经按当前配置构建。
        private void EnsureRouteMap()
        {
            if (!mapDirty)
            {
                return;
            }

            routeMap.Clear();
            for (int i = 0; i < routes.Count; i++)
            {
                SceneTransitionRoute route = routes[i];
                if (route == null || string.IsNullOrWhiteSpace(route.RouteId))
                {
                    continue;
                }

                if (!routeMap.ContainsKey(route.RouteId))
                {
                    routeMap.Add(route.RouteId, route);
                }
            }

            mapDirty = false;
        }
    }

    /// <summary>
    /// 描述一条场景转换 Route 的配置数据。
    /// </summary>
    [Serializable]
    public sealed class SceneTransitionRoute
    {
        [NonSerialized]
        private SceneTransitionConfig owner;

        [SerializeField]
        [LabelText("Route Id")]
        private string routeId;

        [SerializeField]
        [LabelText("Display Name")]
        private string displayName;

        [SerializeField]
        [LabelText("Target Scene")]
        [WSScene]
        private string targetSceneName;

        [SerializeField]
        [LabelText("Target Spawn Id")]
        [InfoBox("@GetTargetSpawnIdInfoMessage()", InfoMessageType.Info, "@ShouldShowTargetSpawnIdInfoBox()")]
        [InfoBox("@GetMissingTargetSpawnIdMessage()", InfoMessageType.Warning, "@ShouldShowMissingTargetSpawnIdBox()")]
        [ValueDropdown(nameof(GetTargetSpawnIdDropdown), IsUniqueList = false)]
        private string targetSpawnId;

        [SerializeField]
        [LabelText("Reset Rigidbody Velocity")]
        private bool resetRigidbodyVelocity = true;

        [SerializeField]
        [LabelText("Apply Spawn Rotation")]
        private bool applySpawnRotation;

        /// <summary>
        /// 唯一 RouteId。
        /// </summary>
        public string RouteId => routeId;

        /// <summary>
        /// Inspector 中展示的 Route 名称。
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// Route 指向的目标场景名称。
        /// </summary>
        public string TargetSceneName => targetSceneName;

        /// <summary>
        /// Route 指向的目标地点 Id。
        /// </summary>
        public string TargetSpawnId => targetSpawnId;

        /// <summary>
        /// 转场落位时是否清空 Rigidbody2D 速度。
        /// </summary>
        public bool ResetRigidbodyVelocity => resetRigidbodyVelocity;

        /// <summary>
        /// 转场落位时是否应用出生点旋转。
        /// </summary>
        public bool ApplySpawnRotation => applySpawnRotation;

        // 记录所属 Config，供 Odin Inspector 读取编辑器缓存。
        internal void SetOwner(SceneTransitionConfig config)
        {
            owner = config;
        }

        // 根据目标场景返回当前收集到的 SpawnId 下拉项。
        private IEnumerable<ValueDropdownItem<string>> GetTargetSpawnIdDropdown()
        {
            if (owner == null ||
                string.IsNullOrWhiteSpace(targetSceneName) ||
                !owner.TryGetCollectedSpawnIds(targetSceneName, out IReadOnlyList<string> spawnIds))
            {
                yield break;
            }

            for (int i = 0; i < spawnIds.Count; i++)
            {
                string spawnId = spawnIds[i];
                yield return new ValueDropdownItem<string>(spawnId, spawnId);
            }
        }

        // 说明 TargetSpawnId 的编辑器缓存来源。
        private string GetTargetSpawnIdInfoMessage()
        {
            return string.IsNullOrWhiteSpace(targetSceneName)
                ? "Set Target Scene before selecting Target Spawn Id."
                : $"TargetSpawnId options are collected from open scene '{targetSceneName}'. " +
                  "Open the target scene and refresh the config after editing SceneSpawnRoot.";
        }

        // 目标场景没有可用缓存时显示说明。
        private bool ShouldShowTargetSpawnIdInfoBox()
        {
            return owner == null ||
                   string.IsNullOrWhiteSpace(targetSceneName) ||
                   !owner.TryGetCollectedSpawnIds(targetSceneName, out IReadOnlyList<string> spawnIds) ||
                   spawnIds.Count == 0;
        }

        // 当前 TargetSpawnId 不在缓存中时显示 warning。
        private string GetMissingTargetSpawnIdMessage()
        {
            return $"TargetSpawnId '{targetSpawnId}' was not collected from scene '{targetSceneName}'.";
        }

        // 判断当前 TargetSpawnId 是否缺失于已收集缓存。
        private bool ShouldShowMissingTargetSpawnIdBox()
        {
            return owner != null &&
                   !string.IsNullOrWhiteSpace(targetSceneName) &&
                   !string.IsNullOrWhiteSpace(targetSpawnId) &&
                   owner.TryGetCollectedSpawnIds(targetSceneName, out IReadOnlyList<string> spawnIds) &&
                   spawnIds.Count > 0 &&
                   !SceneTransitionConfig.ContainsSpawnId(spawnIds, targetSpawnId);
        }
    }

    /// <summary>
    /// SceneTransitionConfig 中缓存的单个场景 SpawnId 列表，仅用于 Inspector 编辑辅助。
    /// </summary>
    [Serializable]
    internal sealed class SceneSpawnIdCache
    {
        [SerializeField]
        [LabelText("Scene Name")]
        private string sceneName;

        [SerializeField]
        [LabelText("Spawn Ids")]
        private List<string> spawnIds = new List<string>();

        /// <summary>
        /// 缓存所属的场景名称。
        /// </summary>
        public string SceneName => sceneName;

        /// <summary>
        /// 从该场景 SceneSpawnRoot 中收集到的 SpawnId 列表。
        /// </summary>
        public IReadOnlyList<string> SpawnIds => spawnIds;

        // Unity 序列化使用的默认构造。
        private SceneSpawnIdCache()
        {
        }

        internal SceneSpawnIdCache(string sceneName)
        {
            this.sceneName = sceneName;
        }

        // 添加一个 SpawnId，重复时返回 false。
        internal bool TryAddSpawnId(string spawnId)
        {
            if (SceneTransitionConfig.ContainsSpawnId(spawnIds, spawnId))
            {
                return false;
            }

            spawnIds.Add(spawnId);
            return true;
        }
    }
}
