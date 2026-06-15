using System;

namespace GameData
{
    /// <summary>
    /// 开始一次对话时传入的运行时配置。
    /// </summary>
    [Serializable]
    public sealed class DialogueStartOptions
    {
        #region 属性
        /// <summary>
        /// 本次对话中显示在左侧头像位的说话人 Id。
        /// </summary>
        [DialogueSpeakerId]
        public string LeftSpeakerId;

        /// <summary>
        /// 本次对话中显示在右侧头像位的说话人 Id。
        /// </summary>
        [DialogueSpeakerId]
        public string RightSpeakerId;
        #endregion
    }
}