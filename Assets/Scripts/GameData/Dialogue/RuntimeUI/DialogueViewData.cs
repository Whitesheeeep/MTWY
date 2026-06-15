using System.Collections.Generic;
using UnityEngine;
using WS_Modules.MVVM;

namespace GameData
{
    /// <summary>
    /// 对话窗口当前对白的 UI 显示数据。
    /// </summary>
    public sealed class DialogueViewData : IViewData
    {
        #region 初始化
        /// <summary>
        /// 创建当前对白显示数据。
        /// </summary>
        public DialogueViewData(
            string speakerId,
            string speakerName,
            Sprite portraitSprite,
            bool isLeftPortrait,
            string text,
            bool canContinue,
            IReadOnlyList<DialogueChoiceViewData> choices)
        {
            SpeakerId = speakerId;
            SpeakerName = speakerName;
            PortraitSprite = portraitSprite;
            IsLeftPortrait = isLeftPortrait;
            Text = text;
            CanContinue = canContinue;
            Choices = choices ?? new List<DialogueChoiceViewData>();
        }
        #endregion

        #region 属性
        /// <summary>
        /// 当前说话人 Id。
        /// </summary>
        public string SpeakerId { get; }

        /// <summary>
        /// 当前说话人的显示名。
        /// </summary>
        public string SpeakerName { get; }

        /// <summary>
        /// 当前说话人的头像 Sprite。
        /// </summary>
        public Sprite PortraitSprite { get; }

        /// <summary>
        /// 当前头像是否显示在左侧头像位。
        /// </summary>
        public bool IsLeftPortrait { get; }

        /// <summary>
        /// 当前对白文本。
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// 当前对白是否可以通过 Continue 推进。
        /// </summary>
        public bool CanContinue { get; }

        /// <summary>
        /// 当前可显示的选项列表。
        /// </summary>
        public IReadOnlyList<DialogueChoiceViewData> Choices { get; }
        #endregion
    }
}
