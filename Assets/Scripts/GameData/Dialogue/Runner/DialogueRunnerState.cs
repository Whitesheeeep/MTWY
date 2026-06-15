namespace GameData
{
    /// <summary>
    /// 对话 Runner 当前所处的运行状态。
    /// </summary>
    public enum DialogueRunnerState
    {
        /// <summary>
        /// 尚未开始或已经手动停止。
        /// </summary>
        Idle,

        /// <summary>
        /// 当前停留在普通对白节点，可通过 Continue 推进。
        /// </summary>
        Speech,

        /// <summary>
        /// 当前对白存在可选分支，需要通过 SelectChoice 推进。
        /// </summary>
        Choice,

        /// <summary>
        /// 当前对话已经结束，或因数据不完整被安全终止。
        /// </summary>
        Ended
    }
}
