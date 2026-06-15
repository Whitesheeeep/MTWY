using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 对话动作基类，用于在选择 Choice 后向外部系统发出命令。
    /// </summary>
    public abstract class DialogueAction : ScriptableObject
    {
        #region 动作执行
        /// <summary>
        /// 执行当前动作。
        /// </summary>
        /// <param name="services">对话运行时服务表。</param>
        public abstract void Execute(IDialogueServices services);
        #endregion
    }
}
