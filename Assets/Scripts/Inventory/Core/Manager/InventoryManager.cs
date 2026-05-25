using System;
using System.Collections.Generic;
using GameData;
using Sirenix.OdinInspector;
using UnityEngine;
using WS_Modules.CustomEventSystem;
using WS_Modules.Singleton;

namespace Inventory
{
    /// <summary>
    /// 运行时背包管理器，统一管理 Bar 和 Bag 数据，并通过 Scheduler 协调二者之间的数据流转。
    /// </summary>
    public sealed class InventoryManager : SingletonMonoBase<InventoryManager>
    {
        [SerializeField] private int barCapacity = 10;
        [SerializeField] private int bagCapacity = 30;
        [SerializeField] private int capacity = 60;
        [SerializeField] private int maxStackCount = 64;

        [ReadOnly]
        [SerializeField] private InventoryData barData = new InventoryData();

        [ReadOnly]
        [SerializeField] private InventoryData bagData = new InventoryData();

        private readonly IEventCenter<int> eventModule = new EventCenterModule<int>();
        private IItemDatabase itemDatabase;
        private InventoryScheduler scheduler;

        /// <summary>
        /// Bar 槽位容量。
        /// </summary>
        public int BarCapacity => barCapacity;

        /// <summary>
        /// Bag 当前已解锁槽位容量。
        /// </summary>
        public int BagCapacity => bagCapacity;

        /// <summary>
        /// Bag 最大容量上限。
        /// </summary>
        public int Capacity => capacity;

        /// <summary>
        /// 单个槽位的最大堆叠数量。
        /// </summary>
        public int MaxStackCount => maxStackCount;

        protected override void Awake()
        {
            base.Awake();
            // 背包和 bar 栏的数据初始化
            EnsureData();
            // 数据库拉取
            Initialize();
        }

        private void OnValidate()
        {
            NormalizeSettings();
            EnsureData();
        }

        /// <summary>
        /// 从 GameDatabase 拉取背包依赖的物品数据库。
        /// </summary>
        public void Initialize() => TryInitializeFromGameDatabase(true);

        /// <summary>
        /// 由外部推送背包依赖的物品数据库，适合明确启动顺序的场景。
        /// </summary>
        /// <param name="database">用于校验物品编号和读取物品配置的数据库。</param>
        public void Initialize(IItemDatabase database)
        {
            itemDatabase = database ?? throw new ArgumentNullException(nameof(database));
            EnsureData();
        }

        #region 事件注册
        public IUnRegister RegisterBarSlotChanged(Action<InventorySlotChangedEventArgs> handler) => eventModule.Register((int)InventoryEventType.BarSlotChanged, handler);


        public IUnRegister RegisterBagSlotChanged(Action<InventorySlotChangedEventArgs> handler) => eventModule.Register((int)InventoryEventType.BagSlotChanged, handler);

        public IUnRegister RegisterBarSlotsChanged(Action<InventorySlotsChangedEventArgs> handler) => eventModule.Register((int)InventoryEventType.BarSlotsChanged, handler);

        public IUnRegister RegisterBagSlotsChanged(Action<InventorySlotsChangedEventArgs> handler) => eventModule.Register((int)InventoryEventType.BagSlotsChanged, handler);
        #endregion

        /// <summary>
        /// 向背包中加入物品，优先进入 Bar，Bar 放不下的剩余物品进入 Bag。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">需要加入的数量。</param>
        /// <returns>未能放入背包的剩余数量，返回 0 表示全部放入。</returns>
        public int AddItem(int itemId, int count)
        {
            EnsureItemExists(itemId);
            EnsureData();

            InventoryChangeSet changeSet = new InventoryChangeSet();
            int remaining = scheduler.AddItem(itemId, count, maxStackCount, changeSet);
            NotifyChangeSet(changeSet);
            return remaining;
        }

        /// <summary>
        /// 尝试加入物品，只有全部放入 Bar/Bag 时才提交变化。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">需要加入的数量。</param>
        /// <returns>全部放入返回 true，空间不足返回 false。</returns>
        public bool TryAddItem(int itemId, int count)
        {
            EnsureItemExists(itemId);
            EnsureData();

            InventoryData barSnapshot = barData.Clone();
            InventoryData bagSnapshot = bagData.Clone();
            InventoryChangeSet changeSet = new InventoryChangeSet();
            int remaining = scheduler.AddItem(itemId, count, maxStackCount, changeSet);
            if (remaining == 0)
            {
                NotifyChangeSet(changeSet);
                return true;
            }

            barData.CopyFrom(barSnapshot);
            bagData.CopyFrom(bagSnapshot);
            return false;
        }

