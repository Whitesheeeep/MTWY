namespace GameData.Editor
{
    /// <summary>
    /// 对话图编辑器校验面板中显示的一条提示信息。
    /// </summary>
    internal readonly struct DialogueGraphValidationMessage
    {
        #region 初始化
        /// <summary>
        /// 创建校验提示。
        /// </summary>
        /// <param name="message">提示文本。</param>
        public DialogueGraphValidationMessage(string message)
        {
            Message = message;
        }
        #endregion

        #region 属性
        /// <summary>
        /// 校验提示文本。
        /// </summary>
        public string Message { get; }
        #endregion
    }
}
