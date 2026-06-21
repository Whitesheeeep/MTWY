using GameData;
using UnityEngine;

namespace FarmSystem
{
    /// <summary>
    /// 作物播种事件参数。后续表现层可据此创建初始作物表现。
    /// </summary>
    public readonly struct FarmCropPlantedEventArgs
    {
        public FarmCropPlantedEventArgs(string mapId, Vector3Int cell, CropData cropData, PlantedCropState currentState)
        {
            MapId = mapId;
            Cell = cell;
            CropData = cropData;
            CropDataId = cropData?.Id ?? 0;
            CurrentState = currentState;
        }

        public string MapId { get; }
        public Vector3Int Cell { get; }
        public CropData CropData { get; }
        public int CropDataId { get; }
        public PlantedCropState CurrentState { get; }
    }

    /// <summary>
    /// 作物每日成长事件参数。只要当天成功累计了成长天数，就会派发。
    /// </summary>
    public readonly struct FarmCropGrowthAdvancedEventArgs
    {
        public FarmCropGrowthAdvancedEventArgs(CropData cropData, FarmCropGrowthResult result)
        {
            MapId = result.MapId;
            Cell = result.Cell;
            CropData = cropData;
            CropDataId = result.CropDataId;
            PreviousState = result.PreviousState;
            CurrentState = result.CurrentState;
            PreviousStageIndex = result.PreviousStageIndex;
            CurrentStageIndex = result.CurrentStageIndex;
            StageChanged = result.StageChanged;
        }

        public string MapId { get; }
        public Vector3Int Cell { get; }
        public CropData CropData { get; }
        public int CropDataId { get; }
        public PlantedCropState PreviousState { get; }
        public PlantedCropState CurrentState { get; }
        public int PreviousStageIndex { get; }
        public int CurrentStageIndex { get; }
        public bool StageChanged { get; }
    }

    /// <summary>
    /// 作物成长阶段变化事件参数。后续作物表现层主要监听这个事件刷新 Sprite。
    /// </summary>
    public readonly struct FarmCropStageChangedEventArgs
    {
        public FarmCropStageChangedEventArgs(CropData cropData, FarmCropGrowthResult result)
        {
            MapId = result.MapId;
            Cell = result.Cell;
            CropData = cropData;
            CropDataId = result.CropDataId;
            PreviousState = result.PreviousState;
            CurrentState = result.CurrentState;
            PreviousStageIndex = result.PreviousStageIndex;
            CurrentStageIndex = result.CurrentStageIndex;
        }

        public string MapId { get; }
        public Vector3Int Cell { get; }
        public CropData CropData { get; }
        public int CropDataId { get; }
        public PlantedCropState PreviousState { get; }
        public PlantedCropState CurrentState { get; }
        public int PreviousStageIndex { get; }
        public int CurrentStageIndex { get; }
    }

    /// <summary>
    /// 作物成熟事件参数。成熟定义为当前阶段存在有效 HarvestItemId。
    /// </summary>
    public readonly struct FarmCropMaturedEventArgs
    {
        public FarmCropMaturedEventArgs(CropData cropData, FarmCropGrowthResult result)
        {
            MapId = result.MapId;
            Cell = result.Cell;
            CropData = cropData;
            CropDataId = result.CropDataId;
            CurrentState = result.CurrentState;
        }

        public string MapId { get; }
        public Vector3Int Cell { get; }
        public CropData CropData { get; }
        public int CropDataId { get; }
        public PlantedCropState CurrentState { get; }
    }

    /// <summary>
    /// 作物收获事件参数。第一版只提供收获结果数据，不直接发放到库存。
    /// </summary>
    public readonly struct FarmCropHarvestedEventArgs
    {
        public FarmCropHarvestedEventArgs(CropData cropData, FarmCropHarvestResult result)
        {
            MapId = result.MapId;
            Cell = result.Cell;
            CropData = cropData;
            CropDataId = result.CropDataId;
            HarvestedState = result.HarvestedState;
            CurrentState = result.CurrentState;
            HarvestItemId = result.HarvestItemId;
            HarvestMinCount = result.HarvestMinCount;
            HarvestMaxCount = result.HarvestMaxCount;
            HarvestCount = result.HarvestCount;
            Regrew = result.Regrew;
        }

        public string MapId { get; }
        public Vector3Int Cell { get; }
        public CropData CropData { get; }
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