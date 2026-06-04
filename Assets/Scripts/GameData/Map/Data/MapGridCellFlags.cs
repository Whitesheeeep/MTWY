using System;

namespace GameData
{
    /// <summary>
    /// 描述地图格子的静态或运行时逻辑属性。
    /// </summary>
    [Flags]
    public enum MapGridCellFlags
    {
        /// <summary>
        /// 没有额外逻辑属性。
        /// </summary>
        None = 0,

        /// <summary>
        /// 格子被阻挡，默认不可通行。
        /// </summary>
        Blocked = 1 << 0,

        /// <summary>
        /// 格子是水域，默认不可通行。
        /// </summary>
        Water = 1 << 1,

        /// <summary>
        /// 格子允许被锄地或挖掘。
        /// </summary>
        CanDig = 1 << 2,

        /// <summary>
        /// 格子允许掉落物品。
        /// </summary>
        CanDropItem = 1 << 3,

        /// <summary>
        /// 格子允许放置家具。
        /// </summary>
        CanPlaceFurniture = 1 << 4,

        /// <summary>
        /// 格子属于 NPC 障碍层，默认不可通行。
        /// </summary>
        NpcObstacle = 1 << 5
    }
}
