using CursorSystem;
using FarmSystem;
using GameData;
using UnityEngine;

namespace InteractionSystem
{
    /// <summary>
    /// 根据当前选中物品类型，把地图格子交互分发给具体业务系统。
    /// </summary>
    public sealed class ItemCellActionRouter
    {
        // 路由层只负责把 MapCell 动作交给对应业务系统。
        // 不在这里创建表现对象，也不处理 Tile、家具预览或作物动画。
        public bool TryHandle(ItemInteractionContext context)
        {
            switch (context.SelectedItemType)
            {
                case E_ItemType.HoeTool:
                    return FarmLandManager.Instance.TryTill(context);
                case E_ItemType.WaterTool:
                    return FarmLandManager.Instance.TryWater(context);
                case E_ItemType.Seed:
                    return FarmLandManager.Instance.TryPlant(context);
                case E_ItemType.CollectTool:
                case E_ItemType.ReapTool:
                    return FarmLandManager.Instance.TryHarvest(context);
                case E_ItemType.Furniture:
                    Debug.LogWarning("[ItemCellActionRouter] Furniture placement is not implemented yet.");
                    return false;
                default:
                    return false;
            }
        }
    }
}
