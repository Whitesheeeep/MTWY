using System;

namespace GameData
{
    [Flags]
    public enum MapGridCellFlags
    {
        None = 0,
        Blocked = 1 << 0,
        Water = 1 << 1,
        CanDig = 1 << 2,
        CanDropItem = 1 << 3,
        CanPlaceFurniture = 1 << 4,
        NpcObstacle = 1 << 5
    }
}
