using GameData;
using FarmSystem;

namespace CursorSystem
{
    public static class ItemMapCellInteractionRules
    {
        public static bool CanInteract(ItemData selectedItemData, MapGridCellInfo cellInfo)
        {
            if (selectedItemData == null)
            {
                return false;
            }

            switch (selectedItemData.itemType)
            {
                case E_ItemType.HoeTool:
                    return CanHoeInteract(selectedItemData, cellInfo);
                case E_ItemType.WaterTool:
                    return FarmLandManager.Instance.CanWater(CreateContext(selectedItemData, cellInfo));
                case E_ItemType.Seed:
                    return FarmLandManager.Instance.CanPlant(CreateContext(selectedItemData, cellInfo));
                case E_ItemType.CollectTool:
                case E_ItemType.ReapTool:
                    return FarmLandManager.Instance.CanHarvest(CreateContext(selectedItemData, cellInfo));
                case E_ItemType.Furniture:
                    return HasFlag(cellInfo, MapGridCellFlags.CanPlaceFurniture);
                default:
                    return false;
            }
        }

        private static bool CanHoeInteract(ItemData selectedItemData, MapGridCellInfo cellInfo)
        {
            ItemInteractionContext context = CreateContext(selectedItemData, cellInfo);
            return FarmLandManager.Instance.CanRemoveCrop(context) || FarmLandManager.Instance.CanTill(context);
        }
        private static bool HasFlag(MapGridCellInfo cellInfo, MapGridCellFlags flag)
        {
            return (cellInfo.FinalFlags & flag) == flag;
        }

        private static ItemInteractionContext CreateContext(ItemData selectedItemData, MapGridCellInfo cellInfo)
        {
            return new ItemInteractionContext(
                null,
                selectedItemData,
                default,
                default,
                default,
                cellInfo.CellPosition,
                selectedItemData?.itemUseRadius ?? 0,
                true,
                null,
                CursorTargetType.MapCell,
                cellInfo);
        }
    }
}
