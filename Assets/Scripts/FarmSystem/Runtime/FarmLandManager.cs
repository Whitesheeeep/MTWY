using System;
using System.Collections.Generic;
using CursorSystem;
using GameData;
using Gameplay.TimeSystem;
using UnityEngine;
using WS_Modules.CustomEventSystem;
using WS_Modules.Singleton;
using WS_Modules.Utilities;

namespace FarmSystem
{
    /// <summary>
    /// 农田系统门面，负责协调规则、土地、湿润、作物模块，并把农田占用投影到 MapGrid。
    /// </summary>
    public sealed class FarmLandManager : SingletonBase<FarmLandManager>
    {
        // FarmLandManager 只维护农田业务状态和 MapGrid 占用投影。
        // 表现层应从这里读取状态或订阅事件，不要让业务层直接依赖 Tile、Prefab、特效或动画组件。
        private readonly FarmSoilModule soilModule = new FarmSoilModule();
        private readonly FarmWaterModule waterModule = new FarmWaterModule();
        private readonly FarmCropModule cropModule = new FarmCropModule();

        // 规则层：FarmLandManager 负责创建规则上下文并调用规则管线，但不直接实现规则逻辑。
        private readonly FarmSoilRulePipelineModule soilRulePipelineModule = new FarmSoilRulePipelineModule();
        private readonly FarmWaterRulePipelineModule waterRulePipelineModule = new FarmWaterRulePipelineModule();
        private readonly FarmCropRulePipelineModule cropRulePipelineModule = new FarmCropRulePipelineModule();
        private readonly Dictionary<string, TimeWheelHandle> waterDrainHandlesBySourceId =
            new Dictionary<string, TimeWheelHandle>();

        private IUnRegister dayStartedUnRegister;

        private FarmLandManager()
        {
            RegisterTimeEvents();
        }

        public event Action<FarmCellStateChangedEventArgs> CellStateChanged;
        public event Action<FarmWaterDrainedEventArgs> WaterDrained;
        public event Action<FarmCropPlantedEventArgs> CropPlanted;
        public event Action<FarmCropGrowthAdvancedEventArgs> CropGrowthAdvanced;
        public event Action<FarmCropStageChangedEventArgs> CropStageChanged;
        public event Action<FarmCropMaturedEventArgs> CropMatured;
        public event Action<FarmCropHarvestedEventArgs> CropHarvested;

        public bool CanTill(ItemInteractionContext context)
        {
            return TryCreateRuleContext(context, out FarmRuleContext ruleContext, out _) &&
                   soilRulePipelineModule.CanTill(ruleContext, out _);
        }

        public bool TryTill(ItemInteractionContext context)
        {
            if (!TryCreateRuleContext(context, out FarmRuleContext ruleContext, out string reason) ||
                !soilRulePipelineModule.CanTill(ruleContext, out reason))
            {
                Debug.Log($"[FarmLandManager] 耕地失败: {reason}");
                return false;
            }

            if (!soilModule.TryTill(ruleContext.MapId, ruleContext.TargetCell))
            {
                Debug.Log("[FarmLandManager] 耕地失败: 土地模块拒绝写入耕地状态");
                return false;
            }

            ApplyFarmProjection(ruleContext.MapId, ruleContext.TargetCell);
            Debug.Log($"[FarmLandManager] 耕地成功 mapId={ruleContext.MapId}, cell={ruleContext.TargetCell}");
            RaiseCellStateChanged(ruleContext.MapId, ruleContext.TargetCell);
            return true;
        }

        public bool CanWater(ItemInteractionContext context)
        {
            return TryCreateRuleContext(context, out FarmRuleContext ruleContext, out _) &&
                   waterRulePipelineModule.CanWater(ruleContext, out _);
        }

