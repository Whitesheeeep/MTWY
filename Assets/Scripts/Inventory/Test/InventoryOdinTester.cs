#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Inventory
{
    /// <summary>
    /// 基于 Odin Inspector 的背包手动测试组件，通过场景中的 InventoryManager 触发生产路径。
    /// </summary>
    public sealed class InventoryOdinTester : MonoBehaviour
    {
        #region 字段
        [InfoBox("这个组件只通过场景中的 InventoryManager 测试背包生产接口，不直接操作 InventoryData。")]
        [Title("操作参数")]
        [SerializeField] private int itemId = 1001;
        [SerializeField] private int count = 1;
        [SerializeField] private int fromIndex;
        [SerializeField] private int toIndex = 1;
        [SerializeField] private int barIndex;
        [SerializeField] private int bagIndex;
        [SerializeField] private int expandSlotCount = 5;
        #endregion

        #region Manager 测试
        /// <summary>
        /// 通过 InventoryManager 添加物品，用于测试 Bar 优先、剩余进入 Bag 的规则。
        /// </summary>
        [Button("Manager 添加物品")]
        public void AddItemByManager()
        {
            InventoryManager manager = GetManager();
            if (manager == null) return;

            int remaining = manager.AddItem(itemId, count);
            Debug.Log($"[InventoryOdinTester] Manager 添加物品 itemId={itemId}, count={count}, remaining={remaining}");
            PrintManagerSlots();
        }

        /// <summary>
        /// 通过 InventoryManager 移除物品。
        /// </summary>
        [Button("Manager 移除物品")]
        public void RemoveItemByManager()
        {
            InventoryManager manager = GetManager();
            if (manager == null) return;

            bool success = manager.RemoveItem(itemId, count);
            Debug.Log($"[InventoryOdinTester] Manager 移除物品 itemId={itemId}, count={count}, success={success}");
            PrintManagerSlots();
        }

        /// <summary>
        /// 设置指定物品在 Bar 中的总数量。
        /// </summary>
        [Button("Manager 设置 Bar 数量")]
        public void SetBarCountByManager()
        {
            InventoryManager manager = GetManager();
            if (manager == null) return;

            bool success = manager.SetBarCount(itemId, count);
            Debug.Log($"[InventoryOdinTester] Manager 设置 Bar 数量 itemId={itemId}, count={count}, success={success}");
            PrintManagerSlots();
        }

        /// <summary>
        /// 设置指定物品在 Bag 中的总数量。
        /// </summary>
        [Button("Manager 设置 Bag 数量")]
        public void SetBagCountByManager()
        {
            InventoryManager manager = GetManager();
            if (manager == null) return;

            bool success = manager.SetBagCount(itemId, count);
            Debug.Log($"[InventoryOdinTester] Manager 设置 Bag 数量 itemId={itemId}, count={count}, success={success}");
            PrintManagerSlots();
        }

        /// <summary>
        /// 运行时扩展 Bag 已解锁容量。
        /// </summary>
        [Button("Manager 扩容 Bag")]
        public void ExpandBagCapacityByManager()
        {
            InventoryManager manager = GetManager();
            if (manager == null) return;

            int beforeBagCapacity = manager.BagCapacity;
            int beforeSlotCount = manager.GetBagSlots().Count;
            bool success = manager.ExpandBagCapacity(expandSlotCount);
            Debug.Log($"[InventoryOdinTester] Manager 扩容 Bag additional={expandSlotCount}, success={success}, beforeCapacity={beforeBagCapacity}, afterCapacity={manager.BagCapacity}, beforeSlots={beforeSlotCount}, afterSlots={manager.GetBagSlots().Count}, maxCapacity={manager.Capacity}");
            PrintManagerSlots();
        }

        /// <summary>
        /// 检查 Bag 扩容结果是否符合当前 Manager 容量语义。
        /// </summary>
        [Button("测试 Bag 扩容结果")]
        public void CheckBagCapacityResult()
        {
            InventoryManager manager = GetManager();
            if (manager == null) return;

            bool capacityValid = manager.BagCapacity <= manager.Capacity;
            bool slotCountMatched = manager.GetBagSlots().Count == manager.BagCapacity;
            Debug.Log($"[InventoryOdinTester] 测试 Bag 扩容结果 capacityValid={capacityValid}, slotCountMatched={slotCountMatched}, bagCapacity={manager.BagCapacity}, maxCapacity={manager.Capacity}, slotCount={manager.GetBagSlots().Count}");
        }
        #endregion

        #region Container 测试
        /// <summary>
        /// 移动 Bar 内部槽位。
        /// </summary>
        [Button("Container 移动 Bar 槽位")]
        public void ContainerBarSlotMove()
        {
            InventoryManager manager = GetManager();
            if (manager == null) return;

            bool success = manager.BarContainer.MoveSlot(fromIndex, toIndex);
            Debug.Log($"[InventoryOdinTester] Container 移动 Bar 槽位 from={fromIndex}, to={toIndex}, success={success}");
            PrintManagerSlots();
        }

        /// <summary>
        /// 移动 Bag 内部槽位。
        /// </summary>
        [Button("Container 移动 Bag 槽位")]
        public void ContainerBagSlotMove()
        {
            InventoryManager manager = GetManager();
            if (manager == null) return;

            bool success = manager.BagContainer.MoveSlot(fromIndex, toIndex);
            Debug.Log($"[InventoryOdinTester] Container 移动 Bag 槽位 from={fromIndex}, to={toIndex}, success={success}");
            PrintManagerSlots();
        }

        /// <summary>
        /// 合并 Bag 内部槽位。
        /// </summary>
        [Button("Container 合并 Bag 槽位")]
        public void ContainerMergeBagSlot()
        {
            InventoryManager manager = GetManager();
            if (manager == null) return;

            bool success = manager.BagContainer.MergeSlots(fromIndex, toIndex);
            Debug.Log($"[InventoryOdinTester] Container 合并 Bag 槽位 from={fromIndex}, to={toIndex}, success={success}");
            PrintManagerSlots();
        }

        /// <summary>
        /// 拆分 Bag 内部槽位。
        /// </summary>
        [Button("Container 拆分 Bag 槽位")]
        public void ContainerBagSlotSplit()
        {
            InventoryManager manager = GetManager();
            if (manager == null) return;

            bool success = manager.BagContainer.SplitSlot(fromIndex, count, toIndex);
            Debug.Log($"[InventoryOdinTester] Container 拆分 Bag 槽位 from={fromIndex}, to={toIndex}, count={count}, success={success}");
            PrintManagerSlots();
        }
        #endregion

        #region Transfer 测试
        /// <summary>
        /// 将 Bar 槽位移动到 Bag 槽位。
        /// </summary>
        [Button("Transfer Bar 到 Bag")]
        public void TransferBarSlotToBag()
        {
            InventoryManager manager = GetManager();
            if (manager == null) return;

            bool success = InventorySlotTransferService.MoveSlot(manager.BarContainer, barIndex, manager.BagContainer, bagIndex);
            Debug.Log($"[InventoryOdinTester] Transfer Bar 到 Bag barIndex={barIndex}, bagIndex={bagIndex}, success={success}");
            PrintManagerSlots();
        }

        /// <summary>
        /// 将 Bag 槽位移动到 Bar 槽位。
        /// </summary>
        [Button("Transfer Bag 到 Bar")]
        public void TransferBagSlotToBar()
        {
            InventoryManager manager = GetManager();
            if (manager == null) return;

            bool success = InventorySlotTransferService.MoveSlot(manager.BagContainer, bagIndex, manager.BarContainer, barIndex);
            Debug.Log($"[InventoryOdinTester] Transfer Bag 到 Bar bagIndex={bagIndex}, barIndex={barIndex}, success={success}");
            PrintManagerSlots();
        }
        #endregion

        #region Drop 测试
        /// <summary>
        /// 通过通用槽位丢弃服务将 Bar 指定槽位整格丢弃到世界事件路径。
        /// </summary>
        [Button("通用服务丢弃 Bar 槽位")]
        public void DropBarSlotByService()
        {
            InventoryManager manager = GetManager();
            if (manager == null) return;

            bool success = InventorySlotWorldDropService.DropSlotToWorld(manager.BarContainer, manager.ItemDatabase, barIndex);
            Debug.Log($"[InventoryOdinTester] 通用服务丢弃 Bar 槽位 barIndex={barIndex}, success={success}");
            PrintManagerSlots();
        }

        /// <summary>
        /// 通过通用槽位丢弃服务将 Bag 指定槽位整格丢弃到世界事件路径。
        /// </summary>
        [Button("通用服务丢弃 Bag 槽位")]
        public void DropBagSlotByService()
        {
            InventoryManager manager = GetManager();
            if (manager == null) return;

            bool success = InventorySlotWorldDropService.DropSlotToWorld(manager.BagContainer, manager.ItemDatabase, bagIndex);
            Debug.Log($"[InventoryOdinTester] 通用服务丢弃 Bag 槽位 bagIndex={bagIndex}, success={success}");
            PrintManagerSlots();
        }
        #endregion

        #region Debug
        /// <summary>
        /// 打印场景中 InventoryManager 的 Bar 和 Bag 数据。
        /// </summary>
        [Button("Manager 打印 Bar/Bag")]
        public void PrintManagerSlots()
        {
            InventoryManager manager = GetManager();
            if (manager == null) return;

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"[InventoryOdinTester] Capacity Bag={manager.BagCapacity}, Max={manager.Capacity}, Bar={manager.BarCapacity}");
            builder.AppendLine("[InventoryOdinTester] Manager Bar Slots");
            AppendSlots(builder, manager.GetBarSlots());
            builder.AppendLine("[InventoryOdinTester] Manager Bag Slots");
            AppendSlots(builder, manager.GetBagSlots());
            Debug.Log(builder.ToString());
        }
        #endregion

        #region Unity LifeCycle
        private void OnValidate()
        {
            count = Mathf.Max(1, count);
            expandSlotCount = Mathf.Max(1, expandSlotCount);
        }
        #endregion

        #region Tools
        private static InventoryManager GetManager()
        {
            InventoryManager manager = InventoryManager.Instance;
            if (manager == null) Debug.LogError("[InventoryOdinTester] 场景中不存在 InventoryManager。");

            return manager;
        }

        private static void AppendSlots(StringBuilder builder, IReadOnlyList<InventorySlotData> slots)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlotData slot = slots[i];
                string content = slot.IsEmpty ? "Empty" : $"itemId={slot.itemId}, count={slot.count}";
                builder.AppendLine($"Slot {i}: {content}");
            }
        }
        #endregion
    }
}
#endif
