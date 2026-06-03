using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace WS_Modules.SceneModule
{
    /// <summary>
    /// 目标场景中的场景出生点表，维护 TargetSpawnId 到 Transform 的映射。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneSpawnRoot : MonoBehaviour
    {
        [SerializeField]
        [LabelText("Spawn Entries")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
        private List<SceneSpawnEntry> spawnEntries = new List<SceneSpawnEntry>();

        private readonly Dictionary<string, Transform> spawnPointMap =
            new Dictionary<string, Transform>(StringComparer.Ordinal);

        private bool mapDirty = true;

        /// <summary>
        /// 当前配置的出生点条目列表。
        /// </summary>
        public IReadOnlyList<SceneSpawnEntry> SpawnEntries => spawnEntries;

        /// <summary>
        /// 尝试通过 TargetSpawnId 获取对应出生点 Transform。
        /// </summary>
        /// <param name="targetSpawnId">目标场景地点 Id。</param>
        /// <param name="spawnPoint">匹配到的出生点 Transform。</param>
        /// <returns>如果找到匹配出生点，则返回 true。</returns>
        public bool TryGetSpawnPoint(string targetSpawnId, out Transform spawnPoint)
        {
            EnsureSpawnPointMap();
            return spawnPointMap.TryGetValue(targetSpawnId, out spawnPoint);
        }

        // 标记查找表需要重建。
        private void OnValidate()
        {
            mapDirty = true;
            ValidateSpawnEntries();
        }

        // 标记查找表需要在运行时第一次查询前构建。
        private void Awake()
        {
            mapDirty = true;
        }

        // 手动校验出生点配置，便于 Odin Inspector 中主动检查。
        [Button("Validate Spawn Entries")]
        private void ValidateSpawnEntries()
        {
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < spawnEntries.Count; i++)
            {
                SceneSpawnEntry entry = spawnEntries[i];
                if (entry == null)
                {
                    Debug.LogWarning($"{nameof(SceneSpawnRoot)} has a null spawn entry at index {i}.", this);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.TargetSpawnId))
                {
                    Debug.LogWarning($"{nameof(SceneSpawnRoot)} has an empty TargetSpawnId at index {i}.", this);
                }
                else if (!seenIds.Add(entry.TargetSpawnId))
                {
                    Debug.LogWarning(
                        $"{nameof(SceneSpawnRoot)} has duplicate TargetSpawnId '{entry.TargetSpawnId}'.",
                        this);
                }

                if (entry.SpawnTransform == null)
                {
                    Debug.LogWarning(
                        $"{nameof(SceneSpawnRoot)} spawn entry '{entry.TargetSpawnId}' has no Transform.",
                        this);
                }
            }
        }

        // 确保出生点查找表已经按当前配置构建。
        private void EnsureSpawnPointMap()
        {
            if (!mapDirty)
            {
                return;
            }

            spawnPointMap.Clear();
            for (int i = 0; i < spawnEntries.Count; i++)
            {
                SceneSpawnEntry entry = spawnEntries[i];
                if (entry == null ||
                    string.IsNullOrWhiteSpace(entry.TargetSpawnId) ||
                    entry.SpawnTransform == null)
                {
                    continue;
                }

                if (!spawnPointMap.ContainsKey(entry.TargetSpawnId))
                {
                    spawnPointMap.Add(entry.TargetSpawnId, entry.SpawnTransform);
                }
            }

            mapDirty = false;
        }
    }

    /// <summary>
    /// SceneSpawnRoot 中的一条 TargetSpawnId 到 Transform 映射配置。
    /// </summary>
    [Serializable]
    public sealed class SceneSpawnEntry
    {
        [SerializeField]
        [LabelText("Target Spawn Id")]
        private string targetSpawnId;

        [SerializeField]
        [LabelText("Spawn Transform")]
        private Transform spawnTransform;

        /// <summary>
        /// 目标场景地点 Id。
        /// </summary>
        public string TargetSpawnId => targetSpawnId;

        /// <summary>
        /// 目标场景地点对应的位置 Transform。
        /// </summary>
        public Transform SpawnTransform => spawnTransform;
    }
}
