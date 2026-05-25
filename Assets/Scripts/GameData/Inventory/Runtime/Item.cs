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
        [SerializeField] private int count = 1;

        private SpriteRenderer spriteRenderer;
        private BoxCollider2D pickupCollider;
        private ItemData currentItem;

        public int ItemId => itemId;
        public int Count => Mathf.Max(1, count);
        public ItemData CurrentItem => currentItem;
        public Collider2D PickupCollider => pickupCollider;

        /// <summary>
        /// 初始化世界物品数据。
        /// </summary>
        /// <param name="newItemId">物品编号。</param>
        /// <param name="newCount">物品数量。</param>
        public void Initialize(int newItemId, int newCount)
        {
            itemId = newItemId;
            SetCount(newCount);
            EnsureComponents();
            ApplyItem();
            ApplyColliderSettings();
        }

        /// <summary>
        /// 设置世界物品数量。
        /// </summary>
        /// <param name="newCount">新的物品数量。</param>
        public void SetCount(int newCount)
        {
            count = Mathf.Max(1, newCount);
        }

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
            count = Mathf.Max(1, count);
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

            if (spriteRenderer == null) return;

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
            if (pickupCollider == null || spriteRenderer == null || spriteRenderer.sprite == null) return;

            pickupCollider.size = spriteRenderer.bounds.size;
            pickupCollider.offset = new Vector2(0, spriteRenderer.sprite.bounds.center.y);
        }
    }
}