        public bool TryWater(ItemInteractionContext context)
        {
            if (!TryCreateRuleContext(context, out FarmRuleContext ruleContext, out string reason) ||
                !waterRulePipelineModule.CanWater(ruleContext, out reason))
            {
                Debug.Log($"[FarmLandManager] 浇水失败: {reason}");
                return false;
            }

            int retentionMinutes = FarmSetting.GetWaterRetentionMinutes();
            long expireTotalMinutes = GameTimeManager.Instance.CurrentTime.Value.TotalMinutes + retentionMinutes;
            waterModule.SetWatered(ruleContext.MapId, ruleContext.TargetCell, expireTotalMinutes);
            ScheduleWaterDrain(ruleContext.MapId, ruleContext.TargetCell, retentionMinutes);

            Debug.Log($"[FarmLandManager] 浇水成功 mapId={ruleContext.MapId}, cell={ruleContext.TargetCell}, expireTotalMinutes={expireTotalMinutes}");
            RaiseCellStateChanged(ruleContext.MapId, ruleContext.TargetCell);
            return true;
        }

        public bool CanPlant(ItemInteractionContext context)
        {
            return CanPlant(context, out _);
        }

        public bool CanPlant(ItemInteractionContext context, out string reason)
        {
            if (!TryCreateRuleContext(context, out FarmRuleContext ruleContext, out reason))
            {
                return false;
            }

            return cropRulePipelineModule.CanPlant(ruleContext, out reason);
        }

        public bool TryPlant(ItemInteractionContext context)
        {
            if (!TryCreateRuleContext(context, out FarmRuleContext ruleContext, out string reason) ||
                !cropRulePipelineModule.CanPlant(ruleContext, out reason))
            {
                Debug.Log($"[FarmLandManager] 播种失败: {reason}");
                return false;
            }

            if (!TryResolvePlantingCropData(ruleContext.SelectedItemData, out CropData cropData))
            {
                Debug.Log("[FarmLandManager] 播种失败: 找不到种子对应的作物配置");
                return false;
            }

            if (!cropModule.TryPlant(ruleContext.MapId, ruleContext.TargetCell, cropData, out PlantedCropState plantedState))
            {
                Debug.Log("[FarmLandManager] 播种失败: 作物模块拒绝写入种植状态");
                return false;
            }

            ApplyFarmProjection(ruleContext.MapId, ruleContext.TargetCell);
            Debug.Log($"[FarmLandManager] 播种成功 seedItemId={ruleContext.SelectedItemData.Id}, cropDataId={cropData.Id}, mapId={ruleContext.MapId}, cell={ruleContext.TargetCell}");
            CropPlanted?.Invoke(new FarmCropPlantedEventArgs(ruleContext.MapId, ruleContext.TargetCell, cropData, plantedState));
            RaisePlantSeedConsumeRequested(ruleContext.MapId, ruleContext.TargetCell, cropData.Id, ruleContext.SelectedItemData.Id);
            RaiseCellStateChanged(ruleContext.MapId, ruleContext.TargetCell);
            return true;
        }

        public bool CanHarvest(ItemInteractionContext context)
        {
            return TryCreateRuleContext(context, out FarmRuleContext ruleContext, out _) &&
                   cropRulePipelineModule.CanHarvest(ruleContext, out _);
        }

