using GameData;
using Inventory;
using UnityEngine;
using WS_Modules.LogModule;
using WS_Modules.Pooling;

namespace WorldItems
{
    /// <summary>
    /// Collects 2D world Items into Inventory and synchronizes world item records.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemPickupCollector2D : MonoBehaviour
    {
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
            if (targetCollider == null) return;

            Item item = targetCollider.GetComponentInParent<Item>();
            if (item == null || item.CurrentItem is { canPickedUp: false }) return;

            InventoryManager manager = InventoryManager.Instance;
            if (manager == null)
            {
                Debug.LogError("[ItemPickupCollector2D] Missing InventoryManager, cannot pick up item.");
                return;
            }

            int pickupCount = item.Count;
            int remaining = manager.AddItem(item.ItemId, pickupCount);
            int pickedCount = pickupCount - remaining;
            if (pickedCount <= 0)
            {
                Debug.Log($"[ItemPickupCollector2D] Inventory has no space for itemId: {item.ItemId}");
                return;
            }

            if (remaining > 0)
            {
                item.SetCount(remaining);
                WorldItemManager.UpdateRecordFromItem(item);
                WSLog.LogSuccess($"[ItemPickupCollector2D] Partially picked itemId: {item.ItemId}, picked={pickedCount}, remaining={remaining}");
                return;
            }

            WSLog.LogSuccess($"[ItemPickupCollector2D] Picked itemId: {item.ItemId}, count: {pickedCount}");
            WorldItemManager.RemoveRecordForItem(item);
            PoolManager.Instance.Recycle(item.gameObject);
        }
    }
}
