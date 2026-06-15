using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 对话运行时服务工厂基类，负责把具体外部系统适配并注册到 DialogueServices。
    /// </summary>
    public abstract class DialogueServiceFactory : ScriptableObject
    {
        #region 服务安装
        /// <summary>
        /// 将当前工厂负责的服务安装到指定服务表。
        /// </summary>
        /// <param name="services">本次对话运行时使用的服务表。</param>
        public abstract void Install(DialogueServices services);
        #endregion
    }
}
