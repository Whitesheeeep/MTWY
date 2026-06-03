using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using WS_Modules;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

        // 从当前已加载场景的 SceneSpawnRoot 同步生成或更新 Routes。
        [Button("Refresh Routes From Open Scene Spawns")]
        private void RefreshRoutesFromOpenSceneSpawns()
        {
            OpenSceneSpawnMap spawnMap = BuildOpenSceneSpawnMap();
            int addedCount = 0;
            int updatedCount = 0;

            foreach (KeyValuePair<string, HashSet<string>> sceneEntry in spawnMap.SpawnIdsByScene)
            {
                foreach (string spawnId in sceneEntry.Value)
                {
                    SceneTransitionRoute route = FindRoute(sceneEntry.Key, spawnId);
                    if (route == null)
                    {
                        routes.Add(SceneTransitionRoute.CreateFromSpawn(
                            sceneEntry.Key,
                            spawnId,
                            GenerateUniqueRouteId(sceneEntry.Key, spawnId)));
                        addedCount++;
                    }
                    else
                    {
                        route.UpdateFromSpawn(sceneEntry.Key, spawnId);
                        updatedCount++;
                    }
                }
            }

            mapDirty = true;
            BindRouteOwners();
            MarkDirtyInEditor();

            Debug.Log(
                $"{nameof(SceneTransitionConfig)} refreshed routes from open scene spawns. Added: {addedCount}, Updated: {updatedCount}.",
                this);
        }

        // 删除当前已加载场景中可确认不存在的 Route。
        [Button("Remove Invalid Routes From Open Scenes")]
        private void RemoveInvalidRoutesFromOpenScenes()
        {
            OpenSceneSpawnMap spawnMap = BuildOpenSceneSpawnMap();
            int removedCount = routes.RemoveAll(route => IsRouteInvalidInOpenScenes(route, spawnMap));
            if (removedCount > 0)
            {
                mapDirty = true;
                MarkDirtyInEditor();
            }

            Debug.Log(
                $"{nameof(SceneTransitionConfig)} removed {removedCount} invalid routes from open scenes.",
                this);
        }

        // 手动校验 Route 配置，便于 Odin Inspector 中主动检查。
        [Button("Validate Routes")]
        private void ValidateRoutes()
        {
            BindRouteOwners();
            OpenSceneSpawnMap spawnMap = BuildOpenSceneSpawnMap();
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
                else if (IsRouteInvalidInOpenScenes(route, spawnMap))
                {
                    Debug.LogWarning(
                        $"{nameof(SceneTransitionConfig)} route '{route.RouteId}' target spawn id '{route.TargetSpawnId}' " +
                        $"does not exist in open scene '{route.TargetSceneName}'.",
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

        // 查找匹配目标场景和 SpawnId 的已有 Route。
        private SceneTransitionRoute FindRoute(string sceneName, string targetSpawnId)
        {
            for (int i = 0; i < routes.Count; i++)
            {
                SceneTransitionRoute route = routes[i];
                if (route != null &&
                    string.Equals(route.TargetSceneName, sceneName, StringComparison.Ordinal) &&
                    string.Equals(route.TargetSpawnId, targetSpawnId, StringComparison.Ordinal))
                {
                    return route;
                }
            }

            return null;
        }

        // 根据场景和 SpawnId 生成不重复的 RouteId。
        private string GenerateUniqueRouteId(string sceneName, string targetSpawnId)
        {
            string baseRouteId = $"{sceneName}_{targetSpawnId}";
            string routeId = baseRouteId;
            int suffix = 1;
            while (ContainsRouteId(routeId))
            {
                routeId = $"{baseRouteId}_{suffix}";
                suffix++;
            }

            return routeId;
        }

        // 判断当前 Routes 中是否已经存在指定 RouteId。
        private bool ContainsRouteId(string routeId)
        {
            for (int i = 0; i < routes.Count; i++)
            {
                SceneTransitionRoute route = routes[i];
                if (route != null && string.Equals(route.RouteId, routeId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        // 判断 Route 是否在当前打开的目标场景中已失效。
        private static bool IsRouteInvalidInOpenScenes(SceneTransitionRoute route, OpenSceneSpawnMap spawnMap)
        {
            if (route == null ||
                string.IsNullOrWhiteSpace(route.TargetSceneName) ||
                string.IsNullOrWhiteSpace(route.TargetSpawnId) ||
                !spawnMap.OpenSceneNames.Contains(route.TargetSceneName))
            {
                return false;
            }

            return !spawnMap.SpawnIdsByScene.TryGetValue(route.TargetSceneName, out HashSet<string> spawnIds) ||
                   !spawnIds.Contains(route.TargetSpawnId);
        }

        // 供 Route 的 Odin InfoBox 判断当前 Route 是否已失效。
        internal bool IsRouteInvalidForInspector(SceneTransitionRoute route)
        {
            return IsRouteInvalidInOpenScenes(route, BuildOpenSceneSpawnMap());
        }

        // 构建当前已加载场景中的 SpawnId 查询表。
        private OpenSceneSpawnMap BuildOpenSceneSpawnMap()
        {
            var spawnMap = new OpenSceneSpawnMap();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }

                spawnMap.OpenSceneNames.Add(scene.name);
                CollectSpawnIdsFromScene(scene, spawnMap);
            }

            return spawnMap;
        }

        // 从指定场景的 Root 对象中收集 SpawnId。
        private void CollectSpawnIdsFromScene(Scene scene, OpenSceneSpawnMap spawnMap)
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                SceneSpawnRoot[] roots = rootObjects[i].GetComponentsInChildren<SceneSpawnRoot>(true);
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    CollectSpawnIdsFromRoot(scene.name, roots[rootIndex], spawnMap);
                }
            }
        }

        // 从单个 SceneSpawnRoot 中收集有效 SpawnId 并输出配置问题。
        private void CollectSpawnIdsFromRoot(string sceneName, SceneSpawnRoot root, OpenSceneSpawnMap spawnMap)
        {
            if (!spawnMap.SpawnIdsByScene.TryGetValue(sceneName, out HashSet<string> spawnIds))
            {
                spawnIds = new HashSet<string>(StringComparer.Ordinal);
                spawnMap.SpawnIdsByScene.Add(sceneName, spawnIds);
            }

            IReadOnlyList<SceneSpawnEntry> spawnEntries = root.SpawnEntries;
            for (int i = 0; i < spawnEntries.Count; i++)
            {
                SceneSpawnEntry entry = spawnEntries[i];
                if (!TryValidateSpawnEntry(sceneName, root, entry, i))
                {
                    continue;
                }

                if (!spawnIds.Add(entry.TargetSpawnId))
                {
                    Debug.LogWarning(
                        $"{nameof(SceneSpawnRoot)} in scene '{sceneName}' has duplicate TargetSpawnId '{entry.TargetSpawnId}'.",
                        root);
                }
            }
        }

        // 校验单条 SpawnEntry 是否可用于生成 Route。
        private static bool TryValidateSpawnEntry(string sceneName, SceneSpawnRoot root, SceneSpawnEntry entry, int index)
        {
            if (entry == null)
            {
                Debug.LogWarning(
                    $"{nameof(SceneSpawnRoot)} in scene '{sceneName}' has a null spawn entry at index {index}.",
                    root);
                return false;
            }

            if (string.IsNullOrWhiteSpace(entry.TargetSpawnId))
            {
                Debug.LogWarning(
                    $"{nameof(SceneSpawnRoot)} in scene '{sceneName}' has an empty TargetSpawnId at index {index}.",
                    root);
                return false;
            }

            if (entry.SpawnTransform == null)
            {
                Debug.LogWarning(
                    $"{nameof(SceneSpawnRoot)} in scene '{sceneName}' spawn entry '{entry.TargetSpawnId}' has no Transform.",
                    root);
                return false;
            }

            return true;
        }

        // 标记资产已被编辑器按钮修改。
        private void MarkDirtyInEditor()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
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
        [InfoBox("TargetSpawnId is synced from SceneSpawnRoot.SpawnEntries in currently open scenes. " +
                 "After editing SceneSpawnRoot, refresh routes from the SceneTransitionConfig buttons.")]
        [InfoBox("@GetInvalidRouteMessage()", InfoMessageType.Warning, "@ShouldShowInvalidRouteBox()")]
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

        // 按场景 SpawnEntry 更新 Route 的目标信息。
        internal void UpdateFromSpawn(string sceneName, string spawnId)
        {
            targetSceneName = sceneName;
            targetSpawnId = spawnId;
            displayName = spawnId;
        }

        // 创建一条由场景 SpawnEntry 派生的新 Route。
        internal static SceneTransitionRoute CreateFromSpawn(string sceneName, string spawnId, string routeId)
        {
            return new SceneTransitionRoute
            {
                routeId = routeId,
                displayName = spawnId,
                targetSceneName = sceneName,
                targetSpawnId = spawnId
            };
        }

        // 获取当前失效提示文本。
        private string GetInvalidRouteMessage()
        {
            return $"TargetSpawnId '{targetSpawnId}' does not exist in currently open scene '{targetSceneName}'.";
        }

        // 判断当前 Route 是否在已打开目标场景中失效。
        private bool ShouldShowInvalidRouteBox()
        {
            return owner != null && owner.IsRouteInvalidForInspector(this);
        }
    }

    // 当前已加载场景中的 SpawnId 查询数据。
    internal sealed class OpenSceneSpawnMap
    {
        internal readonly HashSet<string> OpenSceneNames = new HashSet<string>(StringComparer.Ordinal);
        internal readonly Dictionary<string, HashSet<string>> SpawnIdsByScene =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
    }
}
