namespace GameData.CharacterSchedule
{
    /// <summary>
    /// Condition 读取运行时上下文的服务入口。
    /// Condition 只依赖该接口，不直接依赖 Manager 或保存运行时状态。
    /// </summary>
    public interface ICharacterScheduleServices
    {
        /// <summary>
        /// 尝试获取指定类型的日程服务。
        /// </summary>
        bool TryGet<T>(out T service);
    }
}
