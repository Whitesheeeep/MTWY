using System.Collections.Generic;
using UnityEngine;

namespace GameData
{
    public sealed class ItemDatabase : IItemDatabase
    {
        private readonly Dictionary<int, ItemData> itemMap = new Dictionary<int, ItemData>();
        private readonly List<ItemData> items = new List<ItemData>();

        public ItemDatabase(ItemDataList_SO dataList)
        {
            Initialize(dataList);
        }

        public bool TryGet(int id, out ItemData item)
        {
            return itemMap.TryGetValue(id, out item);
        }

        public ItemData Get(int id)
        {
            if (TryGet(id, out ItemData item))
            {
                return item;
            }

            throw new KeyNotFoundException($"[ItemDatabase] Item id not found: {id}");
        }

        public IReadOnlyList<ItemData> GetAll()
        {
            return items;
        }

        public void Clear()
        {
            itemMap.Clear();
            items.Clear();
        }

        private void Initialize(ItemDataList_SO dataList)
        {
            Clear();

            if (dataList == null)
            {
                Debug.LogError("[ItemDatabase] ItemDataList_SO is null.");
                return;
            }

            if (dataList.items == null)
            {
                Debug.LogWarning($"[ItemDatabase] Item list is null: {dataList.name}");
                return;
            }

            foreach (ItemData item in dataList.items)
            {
                if (item == null)
                {
                    Debug.LogWarning($"[ItemDatabase] Null item skipped in {dataList.name}.");
                    continue;
                }

                if (itemMap.ContainsKey(item.Id))
                {
                    Debug.LogError($"[ItemDatabase] Duplicate item id skipped: {item.Id}, name: {item.name}");
                    continue;
                }

                itemMap.Add(item.Id, item);
                items.Add(item);
            }
        }
    }
}
