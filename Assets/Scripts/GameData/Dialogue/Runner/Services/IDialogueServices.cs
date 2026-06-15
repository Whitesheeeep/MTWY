namespace GameData
{
    /// <summary>
    /// 对话运行时服务表接口，Condition 和 Action 通过它获取外部系统能力。
    /// </summary>
    public interface IDialogueServices
    {
        #region 服务查询
        /// <summary>
        /// 尝试获取指定类型的运行时服务。
        /// </summary>
        /// <typeparam name="T">服务接口或服务类型。</typeparam>
        /// <param name="service">获取到的服务实例。</param>
        /// <returns>找到服务时返回 true，否则返回 false。</returns>
        bool TryGet<T>(out T service);
        #endregion
    }
}
