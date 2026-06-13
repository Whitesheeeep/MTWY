using GameData;

namespace CursorSystem
{
    /// <summary>
    /// 工具类型判断工具，统一收口 ItemType 到工具语义的映射。
    /// </summary>
    public static class ToolTypeUtility
    {
        public static bool IsTool(E_ItemType itemType)
        {
            switch (itemType)
            {
                case E_ItemType.HoeTool:
                case E_ItemType.ChopTool:
                case E_ItemType.BreakTool:
                case E_ItemType.ReapTool:
                case E_ItemType.WaterTool:
                case E_ItemType.CollectTool:
                    return true;
                default:
                    return false;
            }
        }
    }
}
