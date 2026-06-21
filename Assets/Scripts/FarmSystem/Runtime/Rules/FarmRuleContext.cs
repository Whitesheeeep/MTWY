using CursorSystem;
using GameData;
using UnityEngine;

namespace FarmSystem
{
    /// <summary>
    /// 农田规则校验上下文，在通用交互上下文外补充当前地图和农田状态查询。
    /// </summary>
    public readonly struct FarmRuleContext
    {
        // 规则层只读上下文：把通用交互数据、当前地图和 Farm 状态查询集中到一起。
        // 表现层不要依赖该类型驱动表现，它只服务动作规则校验。
        public FarmRuleContext(
            ItemInteractionContext interactionContext,
            string mapId,
            FarmLandManager farm)
        {
            InteractionContext = interactionContext;
            MapId = mapId;
            Farm = farm;
        }

        public ItemInteractionContext InteractionContext { get; }
        public string MapId { get; }
        public FarmLandManager Farm { get; }
        public Vector3Int TargetCell => InteractionContext.TargetCell;
        public ItemData SelectedItemData => InteractionContext.SelectedItemData;
        public E_ItemType SelectedItemType => InteractionContext.SelectedItemType;
        public CursorTargetType TargetType => InteractionContext.TargetType;
        public MapGridCellInfo CellInfo => InteractionContext.CellInfo;
        public bool IsTilled => Farm.IsTilled(MapId, TargetCell);
        public bool IsWatered => Farm.IsWatered(MapId, TargetCell);
        public bool IsPlanted => Farm.IsPlanted(MapId, TargetCell);
        public bool IsCropMature => Farm.IsCropMature(MapId, TargetCell);
    }
}