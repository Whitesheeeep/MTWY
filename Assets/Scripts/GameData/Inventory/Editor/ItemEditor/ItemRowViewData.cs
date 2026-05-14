using UnityEngine;

namespace GameData.Editor
{
    internal readonly struct ItemRowViewData
    {
        public ItemRowViewData(ItemData item)
        {
            Item = item;
            Icon = item?.icon;
            Name = string.IsNullOrWhiteSpace(item?.name) ? "<Unnamed>" : item.name;
            Detail = item == null ? string.Empty : $"{item.Id} · {item.itemType}";
        }

        public ItemData Item { get; }
        public Sprite Icon { get; }
        public string Name { get; }
        public string Detail { get; }
    }
}
