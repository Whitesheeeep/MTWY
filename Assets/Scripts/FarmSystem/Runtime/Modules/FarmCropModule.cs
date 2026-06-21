using System;
using System.Collections.Generic;
using GameData;
using UnityEngine;

namespace FarmSystem
{
    /// <summary>
    /// 维护农田格子的作物运行时状态。
    /// 该模块只负责记录、推进和移除状态，不依赖时间系统、数据库、输入或表现层。
    /// </summary>
    public sealed class FarmCropModule
    {
        private readonly Dictionary<string, Dictionary<Vector3Int, PlantedCropState>> cropsByMapId =
            new Dictionary<string, Dictionary<Vector3Int, PlantedCropState>>();

        public bool IsPlanted(string mapId, Vector3Int cell)
        {
            return cropsByMapId.TryGetValue(mapId, out Dictionary<Vector3Int, PlantedCropState> cells) &&
                   cells.ContainsKey(cell);
        }

        public bool TryGetCropState(string mapId, Vector3Int cell, out PlantedCropState state)
        {
            if (cropsByMapId.TryGetValue(mapId, out Dictionary<Vector3Int, PlantedCropState> cells) &&
                cells.TryGetValue(cell, out PlantedCropState storedState))
            {
                state = CloneState(storedState);
                return true;
            }

            state = null;
            return false;
        }

        public IReadOnlyList<FarmCropCellSnapshot> GetPlantedCrops()
        {
            var result = new List<FarmCropCellSnapshot>();
            foreach (KeyValuePair<string, Dictionary<Vector3Int, PlantedCropState>> mapPair in cropsByMapId)
            {
                foreach (KeyValuePair<Vector3Int, PlantedCropState> cellPair in mapPair.Value)
                {
                    result.Add(new FarmCropCellSnapshot(mapPair.Key, cellPair.Key, CloneState(cellPair.Value)));
                }
            }

            return result;
        }

        public IReadOnlyList<FarmCropCellSnapshot> GetPlantedCrops(string mapId)
        {
            var result = new List<FarmCropCellSnapshot>();
            if (string.IsNullOrWhiteSpace(mapId) ||
                !cropsByMapId.TryGetValue(mapId, out Dictionary<Vector3Int, PlantedCropState> cells))
            {
                return result;
            }

            foreach (KeyValuePair<Vector3Int, PlantedCropState> cellPair in cells)
            {
                result.Add(new FarmCropCellSnapshot(mapId, cellPair.Key, CloneState(cellPair.Value)));
            }

            return result;
        }

        public bool TryPlant(string mapId, Vector3Int cell, CropData cropData, out PlantedCropState state)
        {
            state = null;
            if (cropData == null || cropData.Id <= 0)
            {
                return false;
            }

            Dictionary<Vector3Int, PlantedCropState> cells = GetOrCreatePlantMap(mapId);
            if (cells.ContainsKey(cell))
            {
                return false;
            }

            state = new PlantedCropState
            {
                CropDataId = cropData.Id,
                CurrentStageIndex = 0,
                CurrentStageElapsedDays = 0
            };

            cells.Add(cell, CloneState(state));
            return true;
        }

        public bool IsMature(CropData cropData, PlantedCropState state)
        {
            return TryGetCurrentStage(cropData, state, out CropGrowthStageData stage) &&
                   stage.HarvestItemId > 0;
        }

        public bool IsMature(string mapId, Vector3Int cell, CropData cropData)
        {
            return cropsByMapId.TryGetValue(mapId, out Dictionary<Vector3Int, PlantedCropState> cells) &&
                   cells.TryGetValue(cell, out PlantedCropState state) &&
                   IsMature(cropData, state);
        }

        public bool TryAdvanceGrowth(
            string mapId,
            Vector3Int cell,
            CropData cropData,
            out FarmCropGrowthResult result)
        {
            result = default;
            if (!cropsByMapId.TryGetValue(mapId, out Dictionary<Vector3Int, PlantedCropState> cells) ||
                !cells.TryGetValue(cell, out PlantedCropState state) ||
                cropData == null ||
                cropData.Id != state.CropDataId ||
                !TryGetCurrentStage(cropData, state, out CropGrowthStageData currentStage))
            {
                return false;
            }

            if (IsMature(cropData, state) || currentStage.DurationDays <= 0)
            {
                return false;
            }

            PlantedCropState previousState = CloneState(state);
            int previousStageIndex = state.CurrentStageIndex;

            state.CurrentStageElapsedDays++;
            bool stageChanged = false;
            if (state.CurrentStageElapsedDays >= currentStage.DurationDays &&
                state.CurrentStageIndex + 1 < cropData.GrowthStages.Count)
            {
                state.CurrentStageIndex++;
                state.CurrentStageElapsedDays = 0;
                stageChanged = true;
            }

            PlantedCropState currentState = CloneState(state);
            bool becameMature = !IsMature(cropData, previousState) && IsMature(cropData, currentState);
            result = new FarmCropGrowthResult(
                mapId,
                cell,
                cropData.Id,
                previousState,
                currentState,
                previousStageIndex,
                currentState.CurrentStageIndex,
                stageChanged,
                becameMature);
            return true;
        }