        /// <summary>
        /// 扩展 Bag 当前已解锁容量，成功后触发 Bag 整体刷新事件。
        /// </summary>
        /// <param name="additionalSlotCount">需要新增的槽位数量，必须大于 0 且不能超过最大容量。</param>
        /// <returns>扩容成功返回 true，参数无效或超过最大容量返回 false。</returns>
        public bool ExpandBagCapacity(int additionalSlotCount)
        {
            NormalizeSettings();
            EnsureData();

            if (additionalSlotCount <= 0 || bagCapacity + additionalSlotCount > capacity) return false;

            bagCapacity += additionalSlotCount;
            bagData.ExpandCapacity(additionalSlotCount);

            InventoryChangeSet changeSet = new InventoryChangeSet();
            changeSet.MarkBagAllChanged();
            NotifyChangeSet(changeSet);
            return true;
        }

        /// <summary>
        /// 从背包整体移除指定数量物品，优先从 Bag 移除，再从 Bar 移除。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">需要移除的数量。</param>
        /// <returns>移除成功返回 true，数量不足返回 false。</returns>
        public bool RemoveItem(int itemId, int count)
        {
            EnsureItemExists(itemId);
            EnsureData();
            if (!HasEnough(itemId, count))
            {
                return false;
            }

            InventoryChangeSet changeSet = new InventoryChangeSet();
            int remaining = count;

            int bagRemoveCount = Mathf.Min(bagData.GetCount(itemId), remaining);
            if (bagRemoveCount > 0)
            {
                List<int> bagChanged = new List<int>();
                bagData.RemoveItem(itemId, bagRemoveCount, bagChanged);
                changeSet.AddBagSlots(bagChanged);
                remaining -= bagRemoveCount;
            }

            if (remaining > 0)
            {
                List<int> barChanged = new List<int>();
                barData.RemoveItem(itemId, remaining, barChanged);
                changeSet.AddBarSlots(barChanged);
            }

            NotifyChangeSet(changeSet);
            return true;
        }

        /// <summary>
        /// 设置指定物品在 Bar 中的总数量。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">目标数量，0 表示清空该物品。</param>
        /// <returns>设置成功返回 true，槽位不足返回 false。</returns>
        public bool SetBarCount(int itemId, int count) => SetCount(barData, itemId, count, true);

        /// <summary>
        /// 设置指定物品在 Bag 中的总数量。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">目标数量，0 表示清空该物品。</param>
        /// <returns>设置成功返回 true，槽位不足返回 false。</returns>
        public bool SetBagCount(int itemId, int count) => SetCount(bagData, itemId, count, false);

        /// <summary>
        /// 移动 Bar 内部槽位。
        /// </summary>
        /// <param name="fromIndex">来源槽位索引。</param>
        /// <param name="toIndex">目标槽位索引。</param>
        /// <returns>移动成功返回 true。</returns>
        public bool MoveBarSlot(int fromIndex, int toIndex)
        {
            EnsureData();
            InventoryChangeSet changeSet = new InventoryChangeSet();
            bool success = scheduler.MoveBarSlot(fromIndex, toIndex, maxStackCount, changeSet);
            NotifyChangeSet(changeSet);
            return success;
        }

        /// <summary>
        /// 将 Bar 指定槽位整格丢弃到世界中，成功后通过全局事件中心通知世界生成掉落物。
        /// </summary>
        /// <param name="barIndex">Bar 槽位索引。</param>
        /// <returns>丢弃成功返回 true。</returns>
        public bool DropBarSlotToWorld(int barIndex)
        {
            Debug.Log($"[InventoryManager] 请求丢弃 Bar 槽位 index={barIndex}");
            return DropSlotToWorld(barData, barIndex, true);
        }

        /// <summary>
        /// 将 Bag 指定槽位整格丢弃到世界中，成功后通过全局事件中心通知世界生成掉落物。
        /// </summary>
        /// <param name="bagIndex">Bag 槽位索引。</param>
        /// <returns>丢弃成功返回 true。</returns>
        public bool DropBagSlotToWorld(int bagIndex)
        {
            Debug.Log($"[InventoryManager] 请求丢弃 Bag 槽位 index={bagIndex}");
            return DropSlotToWorld(bagData, bagIndex, false);
        }

        /// <summary>
        /// 移动 Bag 内部槽位。
        /// </summary>
        /// <param name="fromIndex">来源槽位索引。</param>
        /// <param name="toIndex">目标槽位索引。</param>
        /// <returns>移动成功返回 true。</returns>
        public bool MoveBagSlot(int fromIndex, int toIndex)
        {
            EnsureData();
            InventoryChangeSet changeSet = new InventoryChangeSet();
            bool success = scheduler.MoveBagSlot(fromIndex, toIndex, maxStackCount, changeSet);
            NotifyChangeSet(changeSet);
            return success;
        }