        public bool TryHarvest(ItemInteractionContext context)
        {
            if (!TryCreateRuleContext(context, out FarmRuleContext ruleContext, out string reason) ||
                !cropRulePipelineModule.CanHarvest(ruleContext, out reason))
            {
                Debug.Log($"[FarmLandManager] 收获失败: {reason}");
                return false;
            }

            if (!TryGetCropData(ruleContext.MapId, ruleContext.TargetCell, out CropData cropData))
            {
                Debug.Log("[FarmLandManager] 收获失败: 找不到地块对应的作物配置");
                return false;
            }

            if (!TryGetCropState(ruleContext.MapId, ruleContext.TargetCell, out PlantedCropState harvestState) ||
                !TryGetHarvestStage(cropData, harvestState, out CropGrowthStageData harvestStage))
            {
                Debug.Log("[FarmLandManager] 收获失败: 找不到有效的成熟收获阶段");
                return false;
            }

            int harvestCount = CalculateHarvestCount(harvestStage);
            if (!cropModule.TryHarvest(ruleContext.MapId, ruleContext.TargetCell, cropData, harvestCount, out FarmCropHarvestResult harvestResult))
            {
                Debug.Log("[FarmLandManager] 收获失败: 作物模块拒绝收获，作物可能尚未成熟");
                return false;
            }

            if (!IsTilled(ruleContext.MapId, ruleContext.TargetCell) && !IsPlanted(ruleContext.MapId, ruleContext.TargetCell))
            {
                ClearFarmProjection(ruleContext.MapId, ruleContext.TargetCell);
            }

            Debug.Log($"[FarmLandManager] 收获成功 cropDataId={harvestResult.CropDataId}, harvestItemId={harvestResult.HarvestItemId}, count={harvestResult.HarvestCount}, countRange={harvestResult.HarvestMinCount}-{harvestResult.HarvestMaxCount}, regrew={harvestResult.Regrew}, mapId={ruleContext.MapId}, cell={ruleContext.TargetCell}");
            CropHarvested?.Invoke(new FarmCropHarvestedEventArgs(cropData, harvestResult));
            RaiseHarvestRewardRequested(harvestResult);
            RaiseCellStateChanged(ruleContext.MapId, ruleContext.TargetCell);
            return true;
        }

        public bool IsTilled(string mapId, Vector3Int cell)
        {
            return soilModule.IsTilled(mapId, cell);
        }

        public bool IsWatered(string mapId, Vector3Int cell)
        {
            return waterModule.IsWatered(mapId, cell);
        }

        public bool TryGetWaterState(string mapId, Vector3Int cell, out FarmWaterState state)
        {
            return waterModule.TryGetWaterState(mapId, cell, out state);
        }

        public bool IsPlanted(string mapId, Vector3Int cell)
        {
            return cropModule.IsPlanted(mapId, cell);
        }

        public bool TryGetCropState(string mapId, Vector3Int cell, out PlantedCropState state)
        {
            return cropModule.TryGetCropState(mapId, cell, out state);
        }

        public bool IsCropMature(string mapId, Vector3Int cell)
        {
            return TryGetCropData(mapId, cell, out CropData cropData) &&
                   cropModule.IsMature(mapId, cell, cropData);
        }

        public bool TryResolvePlantingCropData(ItemData seedItemData, out CropData cropData)
        {
            cropData = null;
            if (seedItemData == null || seedItemData.Id <= 0)
            {
                return false;
            }

            return GameDatabase.TryGet(out ICropDatabase cropDatabase) &&
                   cropDatabase.TryGetBySeedItemId(seedItemData.Id, out cropData);
        }

        public FarmCellState GetCellState(string mapId, Vector3Int cell)
        {
            return new FarmCellState(
                mapId,
                cell,
                IsTilled(mapId, cell),
                IsWatered(mapId, cell),
                IsPlanted(mapId, cell));
        }

        public IEnumerable<Vector3Int> GetTilledCells(string mapId)
        {
            return soilModule.GetTilledCells(mapId);
        }

        public IEnumerable<Vector3Int> GetWateredCells(string mapId)
        {
            return waterModule.GetWateredCells(mapId);
        }

        public IReadOnlyList<FarmCropCellSnapshot> GetPlantedCrops(string mapId)
        {
            return cropModule.GetPlantedCrops(mapId);
        }

        private void RegisterTimeEvents()
        {
            if (dayStartedUnRegister != null || GameTimeManager.Instance == null)
            {
                return;
            }

            dayStartedUnRegister = GameTimeManager.Instance.RegisterDayStarted(OnDayStarted);
        }