        public bool TryHarvest(
            string mapId,
            Vector3Int cell,
            CropData cropData,
            int harvestCount,
            out FarmCropHarvestResult result)
        {
            result = default;
            if (!cropsByMapId.TryGetValue(mapId, out Dictionary<Vector3Int, PlantedCropState> cells) ||
                !cells.TryGetValue(cell, out PlantedCropState state) ||
                cropData == null ||
                cropData.Id != state.CropDataId ||
                !TryGetCurrentStage(cropData, state, out CropGrowthStageData harvestStage) ||
                harvestStage.HarvestItemId <= 0)
            {
                return false;
            }

            PlantedCropState harvestedState = CloneState(state);
            bool regrew = false;
            PlantedCropState currentState = null;

            if (CanRegrowFromHarvest(cropData))
            {
                state.CurrentStageIndex = cropData.RegrowStageIndex;
                state.CurrentStageElapsedDays = 0;
                currentState = CloneState(state);
                regrew = true;
            }
            else
            {
                cells.Remove(cell);
                if (cells.Count == 0)
                {
                    cropsByMapId.Remove(mapId);
                }
            }

            result = new FarmCropHarvestResult(
                mapId,
                cell,
                cropData.Id,
                harvestedState,
                currentState,
                harvestStage.HarvestItemId,
                harvestStage.HarvestMinCount,
                harvestStage.HarvestMaxCount,
                harvestCount,
                regrew);
            return true;
        }

        private static bool TryGetCurrentStage(CropData cropData, PlantedCropState state, out CropGrowthStageData stage)
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
            return stage != null;
        }

        private static bool CanRegrowFromHarvest(CropData cropData)
        {
            return cropData.CanRegrow &&
                   cropData.GrowthStages != null &&
                   cropData.RegrowStageIndex >= 0 &&
                   cropData.RegrowStageIndex < cropData.GrowthStages.Count;
        }

        private static PlantedCropState CloneState(PlantedCropState state)
        {
            if (state == null)
            {
                return null;
            }

            return new PlantedCropState
            {
                CropDataId = state.CropDataId,
                CurrentStageIndex = state.CurrentStageIndex,
                CurrentStageElapsedDays = state.CurrentStageElapsedDays
            };
        }

        private Dictionary<Vector3Int, PlantedCropState> GetOrCreatePlantMap(string mapId)
        {
            if (!cropsByMapId.TryGetValue(mapId, out Dictionary<Vector3Int, PlantedCropState> cells))
            {
                cells = new Dictionary<Vector3Int, PlantedCropState>();
                cropsByMapId.Add(mapId, cells);
            }

            return cells;
        }
    }

    public readonly struct FarmCropCellSnapshot
    {
        public FarmCropCellSnapshot(string mapId, Vector3Int cell, PlantedCropState state)
        {
            MapId = mapId;
            Cell = cell;
            State = state;
        }

        public string MapId { get; }
        public Vector3Int Cell { get; }
        public PlantedCropState State { get; }
    }

    public readonly struct FarmCropGrowthResult
    {
        public FarmCropGrowthResult(
            string mapId,
            Vector3Int cell,
            int cropDataId,
            PlantedCropState previousState,
            PlantedCropState currentState,
            int previousStageIndex,
            int currentStageIndex,
            bool stageChanged,
            bool becameMature)
        {
            MapId = mapId;
            Cell = cell;
            CropDataId = cropDataId;
            PreviousState = previousState;
            CurrentState = currentState;
            PreviousStageIndex = previousStageIndex;
            CurrentStageIndex = currentStageIndex;
            StageChanged = stageChanged;
            BecameMature = becameMature;
        }

        public string MapId { get; }
        public Vector3Int Cell { get; }
        public int CropDataId { get; }
        public PlantedCropState PreviousState { get; }
        public PlantedCropState CurrentState { get; }
        public int PreviousStageIndex { get; }
        public int CurrentStageIndex { get; }
        public bool StageChanged { get; }
        public bool BecameMature { get; }
    }

    public readonly struct FarmCropHarvestResult
    {
        public FarmCropHarvestResult(
            string mapId,
            Vector3Int cell,
            int cropDataId,
            PlantedCropState harvestedState,
            PlantedCropState currentState,
            int harvestItemId,
            int harvestMinCount,
            int harvestMaxCount,
            int harvestCount,
            bool regrew)
        {
            MapId = mapId;
            Cell = cell;
            CropDataId = cropDataId;
            HarvestedState = harvestedState;
            CurrentState = currentState;
            HarvestItemId = harvestItemId;
            HarvestMinCount = harvestMinCount;
            HarvestMaxCount = harvestMaxCount;
            HarvestCount = harvestCount;
            Regrew = regrew;
        }

        public string MapId { get; }
        public Vector3Int Cell { get; }
        public int CropDataId { get; }
        public PlantedCropState HarvestedState { get; }
        public PlantedCropState CurrentState { get; }
        public int HarvestItemId { get; }
        public int HarvestMinCount { get; }
        public int HarvestMaxCount { get; }
        public int HarvestCount { get; }
        public bool Regrew { get; }
    }
}