using System;
using Sirenix.OdinInspector;
using UnityEngine;
using WS_Modules.Extensions;

namespace GameData
{
    [DisallowMultipleComponent]
    public sealed class Item : MonoBehaviour
    {
        [SerializeField] private int itemId;

        private SpriteRenderer spriteRenderer;
        private BoxCollider2D pickupCollider;
        private ItemData currentItem;

        public int ItemId => itemId;
        public ItemData CurrentItem => currentItem;
        public Collider2D PickupCollider => pickupCollider;

        private void Reset()
        {
            EnsureComponents();
            ApplyItem();
        }

        private void Awake()
        {
            EnsureComponents();
        }

        private void Start()
        {
            ApplyItem();
        }

        private void OnValidate()
        {
            EnsureComponents();
            ApplyColliderSettings();
            // ApplyItem();
        }

        [Button]
        private void ApplyItem()
        {
            currentItem = GameDatabase.Get<IItemDatabase>().Get(itemId);
            if (currentItem == null)
            {
                Debug.LogWarning($"未找到 ID 为 {itemId} 的 ItemData。请检查数据库中是否存在该 ID 的数据。");
                return;
            }

            spriteRenderer.sprite = currentItem.worldIcon ?? currentItem.icon;
        }

        private void EnsureComponents()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (pickupCollider == null)
            {
                pickupCollider = GetComponent<Collider2D>() as BoxCollider2D ?? gameObject.GetOrAddComponent<BoxCollider2D>();
            }

            if (pickupCollider == null)
            {
                pickupCollider = gameObject.AddComponent<BoxCollider2D>();
            }
        }

        [Button]
        private void ApplyColliderSettings()
        {
            pickupCollider.size = spriteRenderer.bounds.size;
            pickupCollider.offset = new Vector2(0, spriteRenderer.sprite.bounds.center.y);
        }
    }
}