        /// <summary>
        /// 将 Bag 槽位移动到 Bar 槽位。
        /// </summary>
        /// <param name="bagIndex">Bag 槽位索引。</param>
        /// <param name="barIndex">Bar 槽位索引。</param>
        /// <returns>移动成功返回 true。</returns>
        public bool MoveBagToBar(int bagIndex, int barIndex)
        {
            EnsureData();
            InventoryChangeSet changeSet = new InventoryChangeSet();
            bool success = scheduler.MoveBagToBar(bagIndex, barIndex, maxStackCount, changeSet);
            NotifyChangeSet(changeSet);
            return success;
        }

        /// <summary>
        /// 将 Bar 槽位移动到 Bag 槽位。
        /// </summary>
        /// <param name="barIndex">Bar 槽位索引。</param>
        /// <param name="bagIndex">Bag 槽位索引。</param>
        /// <returns>移动成功返回 true。</returns>
        public bool MoveBarToBag(int barIndex, int bagIndex)
        {
            EnsureData();
            InventoryChangeSet changeSet = new InventoryChangeSet();
            bool success = scheduler.MoveBarToBag(barIndex, bagIndex, maxStackCount, changeSet);
            NotifyChangeSet(changeSet);
            return success;
        }

        /// <summary>
        /// 合并 Bag 内部槽位。
        /// </summary>
        /// <param name="fromIndex">来源槽位索引。</param>
        /// <param name="toIndex">目标槽位索引。</param>
        /// <returns>合并成功返回 true。</returns>
        public bool MergeBagSlots(int fromIndex, int toIndex)
        {
            EnsureData();
            List<int> changed = new List<int>();
            bool success = bagData.MergeSlots(fromIndex, toIndex, maxStackCount, changed);
            if (success)
            {
                InventoryChangeSet changeSet = new InventoryChangeSet();
                changeSet.AddBagSlots(changed);
                NotifyChangeSet(changeSet);
            }

            return success;
        }

        /// <summary>
        /// 拆分 Bag 内部槽位。
        /// </summary>
        /// <param name="fromIndex">来源槽位索引。</param>
        /// <param name="count">拆分数量。</param>
        /// <param name="toIndex">目标槽位索引。</param>
        /// <returns>拆分成功返回 true。</returns>
        public bool SplitBagSlot(int fromIndex, int count, int toIndex)
        {
            EnsureData();
            List<int> changed = new List<int>();
            bool success = bagData.SplitSlot(fromIndex, count, toIndex, maxStackCount, changed);
            if (success)
            {
                InventoryChangeSet changeSet = new InventoryChangeSet();
                changeSet.AddBagSlots(changed);
                NotifyChangeSet(changeSet);
            }

            return success;
        }

        /// <summary>
        /// 从 Bag 拆分指定数量到 Bar。
        /// </summary>
        /// <param name="bagIndex">Bag 来源槽位索引。</param>
        /// <param name="count">拆分数量。</param>
        /// <param name="barIndex">Bar 目标槽位索引。</param>
        /// <returns>拆分成功返回 true。</returns>
        public bool SplitBagToBar(int bagIndex, int count, int barIndex)
        {
            EnsureData();
            InventoryChangeSet changeSet = new InventoryChangeSet();
            bool success = scheduler.SplitBagToBar(bagIndex, count, barIndex, maxStackCount, changeSet);
            NotifyChangeSet(changeSet);
            return success;
        }

        /// <summary>
        /// 从 Bar 拆分指定数量到 Bag。
        /// </summary>
        /// <param name="barIndex">Bar 来源槽位索引。</param>
        /// <param name="count">拆分数量。</param>
        /// <param name="bagIndex">Bag 目标槽位索引。</param>
        /// <returns>拆分成功返回 true。</returns>
        public bool SplitBarToBag(int barIndex, int count, int bagIndex)
        {
            EnsureData();
            InventoryChangeSet changeSet = new InventoryChangeSet();
            bool success = scheduler.SplitBarToBag(barIndex, count, bagIndex, maxStackCount, changeSet);
            NotifyChangeSet(changeSet);
            return success;
        }

        /// <summary>
        /// 获取指定物品在 Bar 和 Bag 中的总数量。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <returns>物品总数量。</returns>
        public int GetCount(int itemId)
        {
            EnsureData();
            return barData.GetCount(itemId) + bagData.GetCount(itemId);
        }

