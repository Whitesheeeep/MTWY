using WS_Modules.MVVM;

namespace GameData
{
    /// <summary>
    /// 对话选项的 UI 显示数据。
    /// </summary>
    public sealed class DialogueChoiceViewData : IViewData
    {
        #region 初始化
        /// <summary>
        /// 创建一条选项显示数据。
        /// </summary>
        /// <param name="index">当前 Runner 选项列表中的索引。</param>
        /// <param name="choiceText">选项显示文本。</param>
        public DialogueChoiceViewData(int index, string choiceText, bool isInteractable, string disabledReason)
        {
            Index = index;
            ChoiceText = choiceText;
            IsInteractable = isInteractable;
            DisabledReason = disabledReason ?? string.Empty;
        }
        #endregion

        #region 属性
        /// <summary>
        /// 当前 Runner 选项列表中的索引。
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// 选项显示文本。
        /// </summary>
        public string ChoiceText { get; }

        /// <summary>
        /// 閫夐」鏄惁鍙互鐐瑰嚮銆?
        /// </summary>
        public bool IsInteractable { get; }

        /// <summary>
        /// 閫夐」涓嶅彲鐐瑰嚮鏃剁殑鏄剧ず鍘熷洜銆?
        /// </summary>
        public string DisabledReason { get; }
        #endregion
    }
}