        private void OnDayStarted(GameTimeChangedEventArgs args)
        {
            IReadOnlyList<FarmCropCellSnapshot> plantedCrops = cropModule.GetPlantedCrops();
            for (int i = 0; i < plantedCrops.Count; i++)
            {
                FarmCropCellSnapshot plantedCrop = plantedCrops[i];
                if (plantedCrop.State == null || !IsWatered(plantedCrop.MapId, plantedCrop.Cell))
                {
                    continue;
                }

                if (!TryGetCropData(plantedCrop.State.CropDataId, out CropData cropData))
                {
                    Debug.LogWarning($"[FarmLandManager] 作物成长跳过: 找不到作物配置 cropDataId={plantedCrop.State.CropDataId}, mapId={plantedCrop.MapId}, cell={plantedCrop.Cell}");
                    continue;
                }

                if (!cropModule.TryAdvanceGrowth(plantedCrop.MapId, plantedCrop.Cell, cropData, out FarmCropGrowthResult growthResult))
                {
                    continue;
                }

                Debug.Log($"[FarmLandManager] 作物成长推进 cropDataId={growthResult.CropDataId}, mapId={growthResult.MapId}, cell={growthResult.Cell}, stage={growthResult.CurrentStageIndex}, elapsedDays={growthResult.CurrentState.CurrentStageElapsedDays}");
                CropGrowthAdvanced?.Invoke(new FarmCropGrowthAdvancedEventArgs(cropData, growthResult));

                if (growthResult.StageChanged)
                {
                    Debug.Log($"[FarmLandManager] 作物阶段变化 cropDataId={growthResult.CropDataId}, mapId={growthResult.MapId}, cell={growthResult.Cell}, {growthResult.PreviousStageIndex}->{growthResult.CurrentStageIndex}");
                    CropStageChanged?.Invoke(new FarmCropStageChangedEventArgs(cropData, growthResult));
                }

                if (growthResult.BecameMature)
                {
                    Debug.Log($"[FarmLandManager] 作物成熟 cropDataId={growthResult.CropDataId}, mapId={growthResult.MapId}, cell={growthResult.Cell}");
                    CropMatured?.Invoke(new FarmCropMaturedEventArgs(cropData, growthResult));
                }
            }
        }

        // 通过全局事件总线请求跨系统消耗本次播种使用的种子。
        private static void RaisePlantSeedConsumeRequested(string mapId, Vector3Int cell, int cropDataId, int seedItemId)
        {
            if (seedItemId <= 0)
            {
                return;
            }

            EventSystem.EventTrigger_Int(
                (int)E_FarmEvent.PlantSeedConsumeRequested,
                new FarmPlantSeedConsumeRequestedEventArgs(
                    mapId,
                    cell,
                    cropDataId,
                    seedItemId,
                    1));
        }
        // 读取当前作物阶段配置，用于收获前计算实际奖励数量。
        private static bool TryGetHarvestStage(CropData cropData, PlantedCropState state, out CropGrowthStageData stage)
        {
            stage = null;
            if (cropData?.GrowthStages == null ||
                state == null ||
                state.CurrentStageIndex < 0 ||
                state.CurrentStageIndex >= cropData.GrowthStages.Count)
            {
                return false;
            }

            stage = cropData.GrowthStages[state.CurrentStageIndex];
            return stage != null && stage.HarvestItemId > 0;
        }

        // 根据阶段配置生成本次实际收获数量。
        private static int CalculateHarvestCount(CropGrowthStageData harvestStage)
        {
            int minCount = Mathf.Max(1, harvestStage.HarvestMinCount);
            int maxCount = harvestStage.HarvestMaxCount < minCount ? minCount : harvestStage.HarvestMaxCount;
            return UnityEngine.Random.Range(minCount, maxCount + 1);
        }

        // 通过全局事件总线请求跨系统发放收获奖励。
        private static void RaiseHarvestRewardRequested(FarmCropHarvestResult harvestResult)
        {
            if (harvestResult.HarvestItemId <= 0 || harvestResult.HarvestCount <= 0)
            {
                return;
            }

            EventSystem.EventTrigger_Int(
                (int)E_FarmEvent.HarvestRewardRequested,
                new FarmHarvestRewardRequestedEventArgs(
                    harvestResult.MapId,
                    harvestResult.Cell,
                    harvestResult.CropDataId,
                    harvestResult.HarvestItemId,
                    harvestResult.HarvestCount));
        }
        private bool TryCreateRuleContext(
            ItemInteractionContext context,
            out FarmRuleContext ruleContext,
            out string reason)
        {
            if (!TryGetCurrentMapId(out string mapId))
            {
                ruleContext = default;
                reason = "当前没有有效地图";
                return false;
            }

            ruleContext = new FarmRuleContext(context, mapId, this);
            reason = string.Empty;
            return true;
        }

