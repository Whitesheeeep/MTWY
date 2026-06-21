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
    /// 运行时背包管理器，统一管理玩家 Inventory 的 Bar 与 Bag 容器生命周期和聚合业务。
    /// </summary>
    public sealed class InventoryManager : SingletonMonoBase<InventoryManager>
    {
        [LabelText("Bar 容器")]
        [SerializeField] private InventoryDataContainer barContainer = new InventoryDataContainer(10);

        [LabelText("Bag 容器")]
        [SerializeField] private ExpandableInventoryDataContainer bagContainer = new ExpandableInventoryDataContainer(30, 60);

        private IItemDatabase itemDatabase;
        private bool initialized;

        /// <summary>
        /// InventoryManager 完成运行时依赖初始化时触发。
        /// </summary>
        public event Action Initialized;

        /// <summary>
        /// 当前 InventoryManager 是否已完成运行时依赖初始化。
        /// </summary>
        public bool IsInitialized => initialized;

        /// <summary>
        /// Bar 槽位容量。
        /// </summary>
        public int BarCapacity => barContainer?.Capacity ?? 0;

        /// <summary>
        /// Bag 当前已解锁槽位容量。
        /// </summary>
        public int BagCapacity => bagContainer?.Capacity ?? 0;

        /// <summary>
        /// Bag 最大容量上限。
        /// </summary>
        public int Capacity => bagContainer?.MaxCapacity ?? 0;

        /// <summary>
        /// 单个槽位的最大堆叠数量。
        /// </summary>
        public int MaxStackCount => InventoryConstants.MaxStackCount;

        /// <summary>
        /// 当前 Inventory 使用的物品数据库。
        /// </summary>
        public IItemDatabase ItemDatabase
        {
            get
            {
                ThrowIfNotInitialized();
                return itemDatabase;
            }
        }

        /// <summary>
        /// Bar 槽位容器。
        /// </summary>
        public InventoryDataContainer BarContainer
        {
            get
            {
                ThrowIfNotInitialized();
                return barContainer;
            }
        }

        /// <summary>
        /// Bag 槽位容器。
        /// </summary>
        public ExpandableInventoryDataContainer BagContainer
        {
            get
            {
                ThrowIfNotInitialized();
                return bagContainer;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            EnsureStorageData();
        }

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            EventSystem.Register_Int<FarmHarvestRewardRequestedEventArgs>(
                    (int)E_FarmEvent.HarvestRewardRequested,
                    OnFarmHarvestRewardRequested)
                .UnRegisterWhenGameObjectDisabled(gameObject);
            EventSystem.Register_Int<FarmPlantSeedConsumeRequestedEventArgs>(
                    (int)E_FarmEvent.PlantSeedConsumeRequested,
                    OnFarmPlantSeedConsumeRequested)
                .UnRegisterWhenGameObjectDisabled(gameObject);
        }

        /// <summary>
        /// 从 GameDatabase 拉取背包依赖的物品数据库。
        /// </summary>
        public void Initialize()
        {
            if (initialized) return;

            if (TryInitializeFromGameDatabase(true))
                InitializeRuntimeContainers();

            ThrowIfNotInitialized();
        }

        /// <summary>
        /// 由外部推送背包依赖的物品数据库，适合明确启动顺序的场景。
        /// </summary>
        /// <param name="database">用于校验物品编号和读取物品配置的数据库。</param>
        public void Initialize(IItemDatabase database)
        {
            if (initialized)
                throw new InvalidOperationException("[InventoryManager] Container 已经初始化，运行时不允许替换 IItemDatabase。");

            itemDatabase = database ?? throw new ArgumentNullException(nameof(database));
            InitializeRuntimeContainers();
        }

        #region 事件注册
        /// <summary>
        /// 注册 Bar 单槽位变化事件。
        /// </summary>
        /// <param name="handler">事件处理函数。</param>
        /// <returns>注销句柄。</returns>
        public IUnRegister RegisterBarSlotChanged(Action<InventorySlotChangedEventArgs> handler)
        {
            ThrowIfNotInitialized();
            return barContainer.RegisterSlotChanged(handler);
        }

        /// <summary>
        /// 注册 Bag 单槽位变化事件。
        /// </summary>
        /// <param name="handler">事件处理函数。</param>
        /// <returns>注销句柄。</returns>
        public IUnRegister RegisterBagSlotChanged(Action<InventorySlotChangedEventArgs> handler)
        {
            ThrowIfNotInitialized();
            return bagContainer.RegisterSlotChanged(handler);
        }

        /// <summary>
        /// 注册 Bar 槽位列表整体变化事件。
        /// </summary>
        /// <param name="handler">事件处理函数。</param>
        /// <returns>注销句柄。</returns>
        public IUnRegister RegisterBarSlotsChanged(Action<InventorySlotsChangedEventArgs> handler)
        {
            ThrowIfNotInitialized();
            return barContainer.RegisterSlotsChanged(handler);
        }

        /// <summary>
        /// 注册 Bag 槽位列表整体变化事件。
        /// </summary>
        /// <param name="handler">事件处理函数。</param>
        /// <returns>注销句柄。</returns>
        public IUnRegister RegisterBagSlotsChanged(Action<InventorySlotsChangedEventArgs> handler)
        {
            ThrowIfNotInitialized();
            return bagContainer.RegisterSlotsChanged(handler);
        }
        #endregion

        #region 聚合业务
        /// <summary>
        /// 向背包中加入物品，优先进入 Bar，Bar 放不下的剩余物品进入 Bag。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">需要加入的数量。</param>
        /// <returns>未能放入背包的剩余数量，返回 0 表示全部放入。</returns>
        public int AddItem(int itemId, int count)
        {
            EnsureItemExists(itemId);

            int remaining = barContainer.AddItem(itemId, count);
            if (remaining > 0)
                remaining = bagContainer.AddItem(itemId, remaining);

            return remaining;
        }

        /// <summary>
        /// 尝试加入物品，只有全部放入 Bar/Bag 时才提交变更。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">需要加入的数量。</param>
        /// <returns>全部放入返回 true，空间不足返回 false。</returns>
        public bool TryAddItem(int itemId, int count)
        {
            EnsureItemExists(itemId);

            InventoryData barSnapshot = barContainer.Data.Clone();
            InventoryData bagSnapshot = bagContainer.Data.Clone();
            List<int> barChanged = new List<int>();
            List<int> bagChanged = new List<int>();

            int remaining = barContainer.Data.AddItem(itemId, count, InventoryConstants.MaxStackCount, barChanged);
            if (remaining > 0)
                remaining = bagContainer.Data.AddItem(itemId, remaining, InventoryConstants.MaxStackCount, bagChanged);

            if (remaining == 0)
            {
                barContainer.NotifySlotsChanged(barChanged);
                bagContainer.NotifySlotsChanged(bagChanged);
                return true;
            }

            barContainer.Data.CopyFrom(barSnapshot);
            bagContainer.Data.CopyFrom(bagSnapshot);
            return false;
        }

        /// <summary>
        /// 扩展 Bag 当前已解锁容量，成功后触发 Bag 整体刷新事件。
        /// </summary>
        /// <param name="additionalSlotCount">需要新增的槽位数量，必须大于 0 且不能超过最大容量。</param>
        /// <returns>扩容成功返回 true，参数无效或超过最大容量返回 false。</returns>
        public bool ExpandBagCapacity(int additionalSlotCount)
        {
            return bagContainer.TryExpandCapacity(additionalSlotCount);
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
            if (!HasEnough(itemId, count)) return false;

            int remaining = count;
            int bagRemoveCount = Mathf.Min(bagContainer.GetCount(itemId), remaining);
            if (bagRemoveCount > 0)
            {
                bagContainer.RemoveItem(itemId, bagRemoveCount);
                remaining -= bagRemoveCount;
            }

            if (remaining > 0)
                barContainer.RemoveItem(itemId, remaining);

            return true;
        }

        /// <summary>
        /// 设置指定物品在 Bar 中的总数量。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">目标数量，0 表示清空该物品。</param>
        /// <returns>设置成功返回 true，槽位不足返回 false。</returns>
        public bool SetBarCount(int itemId, int count)
        {
            EnsureItemExists(itemId);
            return barContainer.SetCount(itemId, count);
        }

        /// <summary>
        /// 设置指定物品在 Bag 中的总数量。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">目标数量，0 表示清空该物品。</param>
        /// <returns>设置成功返回 true，槽位不足返回 false。</returns>
        public bool SetBagCount(int itemId, int count)
        {
            EnsureItemExists(itemId);
            return bagContainer.SetCount(itemId, count);
        }

        /// <summary>
        /// 获取指定物品在 Bar 和 Bag 中的总数量。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <returns>物品总数量。</returns>
        public int GetCount(int itemId)
        {
            return barContainer.GetCount(itemId) + bagContainer.GetCount(itemId);
        }

        /// <summary>
        /// 判断 Bar 和 Bag 中是否拥有足够数量的指定物品。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">需要检查的数量。</param>
        /// <returns>数量足够返回 true。</returns>
        public bool HasEnough(int itemId, int count)
        {
            return GetCount(itemId) >= count;
        }

        /// <summary>
        /// 判断 Bar 或 Bag 中是否存在指定物品。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <returns>存在返回 true。</returns>
        public bool Contains(int itemId)
        {
            return GetCount(itemId) > 0;
        }

        /// <summary>
        /// 清空 Bar 和 Bag 中的全部槽位。
        /// </summary>
        public void Clear()
        {
            barContainer.Clear();
            bagContainer.Clear();
        }
        #endregion

        #region 查询与存档
        /// <summary>
        /// 尝试读取物品配置数据。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="itemData">读取到的物品配置。</param>
        /// <returns>读取成功返回 true。</returns>
        public bool TryGetItemData(int itemId, out ItemData itemData)
        {
            ThrowIfNotInitialized();
            return itemDatabase.TryGet(itemId, out itemData);
        }

        /// <summary>
        /// 获取 Bar 指定槽位的快照。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <returns>槽位快照。</returns>
        public InventorySlotData GetBarSlot(int index)
        {
            return barContainer.GetSlot(index);
        }

        /// <summary>
        /// 获取 Bag 指定槽位的快照。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <returns>槽位快照。</returns>
        public InventorySlotData GetBagSlot(int index)
        {
            return bagContainer.GetSlot(index);
        }

        /// <summary>
        /// 获取所有 Bar 槽位快照。
        /// </summary>
        /// <returns>Bar 槽位快照列表。</returns>
        public IReadOnlyList<InventorySlotData> GetBarSlots()
        {
            return barContainer.GetSlotsSnapshot();
        }

        /// <summary>
        /// 获取所有 Bag 槽位快照。
        /// </summary>
        /// <returns>Bag 槽位快照列表。</returns>
        public IReadOnlyList<InventorySlotData> GetBagSlots()
        {
            return bagContainer.GetSlotsSnapshot();
        }

        /// <summary>
        /// 加载 Bar 数据。
        /// </summary>
        /// <param name="source">来源数据。</param>
        public void LoadBar(InventoryData source)
        {
            LoadData(source, barContainer.Data, barContainer.Capacity);
            barContainer.NotifyAllChanged();
        }

        /// <summary>
        /// 加载 Bag 数据。
        /// </summary>
        /// <param name="source">来源数据。</param>
        public void LoadBag(InventoryData source)
        {
            LoadData(source, bagContainer.Data, bagContainer.Capacity);
            bagContainer.NotifyAllChanged();
        }

        /// <summary>
        /// 导出当前 Bar 数据快照。
        /// </summary>
        /// <returns>Bar 数据快照。</returns>
        public InventoryData ExportBarData()
        {
            return barContainer.Data.Clone();
        }

        /// <summary>
        /// 导出当前 Bag 数据快照。
        /// </summary>
        /// <returns>Bag 数据快照。</returns>
        public InventoryData ExportBagData()
        {
            return bagContainer.Data.Clone();
        }
        #endregion

        // Farm 播种种子消耗请求通过事件总线进入背包，避免 Farm 直接依赖 Inventory。
        private void OnFarmPlantSeedConsumeRequested(FarmPlantSeedConsumeRequestedEventArgs args)
        {
            if (args.SeedItemId <= 0 || args.Count <= 0)
            {
                Debug.LogWarning($"[InventoryManager] 播种种子消耗参数无效 seedItemId={args.SeedItemId}, count={args.Count}", this);
                return;
            }

            if (!initialized)
            {
                Debug.LogWarning($"[InventoryManager] 尚未初始化，无法消耗播种种子 seedItemId={args.SeedItemId}, count={args.Count}", this);
                return;
            }

            bool success;
            try
            {
                success = RemoveItem(args.SeedItemId, args.Count);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[InventoryManager] 播种种子消耗失败 seedItemId={args.SeedItemId}, count={args.Count}, error={exception.Message}", this);
                return;
            }

            if (!success)
            {
                Debug.LogWarning($"[InventoryManager] 播种种子数量不足 seedItemId={args.SeedItemId}, count={args.Count}", this);
            }
        }
        // Farm 收获奖励请求通过事件总线进入背包，多余数量继续触发世界掉落事件。
        private void OnFarmHarvestRewardRequested(FarmHarvestRewardRequestedEventArgs args)
        {
            if (args.HarvestItemId <= 0 || args.HarvestCount <= 0)
            {
                Debug.LogWarning($"[InventoryManager] 收获奖励参数无效 itemId={args.HarvestItemId}, count={args.HarvestCount}", this);
                return;
            }

            if (!initialized)
            {
                Debug.LogWarning($"[InventoryManager] 尚未初始化，收获物直接掉落 itemId={args.HarvestItemId}, count={args.HarvestCount}", this);
                DropHarvestRewardToWorld(args.HarvestItemId, args.HarvestCount);
                return;
            }

            int remaining;
            try
            {
                remaining = AddItem(args.HarvestItemId, args.HarvestCount);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[InventoryManager] 收获物加入背包失败，将直接掉落 itemId={args.HarvestItemId}, count={args.HarvestCount}, error={exception.Message}", this);
                DropHarvestRewardToWorld(args.HarvestItemId, args.HarvestCount);
                return;
            }

            if (remaining > 0)
            {
                DropHarvestRewardToWorld(args.HarvestItemId, remaining);
            }
        }

        // 复用现有世界物品生成事件处理背包放不下的收获物。
        private static void DropHarvestRewardToWorld(int itemId, int count)
        {
            if (itemId <= 0 || count <= 0)
            {
                return;
            }

            EventSystem.EventTrigger_Int(
                (int)E_InventoryEvent.DropWorldItemRequested,
                new InventoryDropWorldItemEventArgs(itemId, count));
        }
        #region 内部生命周期
        private void EnsureStorageData()
        {
            barContainer ??= new InventoryDataContainer(10);
            bagContainer ??= new ExpandableInventoryDataContainer(30, 60);
            NormalizeSettings();
            barContainer.NormalizeCapacity(barContainer.Capacity);
            bagContainer.NormalizeCapacity(bagContainer.Capacity);
        }

        private void InitializeRuntimeContainers()
        {
            if (itemDatabase == null)
                throw new InvalidOperationException("[InventoryManager] IItemDatabase 未初始化，无法创建 InventoryDataContainer。");

            EnsureStorageData();
            barContainer.InitializeRuntime(itemDatabase);
            bagContainer.InitializeRuntime(itemDatabase);
            initialized = true;
            Initialized?.Invoke();
        }

        private void ThrowIfNotInitialized()
        {
            if (initialized) return;

            throw new InvalidOperationException("[InventoryManager] 尚未完成初始化，请确认外部依赖已在 Start 阶段完成。");
        }

        private void NormalizeSettings()
        {
            bagContainer?.NormalizeMaxCapacity(Capacity);
        }

        private void LoadData(InventoryData source, InventoryData target, int targetCapacity)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            InventoryData normalized = new InventoryData();
            normalized.NormalizeCapacity(targetCapacity);

            IReadOnlyList<InventorySlotData> sourceSlots = source.GetSlotsSnapshot();
            for (int i = 0; i < sourceSlots.Count && i < targetCapacity; i++)
            {
                InventorySlotData slot = sourceSlots[i];
                if (slot.IsEmpty) continue;

                EnsureItemExists(slot.itemId);
                normalized.SetSlot(i, slot.itemId, slot.count, InventoryConstants.MaxStackCount, null);
            }

            target.CopyFrom(normalized);
        }

        private bool TryInitializeFromGameDatabase(bool logWarning)
        {
            if (itemDatabase != null) return true;

            if (GameDatabase.TryGet(out IItemDatabase database))
            {
                itemDatabase = database;
                return true;
            }

            if (logWarning)
                Debug.LogWarning("[InventoryManager] GameDatabase 尚未注册 IItemDatabase，InventoryManager 暂时无法完成运行时初始化。");

            return false;
        }

        private void EnsureItemExists(int itemId)
        {
            ThrowIfNotInitialized();
            if (!itemDatabase.TryGet(itemId, out _))
                throw new KeyNotFoundException($"[InventoryManager] 未找到物品配置，itemId: {itemId}");
        }
        #endregion
    }
}
