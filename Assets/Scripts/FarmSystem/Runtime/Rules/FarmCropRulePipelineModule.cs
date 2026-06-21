using GameData;

namespace FarmSystem
{
    /// <summary>
    /// 作物动作规则管线模块，负责播种和收获类动作的前置条件校验。
    /// </summary>
    public sealed class FarmCropRulePipelineModule
    {
        // 作物规则组：播种和收获共用作物状态，但分别维护独立的前置条件链。
        private readonly FarmRulePipelineModule plantRulePipeline =
            new FarmRulePipelineModule(
                new IFarmActionRule[]
                {
                    new MapCellContextRule(),
                    new SelectedItemTypeRule(E_ItemType.Seed),
                    new PlantableSeasonRule(),
                    new TilledRule(),
                    new NotPlantedRule()
                });

        private readonly FarmRulePipelineModule harvestRulePipeline =
            new FarmRulePipelineModule(
                new IFarmActionRule[]
                {
                    new ValidTargetCellRule(),
                    new SelectedItemAnyTypeRule(E_ItemType.CollectTool, E_ItemType.ReapTool),
                    new PlantedRule(),
                    new MatureCropRule()
                });

        private readonly FarmRulePipelineModule removeCropRulePipeline =
            new FarmRulePipelineModule(
                new IFarmActionRule[]
                {
                    new MapCellContextRule(),
                    new SelectedItemTypeRule(E_ItemType.HoeTool),
                    new PlantedRule()
                });
        public bool CanPlant(FarmRuleContext context, out string reason)
        {
            return plantRulePipeline.CanExecute(context, out reason);
        }

        public bool CanHarvest(FarmRuleContext context, out string reason)
        {
            return harvestRulePipeline.CanExecute(context, out reason);
        }

        /// <summary>
        /// 判断当前上下文是否允许铲除已有作物。
        /// </summary>
        public bool CanRemoveCrop(FarmRuleContext context, out string reason)
        {
            return removeCropRulePipeline.CanExecute(context, out reason);
        }
    }
}