#if UNITY_EDITOR
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
        [InfoBox("这个组件只通过场景中的 InventoryManager 测试背包生产接口，不直接操作 InventoryData。")]
        [Title("操作参数")]
        [SerializeField] private int itemId = 1001;
        [SerializeField] private int count = 1;
        [SerializeField] private int fromIndex;
        [SerializeField] private int toIndex = 1;

        /// <summary>
        /// 通过 InventoryManager 添加物品，用于测试 Bar 优先、剩余进入 Bag 的规则。
        /// </summary>
        [Button("Manager 添加物品")]
        public void AddItemByManager()
        {
            InventoryManager manager = GetManager();
            if (manager == null)
            {
                return;
            }

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
            if (manager == null)
            {
                return;
            }

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
            if (manager == null)
            {
                return;
            }

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
            if (manager == null)
            {
                return;
            }

            bool success = manager.SetBagCount(itemId, count);
            Debug.Log($"[InventoryOdinTester] Manager 设置 Bag 数量 itemId={itemId}, count={count}, success={success}");
            PrintManagerSlots();
        }

        /// <summary>
        /// 移动 Bag 内部槽位。
        /// </summary>
        [Button("Manager 移动 Bag 槽位")]
        public void MoveBagSlotByManager()
        {
            InventoryManager manager = GetManager();
            if (manager == null)
            {
                return;
            }

            bool success = manager.MoveBagSlot(fromIndex, toIndex);
            Debug.Log($"[InventoryOdinTester] Manager 移动 Bag 槽位 from={fromIndex}, to={toIndex}, success={success}");
            PrintManagerSlots();
        }

        /// <summary>
        /// 合并 Bag 内部槽位。
        /// </summary>
        [Button("Manager 合并 Bag 槽位")]
        public void MergeBagSlotsByManager()
        {
            InventoryManager manager = GetManager();
            if (manager == null)
            {
                return;
            }

            bool success = manager.MergeBagSlots(fromIndex, toIndex);
            Debug.Log($"[InventoryOdinTester] Manager 合并 Bag 槽位 from={fromIndex}, to={toIndex}, success={success}");
            PrintManagerSlots();
        }

        /// <summary>
        /// 拆分 Bag 内部槽位。
        /// </summary>
        [Button("Manager 拆分 Bag 槽位")]
        public void SplitBagSlotByManager()
        {
            InventoryManager manager = GetManager();
            if (manager == null)
            {
                return;
            }

            bool success = manager.SplitBagSlot(fromIndex, count, toIndex);
            Debug.Log($"[InventoryOdinTester] Manager 拆分 Bag 槽位 from={fromIndex}, to={toIndex}, count={count}, success={success}");
            PrintManagerSlots();
        }

        /// <summary>
        /// 打印场景中 InventoryManager 的 Bar 和 Bag 数据。
        /// </summary>
        [Button("Manager 打印 Bar/Bag")]
        public void PrintManagerSlots()
        {
            InventoryManager manager = GetManager();
            if (manager == null)
            {
                return;
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"[InventoryOdinTester] Capacity Bag={manager.BagCapacity}, Max={manager.Capacity}, Bar={manager.BarCapacity}");
            builder.AppendLine("[InventoryOdinTester] Manager Bar Slots");
            AppendSlots(builder, manager.GetBarSlots());
            builder.AppendLine("[InventoryOdinTester] Manager Bag Slots");
            AppendSlots(builder, manager.GetBagSlots());
            Debug.Log(builder.ToString());
        }

        private void OnValidate()
        {
            count = Mathf.Max(1, count);
        }

        private static InventoryManager GetManager()
        {
            InventoryManager manager = InventoryManager.Instance;
            if (manager == null)
            {
                Debug.LogError("[InventoryOdinTester] 场景中不存在 InventoryManager。");
            }

            return manager;
        }

        private static void AppendSlots(StringBuilder builder, System.Collections.Generic.IReadOnlyList<InventorySlotData> slots)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlotData slot = slots[i];
                string content = slot.IsEmpty ? "Empty" : $"itemId={slot.itemId}, count={slot.count}";
                builder.AppendLine($"Slot {i}: {content}");
            }
        }
    }
}
#endif
