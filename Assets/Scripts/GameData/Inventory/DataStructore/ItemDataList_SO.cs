using System.Collections.Generic;
using UnityEngine;

namespace GameData
{
    [CreateAssetMenu(fileName = "ItemDataList", menuName = "GameData/ItemData", order = 0)]
    public class ItemDataList_SO : ScriptableObject
    {
        public List<ItemData> items;
    }
}