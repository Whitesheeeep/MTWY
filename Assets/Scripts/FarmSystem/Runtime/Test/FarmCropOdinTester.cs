#if UNITY_EDITOR
using CursorSystem;
using GameData;
using Gameplay.TimeSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FarmSystem
{
    /// <summary>
    /// 基于 Odin Inspector 的农田作物手动测试组件，用于验证播种季节限制、作物成长和收获流程。
    /// </summary>
    public sealed class FarmCropOdinTester : MonoBehaviour
    {
        [Title("目标格子")]
        [SerializeField] private Vector3Int targetCell;
        [SerializeField] private bool useMapGridCellInfo = true;
        [SerializeField] private bool fallbackToSyntheticCanDigCellInfo = true;

        [Title("测试物品")]
        [SerializeField] private int seedItemId = 1003;
        [SerializeField] private int hoeToolItemId = 1012;
        [SerializeField] private int waterToolItemId = 1013;
        [SerializeField] private int harvestToolItemId = 1011;
        [SerializeField] private int itemUseRadius = 1;

        [Title("测试时间")]
        [SerializeField] private GameSeason testSeason = GameSeason.Spring;
        [SerializeField, MinValue(1)] private int testYear = 1;
        [SerializeField, MinValue(1)] private int testDay = 1;
        [SerializeField, Range(0, 23)] private int testHour = 6;
        [SerializeField, Range(0, 59)] private int testMinute;

        [Title("成长测试")]
        [SerializeField, MinValue(1)] private int maxGrowthDays = 20;
        [SerializeField] private bool waterBeforeEachGrowthDay = true;

        [Title("只读状态")]
        [ShowInInspector, ReadOnly] private string CurrentMapId => MapGridManager.Instance.CurrentMapId;
        [ShowInInspector, ReadOnly] private string CurrentTimeText => GameTimeManager.Instance?.CurrentTime.Value.ToString();
        [ShowInInspector, ReadOnly] private bool IsTilled => FarmLandManager.Instance.IsTilled(CurrentMapId, targetCell);
        [ShowInInspector, ReadOnly] private bool IsWatered => FarmLandManager.Instance.IsWatered(CurrentMapId, targetCell);
        [ShowInInspector, ReadOnly] private bool IsPlanted => FarmLandManager.Instance.IsPlanted(CurrentMapId, targetCell);
        [ShowInInspector, ReadOnly] private bool IsMature => FarmLandManager.Instance.IsCropMature(CurrentMapId, targetCell);

        /// <summary>
        /// 将当前游戏时间设置到测试季节，用于验证季节播种规则。
        /// </summary>
        [Button("设置到测试季节", ButtonSizes.Large)]
        public void SetTimeToTestSeason()
        {
            SetTimeToSeason(testSeason);
        }

        /// <summary>
        /// 检查当前测试季节下，完整 Farm 播种规则是否通过。
        /// </summary>
        [Button("完整 CanPlant 检查", ButtonSizes.Large)]
        public void CheckCanPlantInCurrentSeason()
        {
            SetTimeToSeason(testSeason);
            CheckCanPlantForSeason(testSeason);
        }

        /// <summary>
        /// 执行一次耕地、浇水、播种流程，用于验证播种规则和作物状态写入。
        /// </summary>
        [Button("耕地+浇水+播种", ButtonSizes.Large)]
        public void TillWaterAndPlant()
        {
            SetTimeToSeason(testSeason);
            bool tillResult = FarmLandManager.Instance.TryTill(CreateContext(E_ItemType.HoeTool, hoeToolItemId));
            bool waterResult = FarmLandManager.Instance.TryWater(CreateContext(E_ItemType.WaterTool, waterToolItemId));
            bool plantResult = FarmLandManager.Instance.TryPlant(CreateContext(E_ItemType.Seed, seedItemId));
            PrintCropState($"耕地+浇水+播种结果 till={tillResult}, water={waterResult}, plant={plantResult}");
        }

        /// <summary>
        /// 推进一个游戏日，用于触发 GameTimeManager 的 DayStarted 事件和作物成长结算。
        /// </summary>
        [Button("推进一天", ButtonSizes.Large)]
        public void AdvanceOneDay()
        {
            GameTimeManager.Instance.AdvanceMinutes(GameTimeManager.MinutesPerDay);
            PrintCropState("推进一天后状态");
        }

        /// <summary>
        /// 从当前测试季节开始执行播种、逐日成长，并在成熟后尝试收获。
        /// </summary>
        [Button("完整成长并尝试收获", ButtonSizes.Large)]
        public void RunGrowthAndHarvestFlow()
        {
            SetTimeToSeason(testSeason);

            bool tillResult = FarmLandManager.Instance.TryTill(CreateContext(E_ItemType.HoeTool, hoeToolItemId));
            bool waterResult = FarmLandManager.Instance.TryWater(CreateContext(E_ItemType.WaterTool, waterToolItemId));
            bool plantResult = FarmLandManager.Instance.TryPlant(CreateContext(E_ItemType.Seed, seedItemId));
            Debug.Log($"[FarmCropOdinTester] 初始流程 till={tillResult}, water={waterResult}, plant={plantResult}");

            for (int day = 1; day <= maxGrowthDays; day++)
            {
                if (waterBeforeEachGrowthDay)
                {
                    FarmLandManager.Instance.TryWater(CreateContext(E_ItemType.WaterTool, waterToolItemId));
                }

                GameTimeManager.Instance.AdvanceMinutes(GameTimeManager.MinutesPerDay);
                PrintCropState($"成长测试第 {day} 天后");

                if (FarmLandManager.Instance.IsCropMature(CurrentMapId, targetCell))
                {
                    bool harvestResult = FarmLandManager.Instance.TryHarvest(CreateContext(E_ItemType.CollectTool, harvestToolItemId));
                    PrintCropState($"成熟后尝试收获 harvest={harvestResult}");
                    return;
                }
            }

            Debug.LogWarning($"[FarmCropOdinTester] 在 maxGrowthDays={maxGrowthDays} 天内未成熟，未执行收获。请检查 CropData.GrowthStages、浇水状态或测试参数。");
        }

        /// <summary>
        /// 分别切到四季并执行完整 CanPlant 检查。该检查包含已耕地、未种植等全部播种规则。
        /// </summary>
        [Button("四季完整 CanPlant 检查", ButtonSizes.Large)]
        public void CheckAllSeasonsCanPlant()
        {
            CheckCanPlantForSeason(GameSeason.Spring);
            CheckCanPlantForSeason(GameSeason.Summer);
            CheckCanPlantForSeason(GameSeason.Autumn);
            CheckCanPlantForSeason(GameSeason.Winter);
        }

        /// <summary>
        /// 只检查种子配置的可播季节，不检查目标格是否已耕地或已种植。
        /// </summary>
        [Button("四季仅季节规则检查", ButtonSizes.Large)]
        public void CheckAllSeasonsPlantableOnly()
        {
            CheckPlantableSeasonOnly(GameSeason.Spring);
            CheckPlantableSeasonOnly(GameSeason.Summer);
            CheckPlantableSeasonOnly(GameSeason.Autumn);
            CheckPlantableSeasonOnly(GameSeason.Winter);
        }

        private void CheckCanPlantForSeason(GameSeason season)
        {
            SetTimeToSeason(season);
            ItemInteractionContext context = CreateContext(E_ItemType.Seed, seedItemId);
            bool canPlant = FarmLandManager.Instance.CanPlant(context, out string reason);
            Debug.Log($"[FarmCropOdinTester] 四季完整 CanPlant 检查 season={season}, seedItemId={seedItemId}, mapId={CurrentMapId}, cell={targetCell}, canPlant={canPlant}, reason={reason}, tilled={IsTilled}, planted={IsPlanted}, watered={IsWatered}, mature={IsMature}");
        }

        private void CheckPlantableSeasonOnly(GameSeason season)
        {
            SetTimeToSeason(season);
            bool resolved = FarmLandManager.Instance.TryResolvePlantingCropData(CreateItemData(E_ItemType.Seed, seedItemId), out CropData cropData);
            bool seasonAllowed = resolved && IsSeasonAllowed(cropData, GameTimeManager.Instance.CurrentTime.Value.Season);
            string seasonsText = resolved && cropData.PlantableSeasons != null
                ? string.Join(",", cropData.PlantableSeasons)
                : "null";

            Debug.Log($"[FarmCropOdinTester] 四季仅季节规则检查 season={season}, seedItemId={seedItemId}, resolved={resolved}, cropDataId={(resolved ? cropData.Id : 0)}, plantableSeasons={seasonsText}, seasonAllowed={seasonAllowed}");
        }

        private void SetTimeToSeason(GameSeason season)
        {
            int month = GetRepresentativeMonth(season);
            GameTimeManager.Instance.SetTime(testYear, month, testDay, testHour, testMinute);
            Debug.Log($"[FarmCropOdinTester] 设置时间到季节 season={season}, time={GameTimeManager.Instance.CurrentTime.Value}");
        }

        private ItemInteractionContext CreateContext(E_ItemType itemType, int itemId)
        {
            ItemData itemData = CreateItemData(itemType, itemId);
            MapGridCellInfo cellInfo = CreateCellInfo();
            return new ItemInteractionContext(
                null,
                itemData,
                default,
                default,
                targetCell,
                targetCell,
                itemUseRadius,
                true,
                null,
                CursorTargetType.MapCell,
                cellInfo);
        }

        private ItemData CreateItemData(E_ItemType itemType, int itemId)
        {
            return new ItemData
            {
                Id = itemId,
                name = itemType.ToString(),
                itemType = itemType,
                itemUseRadius = itemUseRadius
            };
        }

        private MapGridCellInfo CreateCellInfo()
        {
            if (useMapGridCellInfo && MapGridManager.Instance.TryGetCell(targetCell, out MapGridCellInfo info))
            {
                return info;
            }

            if (!fallbackToSyntheticCanDigCellInfo)
            {
                Debug.LogWarning($"[FarmCropOdinTester] 目标格子无法从 MapGrid 查询，且未启用合成 CanDig CellInfo。cell={targetCell}");
                return default;
            }

            MapGridCellFlags flags = MapGridCellFlags.CanDig | MapGridCellFlags.CanPlaceFurniture;
            return new MapGridCellInfo(targetCell, targetCell.x, targetCell.y, flags, flags);
        }

        private void PrintCropState(string title)
        {
            string mapId = CurrentMapId;
            bool hasCropState = FarmLandManager.Instance.TryGetCropState(mapId, targetCell, out PlantedCropState cropState);
            string cropStateText = hasCropState && cropState != null
                ? $"cropDataId={cropState.CropDataId}, stage={cropState.CurrentStageIndex}, elapsedDays={cropState.CurrentStageElapsedDays}"
                : "no crop state";

            Debug.Log($"[FarmCropOdinTester] {title}: time={GameTimeManager.Instance.CurrentTime.Value}, mapId={mapId}, cell={targetCell}, tilled={IsTilled}, watered={IsWatered}, planted={IsPlanted}, mature={IsMature}, {cropStateText}");
        }

        private static bool IsSeasonAllowed(CropData cropData, GameSeason season)
        {
            return cropData != null &&
                   (cropData.PlantableSeasons == null ||
                    cropData.PlantableSeasons.Count == 0 ||
                    cropData.PlantableSeasons.Contains(season));
        }

        private static int GetRepresentativeMonth(GameSeason season)
        {
            switch (season)
            {
                case GameSeason.Spring:
                    return 3;
                case GameSeason.Summer:
                    return 6;
                case GameSeason.Autumn:
                    return 9;
                case GameSeason.Winter:
                    return 12;
                default:
                    return 3;
            }
        }
    }
}
#endif