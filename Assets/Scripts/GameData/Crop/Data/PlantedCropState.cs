using System;
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 已种植作物的运行时状态。
    /// CropData 回答“这是什么作物”，该状态回答“这块地上的作物长到哪里了”。
    /// </summary>
    [Serializable]
    public class PlantedCropState
    {
        /// <summary>
        /// 当前地块种植的作物配置 ID，对应 CropData.Id。
        /// </summary>
        [Tooltip("当前地块种植的作物配置 ID，对应 CropData.Id。")]
        public int CropDataId;

        /// <summary>
        /// 当前成长阶段索引，对应 CropData.GrowthStages。
        /// </summary>
        [Tooltip("当前成长阶段索引，对应 CropData.GrowthStages。")]
        public int CurrentStageIndex;

        /// <summary>
        /// 当前阶段已经累计成长的天数。
        /// </summary>
        [Tooltip("当前阶段已经累计成长的天数。")]
        public int CurrentStageElapsedDays;
    }
}