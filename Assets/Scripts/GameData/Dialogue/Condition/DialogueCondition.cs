using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 对话条件基类，用于决定 Choice 是否可见或可选。
    /// </summary>
    public abstract class DialogueCondition : ScriptableObject
    {
        #region 条件判断
        /// <summary>
        /// 判断当前条件是否满足。
        /// </summary>
        /// <param name="services">对话运行时服务表。</param>
        /// <param name="failedReason">条件不满足时的失败原因描述。</param>
        /// <returns>条件满足时返回 true。</returns>
        public abstract bool IsMet(IDialogueServices services, out string failedReason);
        #endregion
    }
}
