using System.Collections.Generic;
using GameData;
using UnityEngine;
using WS_Modules.Extensions;
using WS_Modules.SceneModule;

namespace WorldItems
{
    /// <summary>
    /// 世界物品数据管理器，按当前 SceneSystem 场景名作为 mapId 分桶存储世界物品记录。
    /// 不负责生成、回收或销毁 GameObject。
    /// </summary>
    public static class WorldItemManager
    {
        private static readonly Dictionary<string, WorldItemSceneBucket> bucketsByMapId =
            new Dictionary<string, WorldItemSceneBucket>();

        private static int nextInstanceId;

        /// <summary>
        /// 获取当前地图的世界物品记录桶，不存在时自动创建。
        /// </summary>
        public static WorldItemSceneBucket GetCurrentBucket()
        {
            return GetOrCreateBucket(GetCurrentMapId());
        }

        /// <summary>
        /// 获取指定地图的世界物品记录桶，不存在时自动创建。
        /// </summary>
        public static WorldItemSceneBucket GetBucket(string mapId)
        {
            return GetOrCreateBucket(mapId);
        }

        /// <summary>
        /// 获取当前地图的世界物品记录快照视图。
        /// </summary>
        public static IReadOnlyCollection<WorldItemRecord> GetCurrentMapRecords()
        {
            return GetCurrentBucket().Records;
        }

        /// <summary>
        /// 为新生成的世界 Item 创建数据记录，并给 Item 绑定 WorldItemIdentity。
        /// </summary>
        public static int CreateRecordForItem(Item item)
        {
            if (item == null)
            {
                Debug.LogWarning("[WorldItemManager] Cannot create record for null item.");
                return 0;
            }

            int instanceId = ++nextInstanceId;
            WorldItemRecord record = new WorldItemRecord(
                instanceId,
                item.ItemId,
                item.Count,
                item.transform.position);

            WorldItemSceneBucket bucket = GetCurrentBucket();
            if (!bucket.Add(record))
            {
                Debug.LogWarning($"[WorldItemManager] Failed to add world item record instanceId={instanceId}.");
                return 0;
            }

            BindIdentity(item, instanceId);
            return instanceId;
        }

        /// <summary>
        /// 将已存在的数据记录绑定到重建出来的可见 Item 上，不创建新记录。
        /// </summary>
        public static bool BindRecordToItem(Item item, int instanceId)
        {
            if (item == null || instanceId <= 0)
            {
                return false;
            }

            if (!GetCurrentBucket().Contains(instanceId))
            {
                Debug.LogWarning($"[WorldItemManager] Cannot bind missing world item record instanceId={instanceId}.");
                return false;
            }

            BindIdentity(item, instanceId);
            return true;
        }

        /// <summary>
        /// 将 Item 当前数量和位置同步回对应记录。
        /// </summary>
        public static bool UpdateRecordFromItem(Item item)
        {
            if (!TryGetIdentity(item, out WorldItemIdentity identity))
            {
                return false;
            }

            return GetCurrentBucket().UpdateFromItem(item, identity.InstanceId);
        }

        /// <summary>
        /// 删除 Item 对应的数据记录，但不销毁或回收 Item GameObject。
        /// </summary>
        public static bool RemoveRecordForItem(Item item)
        {
            if (!TryGetIdentity(item, out WorldItemIdentity identity))
            {
                return false;
            }

            bool removed = GetCurrentBucket().Remove(identity.InstanceId);
            identity.Clear();
            return removed;
        }

        private static string GetCurrentMapId()
        {
            return SceneSystem.CurrentScene.name;
        }

        private static WorldItemSceneBucket GetOrCreateBucket(string mapId)
        {
            if (string.IsNullOrWhiteSpace(mapId))
            {
                mapId = "UnknownMap";
            }

            if (!bucketsByMapId.TryGetValue(mapId, out WorldItemSceneBucket bucket))
            {
                bucket = new WorldItemSceneBucket();
                bucketsByMapId.Add(mapId, bucket);
            }

            return bucket;
        }

        private static void BindIdentity(Item item, int instanceId)
        {
            WorldItemIdentity identity = item.gameObject.GetOrAddComponent<WorldItemIdentity>();
            identity.Initialize(instanceId);
        }

        private static bool TryGetIdentity(Item item, out WorldItemIdentity identity)
        {
            identity = null;
            return item != null &&
                   item.TryGetComponent(out identity) &&
                   identity.HasIdentity;
        }
    }
}