        private static bool TryGetCurrentMapId(out string mapId)
        {
            mapId = MapGridManager.Instance.CurrentMapId;
            return !string.IsNullOrWhiteSpace(mapId);
        }

        private bool TryGetCropData(string mapId, Vector3Int cell, out CropData cropData)
        {
            cropData = null;
            return cropModule.TryGetCropState(mapId, cell, out PlantedCropState state) &&
                   state != null &&
                   TryGetCropData(state.CropDataId, out cropData);
        }

        private static bool TryGetCropData(int cropDataId, out CropData cropData)
        {
            cropData = null;
            return GameDatabase.TryGet(out ICropDatabase cropDatabase) &&
                   cropDatabase.TryGet(cropDataId, out cropData);
        }

        private void ScheduleWaterDrain(string mapId, Vector3Int cell, int delayMinutes)
        {
            string sourceId = BuildSourceId(mapId, cell);
            CancelWaterDrain(sourceId);

            TimeWheelHandle handle = GameTimeManager.Instance.ScheduleAfterMinutes(
                delayMinutes,
                () => DrainWaterNaturally(mapId, cell));
            waterDrainHandlesBySourceId[sourceId] = handle;
        }

        private void CancelWaterDrain(string sourceId)
        {
            if (!waterDrainHandlesBySourceId.TryGetValue(sourceId, out TimeWheelHandle handle))
            {
                return;
            }

            if (handle.IsValid)
            {
                GameTimeManager.Instance.CancelScheduledTask(handle);
            }

            waterDrainHandlesBySourceId.Remove(sourceId);
        }

        private void DrainWaterNaturally(string mapId, Vector3Int cell)
        {
            string sourceId = BuildSourceId(mapId, cell);
            waterDrainHandlesBySourceId.Remove(sourceId);

            FarmCellState previousCellState = GetCellState(mapId, cell);
            if (!waterModule.TryDrain(mapId, cell, out FarmWaterState previousWaterState))
            {
                return;
            }

            FarmCellState currentCellState = GetCellState(mapId, cell);
            Debug.Log($"[FarmLandManager] 水分自然消退 mapId={mapId}, cell={cell}");

            WaterDrained?.Invoke(new FarmWaterDrainedEventArgs(
                mapId,
                cell,
                previousWaterState,
                previousCellState,
                currentCellState));
            RaiseCellStateChanged(currentCellState);
        }

        private void RaiseCellStateChanged(string mapId, Vector3Int cell)
        {
            RaiseCellStateChanged(GetCellState(mapId, cell));
        }

        private void RaiseCellStateChanged(FarmCellState state)
        {
            CellStateChanged?.Invoke(new FarmCellStateChangedEventArgs(state));
        }

        private static void ApplyFarmProjection(string mapId, Vector3Int cell)
        {
            // MapGrid 投影只表达农田对格子的占用关系，例如禁止家具放置；它不是视觉状态，也不负责替换 Tile。
            var record = new MapGridRuntimeOverrideRecord
            {
                mapId = mapId,
                sourceId = BuildSourceId(mapId, cell),
                occupiedCells = new List<Vector3Int> { cell },
                addFlags = MapGridCellFlags.None,
                removeFlags = MapGridCellFlags.CanPlaceFurniture
            };

            MapGridManager.Instance.TryApplyOverride(record);
        }

        private static void ClearFarmProjection(string mapId, Vector3Int cell)
        {
            // 仅当该格不再被农田业务占用时清理投影；视觉清理由表现层根据业务状态变化处理。
            MapGridManager.Instance.ClearOverrides(mapId, BuildSourceId(mapId, cell));
        }

        private static string BuildSourceId(string mapId, Vector3Int cell)
        {
            return $"Farm:{mapId}:{cell.x}:{cell.y}:{cell.z}";
        }
    }
}