namespace FarmSystem
{
    /// <summary>
    /// 农田动作前置条件规则。
    /// </summary>
    public interface IFarmActionRule
    {
        // 规则必须保持无副作用：只判断当前上下文是否允许执行，并返回失败原因。
        // 状态写入、表现刷新、音效动画都应留给 Manager 或后续表现层处理。
        bool IsMatch(FarmRuleContext context, out string reason);
    }
}
