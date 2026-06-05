using System;
using UnityEngine;

namespace WorldItems
{
    /// <summary>
    /// 单个世界物品的数据记录，只描述物品本体，不保存所属 mapId。
    /// 所属地图由 WorldItemManager 外层场景桶负责区分。
    /// </summary>
    [Serializable]
    public sealed class WorldItemRecord
    {
        public int InstanceId;
        public int ItemId;
        public int Count;
        public Vector3 Position;

        public WorldItemRecord(int instanceId, int itemId, int count, Vector3 position)
        {
            InstanceId = instanceId;
            ItemId = itemId;
            Count = Mathf.Max(1, count);
            Position = position;
        }
    }
}
