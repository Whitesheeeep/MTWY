using System;
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 作物单个成长阶段配置。成长逻辑读取 DurationDays，表现层后续可根据 StageSprite 显示对应阶段。
    /// </summary>
    [Serializable]
    public class CropGrowthStageData
    {
        /// <summary>
        /// 当前阶段需要累计成长的天数。小于等于 0 表示该阶段不会继续自动推进。
        /// </summary>
        [Tooltip("当前阶段需要累计成长的天数。小于等于 0 表示该阶段不会继续自动推进。")]
        public int DurationDays;

        /// <summary>
        /// 当前阶段对应的 Sprite，只作为表现层数据，不由数据层直接创建视觉对象。
        /// </summary>
        [Tooltip("当前阶段对应的 Sprite，只作为表现层数据，不由数据层直接创建视觉对象。")]
        public Sprite StageSprite;

        /// <summary>
        /// 当前阶段收获产物的 ItemData.Id。小于等于 0 表示该阶段不可收获。
        /// </summary>
        [Tooltip("当前阶段收获产物的 ItemData.Id。小于等于 0 表示该阶段不可收获。")]
        public int HarvestItemId = -1;

        /// <summary>
        /// 当前阶段最小收获数量。只有 HarvestItemId 大于 0 时才有意义。
        /// </summary>
        [Tooltip("当前阶段最小收获数量。只有 HarvestItemId 大于 0 时才有意义。")]
        public int HarvestMinCount;

        /// <summary>
        /// 当前阶段最大收获数量。只有 HarvestItemId 大于 0 时才有意义。
        /// </summary>
        [Tooltip("当前阶段最大收获数量。只有 HarvestItemId 大于 0 时才有意义。")]
        public int HarvestMaxCount;
    }
}