        /// <summary>
        /// 判断 Bar 和 Bag 中是否拥有足够数量的指定物品。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">需要检查的数量。</param>
        /// <returns>数量足够返回 true。</returns>
        public bool HasEnough(int itemId, int count)
        {
            EnsureData();
            return GetCount(itemId) >= count;
        }

        /// <summary>
        /// 判断 Bar 或 Bag 中是否存在指定物品。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <returns>存在返回 true。</returns>
        public bool Contains(int itemId)
        {
            EnsureData();
            return GetCount(itemId) > 0;
        }

        /// <summary>
        /// 尝试读取物品配置数据。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="itemData">读取到的物品配置。</param>
        /// <returns>读取成功返回 true。</returns>
        public bool TryGetItemData(int itemId, out ItemData itemData)
        {
            EnsureInitialized();
            return itemDatabase.TryGet(itemId, out itemData);
        }

        /// <summary>
        /// 获取 Bar 指定槽位的快照。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <returns>槽位快照。</returns>
        public InventorySlotData GetBarSlot(int index)
        {
            EnsureData();
            return barData.GetSlot(index);
        }

        /// <summary>
        /// 获取 Bag 指定槽位的快照。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <returns>槽位快照。</returns>
        public InventorySlotData GetBagSlot(int index)
        {
            EnsureData();
            return bagData.GetSlot(index);
        }

        /// <summary>
        /// 获取所有 Bar 槽位快照。
        /// </summary>
        /// <returns>Bar 槽位快照列表。</returns>
        public IReadOnlyList<InventorySlotData> GetBarSlots()
        {
            EnsureData();
            return barData.GetSlotsSnapshot();
        }

        /// <summary>
        /// 获取所有 Bag 槽位快照。
        /// </summary>
        /// <returns>Bag 槽位快照列表。</returns>
        public IReadOnlyList<InventorySlotData> GetBagSlots()
        {
            EnsureData();
            return bagData.GetSlotsSnapshot();
        }

        /// <summary>
        /// 加载 Bar 数据。
        /// </summary>
        /// <param name="source">来源数据。</param>
        public void LoadBar(InventoryData source)
        {
            LoadData(source, barData, barCapacity);
            InventoryChangeSet changeSet = new InventoryChangeSet();
            changeSet.MarkBarAllChanged();
            NotifyChangeSet(changeSet);
        }

        /// <summary>
        /// 加载 Bag 数据。
        /// </summary>
        /// <param name="source">来源数据。</param>
        public void LoadBag(InventoryData source)
        {
            LoadData(source, bagData, bagCapacity);
            InventoryChangeSet changeSet = new InventoryChangeSet();
            changeSet.MarkBagAllChanged();
            NotifyChangeSet(changeSet);
        }

        /// <summary>
        /// 导出当前 Bar 数据快照。
        /// </summary>
        /// <returns>Bar 数据快照。</returns>
        public InventoryData ExportBarData()
        {
            EnsureData();
            return barData.Clone();
        }

        /// <summary>
        /// 导出当前 Bag 数据快照。
        /// </summary>
        /// <returns>Bag 数据快照。</returns>
        public InventoryData ExportBagData()
        {
            EnsureData();
            return bagData.Clone();
        }

        /// <summary>
        /// 清空 Bar 和 Bag 中的全部槽位。
        /// </summary>
        public void Clear()
        {
            EnsureData();
            barData.Clear(null);
            bagData.Clear(null);

            InventoryChangeSet changeSet = new InventoryChangeSet();
            changeSet.MarkBarAllChanged();
            changeSet.MarkBagAllChanged();
            NotifyChangeSet(changeSet);
        }

        private bool SetCount(InventoryData targetData, int itemId, int count, bool isBar)
        {
            EnsureItemExists(itemId);
            EnsureData();

            List<int> changed = new List<int>();
            bool success = targetData.SetCount(itemId, count, maxStackCount, changed);
            if (success)
            {
                InventoryChangeSet changeSet = new InventoryChangeSet();
                if (isBar)
                {
                    changeSet.AddBarSlots(changed);
                }
                else
                {
                    changeSet.AddBagSlots(changed);
                }

                NotifyChangeSet(changeSet);
            }

            return success;
        }

