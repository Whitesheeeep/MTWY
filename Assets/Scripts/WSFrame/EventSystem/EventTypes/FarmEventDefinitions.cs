using UnityEngine;

namespace WS_Modules.CustomEventSystem
{
    /// <summary>
    /// Farm 跨系统事件枚举。
    /// </summary>
    public enum E_FarmEvent
    {
        start = EventIdRange.FarmStart,
        HarvestRewardRequested = start + 1,
        PlantSeedConsumeRequested,
        end,
    }

    /// <summary>
    /// 请求消耗播种种子的事件参数。
    /// </summary>
    public readonly struct FarmPlantSeedConsumeRequestedEventArgs
    {
        /// <summary>
        /// 播种发生的地图 ID。
        /// </summary>
        public string MapId { get; }

        /// <summary>
        /// 播种发生的地图格子。
        /// </summary>
        public Vector3Int Cell { get; }

        /// <summary>
        /// 被种下的作物配置 ID。
        /// </summary>
        public int CropDataId { get; }

        /// <summary>
        /// 需要消耗的种子物品 ID。
        /// </summary>
        public int SeedItemId { get; }

        /// <summary>
        /// 需要消耗的种子数量。
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// 创建播种种子消耗请求事件参数。
        /// </summary>
        public FarmPlantSeedConsumeRequestedEventArgs(
            string mapId,
            Vector3Int cell,
            int cropDataId,
            int seedItemId,
            int count)
        {
            MapId = mapId;
            Cell = cell;
            CropDataId = cropDataId;
            SeedItemId = seedItemId;
            Count = count;
        }
    }
    /// <summary>
    /// 请求发放作物收获奖励的事件参数。
    /// </summary>
    public readonly struct FarmHarvestRewardRequestedEventArgs
    {
        /// <summary>
        /// 收获发生的地图 ID。
        /// </summary>
        public string MapId { get; }

        /// <summary>
        /// 收获发生的地图格子。
        /// </summary>
        public Vector3Int Cell { get; }

        /// <summary>
        /// 被收获的作物配置 ID。
        /// </summary>
        public int CropDataId { get; }

        /// <summary>
        /// 收获产物物品 ID。
        /// </summary>
        public int HarvestItemId { get; }

        /// <summary>
        /// 本次实际收获数量。
        /// </summary>
        public int HarvestCount { get; }

        /// <summary>
        /// 创建作物收获奖励发放请求事件参数。
        /// </summary>
        public FarmHarvestRewardRequestedEventArgs(
            string mapId,
            Vector3Int cell,
            int cropDataId,
            int harvestItemId,
            int harvestCount)
        {
            MapId = mapId;
            Cell = cell;
            CropDataId = cropDataId;
            HarvestItemId = harvestItemId;
            HarvestCount = harvestCount;
        }
    }
}
