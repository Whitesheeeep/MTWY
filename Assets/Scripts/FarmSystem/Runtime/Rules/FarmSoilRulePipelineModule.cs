using GameData;

namespace FarmSystem
{
    /// <summary>
    /// 土地动作规则管线模块，负责耕地类动作的前置条件校验。
    /// </summary>
    public sealed class FarmSoilRulePipelineModule
    {
        // 耕地规则组：用于 hover 高亮和点击执行前的同一套前置条件判断。
        private readonly FarmRulePipelineModule tillRulePipeline =
            new FarmRulePipelineModule(
                new IFarmActionRule[]
                {
                    new MapCellContextRule(),
                    new HasCellFlagRule(MapGridCellFlags.CanDig),
                    new NotTilledRule()
                });

        public bool CanTill(FarmRuleContext context, out string reason)
        {
            return tillRulePipeline.CanExecute(context, out reason);
        }
    }
}
