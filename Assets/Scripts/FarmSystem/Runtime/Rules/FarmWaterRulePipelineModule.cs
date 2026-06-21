namespace FarmSystem
{
    /// <summary>
    /// 湿润动作规则管线模块，负责浇水类动作的前置条件校验。
    /// </summary>
    public sealed class FarmWaterRulePipelineModule
    {
        // 浇水规则组：只确认格子是否具备被浇水的业务条件，湿润表现由后续表现层处理。
        private readonly FarmRulePipelineModule waterRulePipeline =
            new FarmRulePipelineModule(
                new IFarmActionRule[]
                {
                    new MapCellContextRule(),
                    new TilledRule()
                });

        public bool CanWater(FarmRuleContext context, out string reason)
        {
            return waterRulePipeline.CanExecute(context, out reason);
        }
    }
}
