using System;
using System.Collections.Generic;
using GameData;
using Sirenix.OdinInspector;
using UnityEngine;
using WS_Modules.Singleton;

namespace Inventory
{
    /// <summary>
    /// 运行时背包管理器，负责对外提供固定槽位背包的增删改查、拆分合并和数据导入导出。
    /// </summary>
    public sealed class InventoryManager : SingletonMonoBase<InventoryManager>
    {
        [SerializeField] private int capacity = 30;
        [SerializeField] private int maxStackCount = 64;
        [ReadOnly]
        [SerializeField] private InventoryData data = new InventoryData();

        private IItemDatabase itemDatabase;

        /// <summary>
        /// 当前背包容量。
        /// </summary>
        public int Capacity => capacity;

        /// <summary>
        /// 单个槽位的最大堆叠数量。
        /// </summary>
        public int MaxStackCount => maxStackCount;

        protected override void Awake()
        {
            base.Awake();
            EnsureData();
            Initialize();
        }

        private void OnValidate()
        {
            capacity = Mathf.Max(0, capacity);
            maxStackCount = Mathf.Max(1, maxStackCount);
            EnsureData();
        }

        /// <summary>
        /// 从 GameDatabase 拉取背包依赖的物品数据库。
        /// </summary>
        public void Initialize()
        {
            TryInitializeFromGameDatabase(true);
        }

        /// <summary>
        /// 由外部推送背包依赖的物品数据库，适合测试或明确启动顺序的场景。
        /// </summary>
        /// <param name="database">用于校验物品编号和读取物品配置的数据库。</param>
        public void Initialize(IItemDatabase database)
        {
            itemDatabase = database ?? throw new ArgumentNullException(nameof(database));
            EnsureData();
        }

        /// <summary>
        /// 向背包中加入物品，优先填充已有同类堆叠，再占用空槽。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">需要加入的数量。</param>
        /// <returns>未能放入背包的剩余数量，返回 0 表示全部放入。</returns>
        public int AddItem(int itemId, int count)
        {
            EnsureItemExists(itemId);
            EnsureData();
            return data.AddItem(itemId, count, maxStackCount);
        }

        /// <summary>
        /// 尝试向背包中加入物品，只有全部放入时才返回 true。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">需要加入的数量。</param>
        /// <returns>全部放入返回 true，空间不足返回 false。</returns>
        public bool TryAddItem(int itemId, int count)
        {
            EnsureItemExists(itemId);
            EnsureData();

            InventoryData snapshot = data.Clone();
            int remaining = data.AddItem(itemId, count, maxStackCount);
            if (remaining == 0)
            {
                return true;
            }

            data.CopyFrom(snapshot);
            return false;
        }

        /// <summary>
        /// 从背包中移除指定数量的物品，数量不足时不会修改背包。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">需要移除的数量。</param>
        /// <returns>移除成功返回 true，数量不足返回 false。</returns>
        public bool RemoveItem(int itemId, int count)
        {
            EnsureItemExists(itemId);
            EnsureData();
            return data.RemoveItem(itemId, count);
        }

        /// <summary>
        /// 设置指定物品在背包中的总数量。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">目标数量，0 表示清空该物品。</param>
        /// <returns>设置成功返回 true，槽位不足返回 false。</returns>
        public bool SetCount(int itemId, int count)
        {
            EnsureItemExists(itemId);
            EnsureData();
            return data.SetCount(itemId, count, maxStackCount);
        }

        /// <summary>
        /// 移动槽位。目标为空时移动，目标同类时合并，目标不同类时交换。
        /// </summary>
        /// <param name="fromIndex">来源槽位索引。</param>
        /// <param name="toIndex">目标槽位索引。</param>
        /// <returns>操作成功返回 true，否则返回 false。</returns>
        public bool MoveSlot(int fromIndex, int toIndex)
        {
            EnsureData();
            return data.MoveSlot(fromIndex, toIndex, maxStackCount);
        }

        /// <summary>
        /// 将来源槽位尽量合并到目标槽位。
        /// </summary>
        /// <param name="fromIndex">来源槽位索引。</param>
        /// <param name="toIndex">目标槽位索引。</param>
        /// <returns>成功移动至少一个物品返回 true，否则返回 false。</returns>
        public bool MergeSlots(int fromIndex, int toIndex)
        {
            EnsureData();
            return data.MergeSlots(fromIndex, toIndex, maxStackCount);
        }

