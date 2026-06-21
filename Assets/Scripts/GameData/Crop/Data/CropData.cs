using System;
using System.Collections.Generic;
using Gameplay.TimeSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 作物基础配置，描述一种“地里会生长的作物”。
    /// 它不是背包物品本身，而是通过 SeedItemId 与种子 ItemData 建立关联。
    /// </summary>
    [Serializable]
    public class CropData
    {
        /// <summary>
        /// 作物配置 ID，用于标识作物类型，例如萝卜作物、番茄作物。
        /// 不要求与种子物品 ID 或收获产物 ID 相同。
        /// </summary>
        [FoldoutGroup("Crop Data")]
        [BoxGroup("Crop Data/基本信息"), LabelText("作物 ID"), LabelWidth(120)]
        [Tooltip("作物配置 ID，用于标识地里生长的作物类型。不要求与种子物品 ID 或收获产物 ID 相同。")]
        public int Id;

        /// <summary>
        /// 能种出该作物的种子物品 ID，对应 ItemData.Id，且该物品通常应为 E_ItemType.Seed。
        /// 播种时会用玩家当前选中的种子物品 ID 反查 CropData。
        /// </summary>
        [FoldoutGroup("Crop Data")]
        [BoxGroup("Crop Data/基本信息"), LabelText("种子物品 ID"), LabelWidth(120)]
        [Tooltip("能种出该作物的种子物品 ID，对应 ItemData.Id，通常该物品类型应为 Seed。")]
        public int SeedItemId;

        /// <summary>
        /// 允许播种的季节。后续播种规则会用当前 GameSeason 判断是否可种。
        /// </summary>
        [FoldoutGroup("Crop Data")]
        [BoxGroup("Crop Data/生长信息"), LabelText("允许播种的季节"), LabelWidth(120)]
        [Tooltip("允许播种的季节。后续播种规则会用当前 GameSeason 判断是否可种。")]
        public List<GameSeason> PlantableSeasons = new List<GameSeason>();

        /// <summary>
        /// 有序成长阶段列表。索引 0 是播种后的初始阶段，后续按天推进到更高索引。
        /// </summary>
        [FoldoutGroup("Crop Data")]
        [BoxGroup("Crop Data/生长信息"), LabelText("成长阶段列表"), LabelWidth(120)]
        [Tooltip("有序成长阶段列表。索引 0 是播种后的初始阶段，后续按天推进到更高索引。")]
        public List<CropGrowthStageData> GrowthStages = new List<CropGrowthStageData>();

        /// <summary>
        /// 是否支持重复收获。为 false 时，收获后移除地里的作物状态。
        /// </summary>
        [FoldoutGroup("Crop Data")]
        [BoxGroup("Crop Data/收获信息"), LabelText("支持重复收获"), LabelWidth(120)]
        [Tooltip("是否支持重复收获。关闭时，收获后会移除地里的作物状态。")]
        public bool CanRegrow;

        /// <summary>
        /// 重复收获后回退到的成长阶段索引。CanRegrow 为 false 时保持 -1。
        /// </summary>
        [FoldoutGroup("Crop Data")]
        [BoxGroup("Crop Data/收获信息"), LabelText("重复收获回退阶段索引"), LabelWidth(120)]
        [Tooltip("重复收获后回退到的成长阶段索引。CanRegrow 关闭时保持 -1。")]
        public int RegrowStageIndex = -1;
    }
}
