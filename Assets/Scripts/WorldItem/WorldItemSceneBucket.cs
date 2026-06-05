using System.Collections.Generic;
using GameData;
using UnityEngine;

namespace WorldItems
{
    /// <summary>
    /// 单个地图的世界物品记录桶，负责维护该地图内 instanceId 到记录的映射。
    /// </summary>
    public sealed class WorldItemSceneBucket
    {
        private readonly Dictionary<int, WorldItemRecord> records = new Dictionary<int, WorldItemRecord>();

        public IReadOnlyCollection<WorldItemRecord> Records => records.Values;
        public int Count => records.Count;

        public bool Contains(int instanceId)
        {
            return records.ContainsKey(instanceId);
        }

        public bool TryGet(int instanceId, out WorldItemRecord record)
        {
            return records.TryGetValue(instanceId, out record);
        }

        public bool Add(WorldItemRecord record)
        {
            if (record == null || record.InstanceId <= 0 || records.ContainsKey(record.InstanceId))
            {
                return false;
            }

            records.Add(record.InstanceId, record);
            return true;
        }

        public bool Remove(int instanceId)
        {
            return records.Remove(instanceId);
        }

        public bool UpdateFromItem(Item item, int instanceId)
        {
            if (item == null || !records.TryGetValue(instanceId, out WorldItemRecord record))
            {
                return false;
            }

            record.ItemId = item.ItemId;
            record.Count = Mathf.Max(1, item.Count);
            record.Position = item.transform.position;
            return true;
        }

        public void Clear()
        {
            records.Clear();
        }
    }
}