        /// <summary>
        /// 从一个槽位拆出指定数量到另一个槽位。
        /// </summary>
        /// <param name="fromIndex">来源槽位索引。</param>
        /// <param name="count">拆分数量。</param>
        /// <param name="toIndex">目标槽位索引。</param>
        /// <returns>拆分成功返回 true，否则返回 false。</returns>
        public bool SplitSlot(int fromIndex, int count, int toIndex)
        {
            EnsureData();
            return data.SplitSlot(fromIndex, count, toIndex, maxStackCount);
        }

        /// <summary>
        /// 获取指定物品在背包中的总数量。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <returns>当前背包中该物品的总数量。</returns>
        public int GetCount(int itemId)
        {
            EnsureData();
            return data.GetCount(itemId);
        }

        /// <summary>
        /// 判断背包中是否拥有足够数量的指定物品。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">需要检查的数量。</param>
        /// <returns>数量足够返回 true，否则返回 false。</returns>
        public bool HasEnough(int itemId, int count)
        {
            EnsureData();
            return data.HasEnough(itemId, count);
        }

        /// <summary>
        /// 判断背包中是否存在指定物品。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <returns>存在至少一个该物品返回 true。</returns>
        public bool Contains(int itemId)
        {
            EnsureData();
            return data.Contains(itemId);
        }

        /// <summary>
        /// 尝试读取物品配置数据。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="itemData">读取到的物品配置。</param>
        /// <returns>物品存在返回 true，否则返回 false。</returns>
        public bool TryGetItemData(int itemId, out ItemData itemData)
        {
            EnsureInitialized();
            return itemDatabase.TryGet(itemId, out itemData);
        }

        /// <summary>
        /// 获取指定槽位的快照。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <returns>槽位快照。</returns>
        public InventorySlotData GetSlot(int index)
        {
            EnsureData();
            return data.GetSlot(index);
        }

        /// <summary>
        /// 获取所有槽位的快照。
        /// </summary>
        /// <returns>槽位快照列表。</returns>
        public IReadOnlyList<InventorySlotData> GetSlots()
        {
            EnsureData();
            return data.GetSlotsSnapshot();
        }

        /// <summary>
        /// 从背包数据快照加载槽位数据。
        /// </summary>
        /// <param name="source">来源背包数据。</param>
        public void Load(InventoryData source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            EnsureInitialized();
            EnsureData();

            InventoryData normalized = new InventoryData();
            normalized.NormalizeCapacity(capacity);

            IReadOnlyList<InventorySlotData> sourceSlots = source.GetSlotsSnapshot();
            for (int i = 0; i < sourceSlots.Count && i < capacity; i++)
            {
                InventorySlotData slot = sourceSlots[i];
                if (slot.IsEmpty)
                {
                    continue;
                }

                EnsureItemExists(slot.itemId);
                normalized.SetSlot(i, slot.itemId, slot.count, maxStackCount);
            }

            data.CopyFrom(normalized);
        }

        /// <summary>
        /// 导出当前背包数据快照。
        /// </summary>
        /// <returns>可用于存档的背包数据。</returns>
        public InventoryData ExportData()
        {
            EnsureData();
            return data.Clone();
        }

        /// <summary>
        /// 清空背包中的全部槽位。
        /// </summary>
        public void Clear()
        {
            EnsureData();
            data.Clear();
        }

        private void EnsureData()
        {
            if (data == null)
            {
                data = new InventoryData();
            }

            data.NormalizeCapacity(capacity);
        }

        private void EnsureInitialized()
        {
            if (itemDatabase != null)
            {
                return;
            }

            if (TryInitializeFromGameDatabase(false))
            {
                return;
            }

            throw new InvalidOperationException("[InventoryManager] IItemDatabase 未注册，请先完成 GameDatabase 注册。");
        }

        private bool TryInitializeFromGameDatabase(bool logWarning)
        {
            if (itemDatabase != null)
            {
                return true;
            }

            if (GameDatabase.TryGet(out IItemDatabase database))
            {
                itemDatabase = database;
                return true;
            }

            if (logWarning)
            {
                Debug.LogWarning("[InventoryManager] GameDatabase 尚未注册 IItemDatabase，稍后使用背包时会再次尝试拉取。");
            }

            return false;
        }

        private void EnsureItemExists(int itemId)
        {
            EnsureInitialized();
            if (!itemDatabase.TryGet(itemId, out _))
            {
                throw new KeyNotFoundException($"[InventoryManager] 未找到物品配置，itemId: {itemId}");
            }
        }
    }
}
