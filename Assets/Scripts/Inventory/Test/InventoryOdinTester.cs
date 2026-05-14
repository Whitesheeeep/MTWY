using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Inventory
{
    /// <summary>
    /// 基于 Odin Inspector 的背包手动测试组件，用于在 Inspector 中触发背包数据操作。
    /// </summary>
    public sealed class InventoryOdinTester : MonoBehaviour
    {
        [InfoBox("这个组件用于在 Inspector 中测试 InventoryData 的各种操作。配置好参数后点击对应按钮即可执行操作，结果会打印在控制台中。")]

        [Title("背包配置")]
        [SerializeField] private int capacity = 30;
        [SerializeField] private int maxStackCount = 64;

        [Title("操作参数")]
        [SerializeField] private int itemId = 1001;
        [SerializeField] private int count = 1;
        [SerializeField] private int fromIndex;
        [SerializeField] private int toIndex = 1;

        [Title("测试数据")]
        [SerializeField] private InventoryData data = new InventoryData();

        /// <summary>
        /// 将测试背包重置为当前配置的容量。
        /// </summary>
        [Button("重置背包")]
        public void ResetInventory()
        {
            data = new InventoryData();
            NormalizeData();
            LogSlots("重置背包");
        }

        /// <summary>
        /// 向测试背包中加入当前配置的物品数量。
        /// </summary>
        [Button("添加物品")]
        public void AddItem()
        {
            NormalizeData();
            int remaining = data.AddItem(itemId, count, maxStackCount);
            LogSlots($"添加物品 itemId={itemId}, count={count}, remaining={remaining}");
        }

        /// <summary>
        /// 从测试背包中移除当前配置的物品数量。
        /// </summary>
        [Button("移除物品")]
        public void RemoveItem()
        {
            NormalizeData();
            bool success = data.RemoveItem(itemId, count);
            LogSlots($"移除物品 itemId={itemId}, count={count}, success={success}");
        }

        /// <summary>
        /// 将指定物品设置为当前配置的总数量。
        /// </summary>
        [Button("设置总数量")]
        public void SetCount()
        {
            NormalizeData();
            bool success = data.SetCount(itemId, count, maxStackCount);
            LogSlots($"设置总数量 itemId={itemId}, count={count}, success={success}");
        }

        /// <summary>
        /// 把来源槽位移动到目标槽位，目标槽位不同物品时会交换。
        /// </summary>
        [Button("移动槽位")]
        public void MoveSlot()
        {
            NormalizeData();
            bool success = data.MoveSlot(fromIndex, toIndex, maxStackCount);
            LogSlots($"移动槽位 from={fromIndex}, to={toIndex}, success={success}");
        }

        /// <summary>
        /// 将来源槽位尽量合并到目标槽位。
        /// </summary>
        [Button("合并槽位")]
        public void MergeSlots()
        {
            NormalizeData();
            bool success = data.MergeSlots(fromIndex, toIndex, maxStackCount);
            LogSlots($"合并槽位 from={fromIndex}, to={toIndex}, success={success}");
        }

        /// <summary>
        /// 从来源槽位拆分当前配置数量到目标槽位。
        /// </summary>
        [Button("拆分槽位")]
        public void SplitSlot()
        {
            NormalizeData();
            bool success = data.SplitSlot(fromIndex, count, toIndex, maxStackCount);
            LogSlots($"拆分槽位 from={fromIndex}, to={toIndex}, count={count}, success={success}");
        }

        /// <summary>
        /// 清空测试背包中的所有槽位。
        /// </summary>
        [Button("清空背包")]
        public void Clear()
        {
            NormalizeData();
            data.Clear();
            LogSlots("清空背包");
        }

        /// <summary>
        /// 打印当前测试背包的所有槽位。
        /// </summary>
        [Button("打印槽位")]
        public void PrintSlots()
        {
            NormalizeData();
            LogSlots("打印槽位");
        }

        private void OnValidate()
        {
            capacity = Mathf.Max(0, capacity);
            maxStackCount = Mathf.Max(1, maxStackCount);
            count = Mathf.Max(1, count);
            NormalizeData();
        }

        private void NormalizeData()
        {
            if (data == null)
            {
                data = new InventoryData();
            }

            data.NormalizeCapacity(capacity);
        }

        private void LogSlots(string title)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"[InventoryOdinTester] {title}");

            for (int i = 0; i < data.SlotCount; i++)
            {
                InventorySlotData slot = data.GetSlot(i);
                string content = slot.IsEmpty ? "Empty" : $"itemId={slot.itemId}, count={slot.count}";
                builder.AppendLine($"Slot {i}: {content}");
            }

            Debug.Log(builder.ToString());
        }
    }
}
