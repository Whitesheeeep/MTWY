using GameData;
using UnityEngine;
using WS_Modules.CustomEventSystem;

namespace Inventory
{
    /// <summary>
    /// 背包世界掉落物生成器，监听全局 Inventory 丢弃事件并生成世界 Item。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventoryWorldDropSpawner2D : MonoBehaviour
    {
        [SerializeField] private Item itemPrefab;
        [SerializeField] private Transform itemParent;
        [SerializeField] private DropSourceModule dropSource = DropSourceModule.CreateDefault();

        private void OnEnable()
        {
            Debug.Log("[InventoryWorldDropSpawner2D] 注册全局丢弃事件监听。", this);
            EventSystem.Register_Int<InventoryDropWorldItemEventArgs>(
                    (int)E_InventoryEvent.DropWorldItemRequested,
                    OnDropWorldItemRequested)
                .UnRegisterWhenGameObjectDisabled(gameObject);
        }

        private void OnDropWorldItemRequested(InventoryDropWorldItemEventArgs eventArgs)
        {
            Debug.Log($"[InventoryWorldDropSpawner2D] 收到丢弃事件 itemId={eventArgs.ItemId}, count={eventArgs.Count}", this);
            if (itemPrefab == null)
            {
                Debug.LogWarning("[InventoryWorldDropSpawner2D] 缺少 Item 预制体，无法生成世界掉落物。", this);
                return;
            }

            if (!TryGetDropPosition(out Vector3 position))
            {
                Debug.LogWarning($"[InventoryWorldDropSpawner2D] 计算掉落位置失败 itemId={eventArgs.ItemId}, count={eventArgs.Count}", this);
                return;
            }

            Item item = Instantiate(itemPrefab, position, Quaternion.identity, itemParent);
            item.Initialize(eventArgs.ItemId, eventArgs.Count);
            Debug.Log($"[InventoryWorldDropSpawner2D] 已生成世界物品 itemId={eventArgs.ItemId}, count={eventArgs.Count}, position={position}", item);
        }

        private bool TryGetDropPosition(out Vector3 position)
        {
            dropSource.GetDropPose(transform, out Vector3 origin, out Vector2 direction, out float distance);
            position = origin + (Vector3)(direction * distance);
            Debug.Log($"[InventoryWorldDropSpawner2D] 掉落位置 origin={origin}, direction={direction}, distance={distance}, position={position}", this);
            return true;
        }

        private void OnValidate()
        {
            dropSource.Validate();
        }

        /// <summary>
        /// 背包世界丢弃来源模块，用于提供掉落物生成的世界坐标、方向和距离。
        /// </summary>
        [System.Serializable]
        private struct DropSourceModule
        {
            [SerializeField] private Transform dropOrigin;
            [SerializeField] private Vector2 dropDirection;
            [SerializeField] private float dropDistance;

            public void GetDropPose(Transform fallbackOrigin, out Vector3 origin, out Vector2 direction, out float distance)
            {
                origin = dropOrigin != null ? dropOrigin.position : fallbackOrigin.position;
                direction = dropDirection.sqrMagnitude > 0f ? dropDirection.normalized : Vector2.down;
                distance = Mathf.Max(0f, dropDistance);
            }

            public void Validate()
            {
                dropDistance = Mathf.Max(0f, dropDistance);
                if (dropDirection.sqrMagnitude <= 0f) dropDirection = Vector2.down;
            }

            public static DropSourceModule CreateDefault()
            {
                return new DropSourceModule
                {
                    dropOrigin = null,
                    dropDirection = Vector2.down,
                    dropDistance = 1f
                };
            }
        }
    }
}
