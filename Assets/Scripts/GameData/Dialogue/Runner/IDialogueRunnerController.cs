namespace GameData
{
    /// <summary>
    /// 对话运行控制器接口，用于把对话资源、Runner 状态和基础推进操作绑定到同一个运行对象上。
    /// </summary>
    public interface IDialogueRunnerController
    {
        #region 属性
        /// <summary>
        /// 当前控制器绑定的对话图资源。
        /// </summary>
        DialogueGraph_SO Graph { get; }

        /// <summary>
        /// 当前控制器持有的对话运行器实例。
        /// </summary>
        DialogueRunner Runner { get; }
        #endregion

        #region 对话控制
        /// <summary>
        /// 从绑定的对话图开始运行对话。
        /// </summary>
        void RunnerStart() => Runner?.Start(Graph);

        /// <summary>
        /// 在当前对话没有选项时推进到下一个节点。
        /// </summary>
        void RunnerContinue() => Runner?.Continue();

        /// <summary>
        /// 选择当前对话选项列表中的指定选项。
        /// </summary>
        /// <param name="choiceIndex">当前选项列表中的索引。</param>
        void RunnerChoiceSelect(int choiceIndex) => Runner?.SelectChoice(choiceIndex);
        #endregion
    }
}
