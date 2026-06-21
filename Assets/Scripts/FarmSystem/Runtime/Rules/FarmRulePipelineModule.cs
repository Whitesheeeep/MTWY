using System;

namespace FarmSystem
{
    /// <summary>
    /// 农田规则管线模块，按顺序执行一组农田动作规则。
    /// </summary>
    public sealed class FarmRulePipelineModule
    {
        // Farm 专属的规则链执行器。按顺序短路校验，不修改上下文，也不触发任何表现。
        private static readonly IFarmActionRule[] EmptyRules = Array.Empty<IFarmActionRule>();

        private readonly IFarmActionRule[] rules;

        public FarmRulePipelineModule(IFarmActionRule[] defaultRules)
        {
            if (defaultRules == null || defaultRules.Length == 0)
            {
                rules = EmptyRules;
                return;
            }

            rules = new IFarmActionRule[defaultRules.Length];
            Array.Copy(defaultRules, rules, defaultRules.Length);
        }

        public bool CanExecute(FarmRuleContext context, out string reason)
        {
            for (int i = 0; i < rules.Length; i++)
            {
                IFarmActionRule rule = rules[i];
                if (rule == null)
                {
                    continue;
                }

                if (!rule.IsMatch(context, out reason))
                {
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }
    }
}
