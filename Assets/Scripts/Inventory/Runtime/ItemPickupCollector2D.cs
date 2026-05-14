using System;
using GameData;
using UnityEngine;
using WS_Modules.LogModule;

namespace Inventory
{
    /// <summary>
    /// 2D 物品拾取收集器，碰撞到带有 Item 组件的物体时尝试加入背包。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemPickupCollector2D : MonoBehaviour
    {
        private const int PickupCount = 1;

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryCollect(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryCollect(collision.collider);
        }

        private void TryCollect(Collider2D targetCollider)
        {
            if (targetCollider == null)
            {
                return;
            }

            Item item = targetCollider.GetComponentInParent<Item>();
            if (item == null || item.CurrentItem is { canPickedUp: false })
            {
                return;
            }

            InventoryManager manager = InventoryManager.Instance;
            if (manager == null)
            {
                Debug.LogError("[ItemPickupCollector2D] 场景中不存在 InventoryManager，无法拾取物品。");
                return;
            }

            try
            {
                if (!manager.TryAddItem(item.ItemId, PickupCount))
                {
                    Debug.Log($"[ItemPickupCollector2D] 背包空间不足，无法拾取 itemId: {item.ItemId}");
                    return;
                }
                else
                {
                    WSLog.LogSuccess($"[ItemPickupCollector2D] 成功拾取 itemId: {item.ItemId}, count: {PickupCount}");
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[ItemPickupCollector2D] 拾取物品失败，itemId: {item.ItemId}, error: {exception.Message}");
                return;
            }

            Destroy(item.gameObject);
        }
    }
}
