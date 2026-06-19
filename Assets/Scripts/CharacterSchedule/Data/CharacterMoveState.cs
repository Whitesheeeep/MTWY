namespace GameData.CharacterSchedule
{
    /// <summary>
    /// 角色移动状态。ScheduleManager 只维护逻辑状态，AgentManager 根据状态驱动当前场景实体。
    /// </summary>
    public enum CharacterMoveState
    {
        /// <summary>
        /// 没有可执行日程或当前不需要移动。
        /// </summary>
        Idle,

        /// <summary>
        /// 已生成路径，正在移动。
        /// </summary>
        Moving,

        /// <summary>
        /// 已到达当前日程目标。
        /// </summary>
        Arrived,

        /// <summary>
        /// 当前目标无法到达或配置缺失。
        /// </summary>
        Blocked
    }
}
