using GameData;

namespace CursorSystem
{
    public static class ToolMapCellInteractionRules
    {
        public static bool CanInteract(ItemData toolData, MapGridCellInfo cellInfo)
        {
            if (toolData == null)
            {
                return false;
            }

            switch (toolData.itemType)
            {
                case E_ItemType.HoeTool:
                    return HasFlag(cellInfo, MapGridCellFlags.CanDig);
                default:
                    return false;
            }
        }

        private static bool HasFlag(MapGridCellInfo cellInfo, MapGridCellFlags flag)
        {
            return (cellInfo.FinalFlags & flag) == flag;
        }
    }
}
