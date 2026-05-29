using System;
using GameData;
using UnityEngine;
using WS_Modules.CustomEventSystem;

namespace Inventory
{
    /// <summary>
    /// Inventory 槽位世界丢弃服务，负责从任意槽位容器整格移除物品并通知世界生成掉落物。
    /// </summary>
    public static class InventorySlotWorldDropService
    {
        /// <summary>
        /// 将指定容器槽位中的整格物品丢弃到世界中。
        /// </summary>
        /// <param name="container">来源槽位容器。</param>
        /// <param name="itemDatabase">物品数据库。</param>
        /// <param name="index">来源槽位索引。</param>
        /// <returns>丢弃成功返回 true。</returns>
        public static bool DropSlotToWorld(
            IInventorySlotContainer container,
            IItemDatabase itemDatabase,
            int index)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (itemDatabase == null) throw new ArgumentNullException(nameof(itemDatabase));

            if (index < 0 || index >= container.SlotCount)
            {
                Debug.LogWarning($"[InventorySlotWorldDropService] 丢弃失败：槽位索引无效 index={index}, slotCount={container.SlotCount}");
                return false;
            }

            InventorySlotData slot = container.GetSlot(index);
            if (slot.IsEmpty)
            {
                Debug.LogWarning($"[InventorySlotWorldDropService] 丢弃失败：槽位为空 index={index}");
                return false;
            }

            if (!itemDatabase.TryGet(slot.itemId, out ItemData itemData))
            {
                Debug.LogWarning($"[InventorySlotWorldDropService] 丢弃失败：未找到物品配置 itemId={slot.itemId}");
                return false;
            }

            if (!itemData.canDropped)
            {
                Debug.LogWarning($"[InventorySlotWorldDropService] 丢弃失败：物品配置不允许丢弃 itemId={slot.itemId}, count={slot.count}");
                return false;
            }

            if (!container.RemoveFromSlot(index, slot.count))
            {
                Debug.LogWarning($"[InventorySlotWorldDropService] 丢弃失败：移除槽位数据失败 index={index}, itemId={slot.itemId}, count={slot.count}");
                return false;
            }

            TriggerDropWorldItem(slot.itemId, slot.count);
            Debug.Log($"[InventorySlotWorldDropService] 丢弃成功 itemId={slot.itemId}, count={slot.count}, index={index}");
            return true;
        }

        private static void TriggerDropWorldItem(int itemId, int count)
        {
            Debug.Log($"[InventorySlotWorldDropService] 触发全局丢弃事件 event={E_InventoryEvent.DropWorldItemRequested}, itemId={itemId}, count={count}");
            WS_Modules.CustomEventSystem.EventSystem.EventTrigger_Int(
                (int)E_InventoryEvent.DropWorldItemRequested,
                new InventoryDropWorldItemEventArgs(itemId, count));
        }
    }
}
