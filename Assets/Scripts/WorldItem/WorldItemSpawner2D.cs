using GameData;
using Inventory;
using UnityEngine;
using WS_Modules.CustomEventSystem;
using WS_Modules.LogModule;
using WS_Modules.Pooling;
using WS_Modules.SceneModule;
using WS_Modules.Singleton;

namespace WorldItems
{
    /// <summary>
    /// Spawns, refreshes, and recycles visible world Items through the object pool.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldItemSpawner2D : SingletonMonoBase<WorldItemSpawner2D>
    {
        [SerializeField] private string itemPrefabKey = @"Prefabs\ItemPrefab";
        [SerializeField] private string itemPrefabLabel = "Prefab";
        [SerializeField] private Transform itemParent;
        [SerializeField] private DropSourceModule dropSource;
        [SerializeField] private int prewarmCount = 8;
        [SerializeField] private int maxPoolCapacity = 64;

        private const string DEFAULT_ITEM_PREFAB_KEY = @"Prefabs\ItemPrefab";
        private const string DEFAULT_ITEM_PREFAB_LABEL = @"Prefab";

        protected override void Awake()
        {
            base.Awake();
            PrewarmItemPool();
        }

        private void OnEnable()
        {
            EventSystem.Register_Int<InventoryDropWorldItemEventArgs>(
                    (int)E_InventoryEvent.DropWorldItemRequested,
                    OnDropWorldItemRequested)
                .UnRegisterWhenGameObjectDisabled(gameObject);

            SceneSystem.RegisterLoadSucceeded(OnSceneLoadSucceeded)
                .UnRegisterWhenGameObjectDisabled(gameObject);

            // RefreshVisibleWorldItems();
        }

        private void OnDropWorldItemRequested(InventoryDropWorldItemEventArgs eventArgs)
        {
            if (!TryGetDropPosition(out Vector3 position))
            {
                Debug.LogWarning($"[WorldItemSpawner2D] Failed to calculate drop position itemId={eventArgs.ItemId}, count={eventArgs.Count}", this);
                return;
            }

            Item item = SpawnItem(eventArgs.ItemId, eventArgs.Count, position);
            if (item == null)
            {
                return;
            }

            WorldItemManager.CreateRecordForItem(item);
        }

        /// <summary>
        /// Rebuilds visible world Items from the current map records.
        /// </summary>
        public void RefreshVisibleWorldItems(string sceneName)
        {
            WSLog.Log($"[WorldItemSpawner2D] Refreshing visible world items for map {sceneName}");
            RecycleVisibleWorldItems();

            foreach (WorldItemRecord record in WorldItemManager.GetBucket(sceneName).Records)
            {
                Item item = SpawnItem(record.ItemId, record.Count, record.Position);
                if (item != null)
                {
                    bool bound = WorldItemManager.BindRecordToItem(item, record.InstanceId, sceneName);
                    if (!bound)
                    {
                        PoolManager.Instance.Recycle(item.gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a world Item from the pool and initializes its item data.
        /// </summary>
        private Item SpawnItem(int itemId, int count, Vector3 position)
        {
            GameObject itemObject = PoolManager.Instance.Get(itemPrefabKey, GetItemParent());
            if (itemObject == null)
            {
                Debug.LogWarning($"[WorldItemSpawner2D] Failed to get item prefab from pool. key={itemPrefabKey}, label={itemPrefabLabel}", this);
                return null;
            }

            itemObject.transform.position = position;
            itemObject.transform.rotation = Quaternion.identity;
            WSLog.Log($"[WorldItemSpawner2D] Spawning item: {itemObject.name} at {position}");
            if (!itemObject.TryGetComponent(out Item item))
            {
                Debug.LogWarning($"[WorldItemSpawner2D] Pooled prefab does not contain Item. key={itemPrefabKey}", itemObject);
                PoolManager.Instance.Recycle(itemObject);
                return null;
            }

            item.Initialize(itemId, count);
            return item;
        }

        /// <summary>
        /// Calculates the spawn position for an inventory item dropped into the world.
        /// </summary>
        private bool TryGetDropPosition(out Vector3 position)
        {
            dropSource.GetDropPose(transform, out Vector3 origin, out Vector2 direction, out float distance);
            position = origin + (Vector3)(direction * distance);
            return true;
        }

        private void OnSceneLoadSucceeded(SceneLoadSucceededEventArgs args)
        {
            RefreshVisibleWorldItems(args.LoadInfo.SceneName);
        }

        private void PrewarmItemPool()
        {
            int initCount = Mathf.Max(1, prewarmCount);
            int capacity = maxPoolCapacity <= 0 ? -1 : maxPoolCapacity;
            PoolManager.Instance.Prewarm(itemPrefabKey, initCount, capacity);
        }

        private void RecycleVisibleWorldItems()
        {
            Item[] items = GetItemParent().GetComponentsInChildren<Item>(true);
            for (int i = items.Length - 1; i >= 0; i--)
            {
                Item item = items[i];
                if (item == null ||
                    !item.TryGetComponent(out WorldItemIdentity identity) ||
                    !identity.HasIdentity)
                {
                    continue;
                }

                identity.Clear();
                PoolManager.Instance.Recycle(item.gameObject);
            }
        }

        private Transform GetItemParent()
        {
            return itemParent != null ? itemParent : transform;
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(itemPrefabKey)) itemPrefabKey = DEFAULT_ITEM_PREFAB_KEY;
            if (string.IsNullOrWhiteSpace(itemPrefabLabel)) itemPrefabLabel = DEFAULT_ITEM_PREFAB_LABEL;
            prewarmCount = Mathf.Max(1, prewarmCount);
            if (maxPoolCapacity != -1) maxPoolCapacity = Mathf.Max(1, maxPoolCapacity);
            dropSource.Validate();
        }

        /// <summary>
        /// Provides origin, direction, and distance for inventory drops into the world.
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
        }
    }
}
