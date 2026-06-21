using System;
using CursorSystem;
using GameData;
using Gameplay.TimeSystem;

namespace FarmSystem
{
    // 单条规则只描述一个业务前置条件，方便不同动作复用。
    // 后续表现层如果需要知道“为什么不能交互”，可以复用 reason 文本或在外层映射 UI 提示。
    /// <summary>
    /// 农田动作规则集合。每个规则只判断一个前置条件，便于在不同动作间复用。
    /// </summary>
    internal sealed class MapCellContextRule : IFarmActionRule
    {
        public bool IsMatch(FarmRuleContext context, out string reason)
        {
            if (context.TargetType == CursorTargetType.MapCell &&
                context.SelectedItemData != null)
            {
                reason = string.Empty;
                return true;
            }

            reason = "目标不是有效的地图格子交互";
            return false;
        }
    }

    internal sealed class SelectedItemTypeRule : IFarmActionRule
    {
        private readonly E_ItemType requiredItemType;

        public SelectedItemTypeRule(E_ItemType requiredItemType)
        {
            this.requiredItemType = requiredItemType;
        }

        public bool IsMatch(FarmRuleContext context, out string reason)
        {
            if (context.SelectedItemType == requiredItemType)
            {
                reason = string.Empty;
                return true;
            }

            reason = $"当前选中物品类型不匹配，需要 {requiredItemType}，当前 {context.SelectedItemType}";
            return false;
        }
    }
    internal sealed class SelectedItemAnyTypeRule : IFarmActionRule
    {
        private readonly E_ItemType[] allowedItemTypes;

        public SelectedItemAnyTypeRule(params E_ItemType[] allowedItemTypes)
        {
            this.allowedItemTypes = allowedItemTypes ?? Array.Empty<E_ItemType>();
        }

        public bool IsMatch(FarmRuleContext context, out string reason)
        {
            for (int i = 0; i < allowedItemTypes.Length; i++)
            {
                if (context.SelectedItemType == allowedItemTypes[i])
                {
                    reason = string.Empty;
                    return true;
                }
            }

            reason = $"当前选中物品类型不匹配，当前 {context.SelectedItemType}";
            return false;
        }
    }

    internal sealed class ValidTargetCellRule : IFarmActionRule
    {
        public bool IsMatch(FarmRuleContext context, out string reason)
        {
            if (context.SelectedItemData != null &&
                MapGridManager.Instance.TryGetCell(context.MapId, context.TargetCell, out _))
            {
                reason = string.Empty;
                return true;
            }

            reason = "目标不是有效的地图格子交互";
            return false;
        }
    }

    internal sealed class HasCellFlagRule : IFarmActionRule
    {
        private readonly MapGridCellFlags requiredFlag;

        public HasCellFlagRule(MapGridCellFlags requiredFlag)
        {
            this.requiredFlag = requiredFlag;
        }

        public bool IsMatch(FarmRuleContext context, out string reason)
        {
            if ((context.CellInfo.FinalFlags & requiredFlag) == requiredFlag)
            {
                reason = string.Empty;
                return true;
            }

            reason = $"目标格子缺少必要标记 {requiredFlag}";
            return false;
        }
    }

    internal sealed class TilledRule : IFarmActionRule
    {
        public bool IsMatch(FarmRuleContext context, out string reason)
        {
            if (context.IsTilled)
            {
                reason = string.Empty;
                return true;
            }

            reason = "目标格子还不是耕地";
            return false;
        }
    }

    internal sealed class NotTilledRule : IFarmActionRule
    {
        public bool IsMatch(FarmRuleContext context, out string reason)
        {
            if (!context.IsTilled)
            {
                reason = string.Empty;
                return true;
            }

            reason = "目标格子已经是耕地";
            return false;
        }
    }

    internal sealed class PlantedRule : IFarmActionRule
    {
        public bool IsMatch(FarmRuleContext context, out string reason)
        {
            if (context.IsPlanted)
            {
                reason = string.Empty;
                return true;
            }

            reason = "目标格子没有已种植作物";
            return false;
        }
    }

    internal sealed class NotPlantedRule : IFarmActionRule
    {
        public bool IsMatch(FarmRuleContext context, out string reason)
        {
            if (!context.IsPlanted)
            {
                reason = string.Empty;
                return true;
            }

            reason = "目标格子已经种植了作物";
            return false;
        }
    }

    internal sealed class PlantableSeasonRule : IFarmActionRule
    {
        public bool IsMatch(FarmRuleContext context, out string reason)
        {
            if (!context.Farm.TryResolvePlantingCropData(context.SelectedItemData, out CropData cropData))
            {
                reason = "找不到种子对应的作物配置";
                return false;
            }

            if (cropData.PlantableSeasons == null || cropData.PlantableSeasons.Count == 0)
            {
                reason = string.Empty;
                return true;
            }

            GameSeason currentSeason = GameTimeManager.Instance.CurrentTime.Value.Season;
            if (cropData.PlantableSeasons.Contains(currentSeason))
            {
                reason = string.Empty;
                return true;
            }

            reason = $"当前季节不可播种 cropDataId={cropData.Id}, currentSeason={currentSeason}";
            return false;
        }
    }

    internal sealed class MatureCropRule : IFarmActionRule
    {
        public bool IsMatch(FarmRuleContext context, out string reason)
        {
            if (context.IsCropMature)
            {
                reason = string.Empty;
                return true;
            }

            reason = "目标作物尚未成熟";
            return false;
        }
    }
}