        private bool DropSlotToWorld(InventoryData targetData, int index, bool isBar)
        {
            EnsureData();
            string areaName = isBar ? "Bar" : "Bag";
            if (index < 0 || index >= targetData.SlotCount)
            {
                Debug.LogWarning($"[InventoryManager] 丢弃失败：{areaName} 槽位索引无效 index={index}, slotCount={targetData.SlotCount}");
                return false;
            }

            InventorySlotData slot = targetData.GetSlot(index);
            if (slot.IsEmpty)
            {
                Debug.LogWarning($"[InventoryManager] 丢弃失败：{areaName} 槽位为空 index={index}");
                return false;
            }

            EnsureItemExists(slot.itemId);
            if (!itemDatabase.TryGet(slot.itemId, out ItemData itemData))
            {
                Debug.LogWarning($"[InventoryManager] 丢弃失败：未找到物品配置 itemId={slot.itemId}");
                return false;
            }

            if (!itemData.canDropped)
            {
                Debug.LogWarning($"[InventoryManager] 丢弃失败：物品配置不允许丢弃 itemId={slot.itemId}, count={slot.count}");
                return false;
            }

            // check 完毕
            List<int> changed = new List<int>();
            if (!targetData.RemoveFromSlot(index, slot.count, changed))
            {
                Debug.LogWarning($"[InventoryManager] 丢弃失败：移除槽位数据失败 area={areaName}, index={index}, itemId={slot.itemId}, count={slot.count}");
                return false;
            }

            InventoryChangeSet changeSet = new InventoryChangeSet();
            if (isBar) changeSet.AddBarSlots(changed);
            else changeSet.AddBagSlots(changed);

            NotifyChangeSet(changeSet);
            Debug.Log($"[InventoryManager] 丢弃成功并准备触发世界生成事件 area={areaName}, index={index}, itemId={slot.itemId}, count={slot.count}");
            TriggerDropWorldItem(slot.itemId, slot.count);
            return true;
        }

        private static void TriggerDropWorldItem(int itemId, int count)
        {
            Debug.Log($"[InventoryManager] 触发全局丢弃事件 event={E_InventoryEvent.DropWorldItemRequested}, itemId={itemId}, count={count}");
            WS_Modules.CustomEventSystem.EventSystem.EventTrigger_Int(
                (int)E_InventoryEvent.DropWorldItemRequested,
                new InventoryDropWorldItemEventArgs(itemId, count));
        }

        private void EnsureData()
        {
            NormalizeSettings();
            barData ??= new InventoryData();
            bagData ??= new InventoryData();
            barData.NormalizeCapacity(barCapacity);
            bagData.NormalizeCapacity(bagCapacity);
            scheduler = new InventoryScheduler(barData, bagData);
        }

        private void NormalizeSettings()
        {
            barCapacity = Mathf.Max(0, barCapacity);
            bagCapacity = Mathf.Max(0, bagCapacity);
            capacity = Mathf.Max(bagCapacity, capacity);
            maxStackCount = Mathf.Max(1, maxStackCount);
        }

        private void LoadData(InventoryData source, InventoryData target, int targetCapacity)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            EnsureInitialized();
            EnsureData();

            InventoryData normalized = new InventoryData();
            normalized.NormalizeCapacity(targetCapacity);

            IReadOnlyList<InventorySlotData> sourceSlots = source.GetSlotsSnapshot();
            for (int i = 0; i < sourceSlots.Count && i < targetCapacity; i++)
            {
                InventorySlotData slot = sourceSlots[i];
                if (slot.IsEmpty)
                {
                    continue;
                }

                EnsureItemExists(slot.itemId);
                normalized.SetSlot(i, slot.itemId, slot.count, maxStackCount, null);
            }

            target.CopyFrom(normalized);
        }

        private void NotifyChangeSet(InventoryChangeSet changeSet)
        {
            if (changeSet == null || changeSet.IsEmpty)
            {
                return;
            }

            // bar 检查
            if (changeSet.BarAllChanged)
            {
                eventModule.EventTrigger(
                    (int)InventoryEventType.BarSlotsChanged,
                    InventorySlotsChangedEventArgs.Default);
            }
            else
            {
                foreach (int index in changeSet.BarChangedIndices)
                {
                    eventModule.EventTrigger(
                        (int)InventoryEventType.BarSlotChanged,
                        new InventorySlotChangedEventArgs(index));
                }
            }

            // bag 检查
            if (changeSet.BagAllChanged)
            {
                eventModule.EventTrigger(
                    (int)InventoryEventType.BagSlotsChanged,
                    InventorySlotsChangedEventArgs.Default);
            }
            else
            {
                foreach (int index in changeSet.BagChangedIndices)
                {
                    eventModule.EventTrigger(
                        (int)InventoryEventType.BagSlotChanged,
                        new InventorySlotChangedEventArgs(index));
                }
            }
        }

        // 尝试从 GameDatabase 拉取 IItemDatabase，成功后赋值并返回 true，失败返回 false。
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
