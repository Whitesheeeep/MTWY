using System;
using UnityEngine;

namespace GameData
{
    public enum E_ItemType
    {
        Seed, Commodity, Furniture,
        HoeTool, ChopTool, BreakTool, ReapTool, WaterTool, CollectTool,
        ReapableScenery,
        None
    }

    [Serializable]
    public class ItemData
    {
        public int Id;
        public string name;
        public string description;
        public E_ItemType itemType;
        public Sprite icon;
        public Sprite worldIcon;
        public int itemUseRadius;
        public bool canPickedUp;
        public bool canDropped;
        public bool canCarried;
        public int price;
        [Range(0,100)]
        public int sellPercent;
    }
}
