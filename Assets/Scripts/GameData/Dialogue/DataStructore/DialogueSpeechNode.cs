using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameData
{
    /// <summary>
    /// 对话中的对白节点，保存说话人、文本、线性后续节点和玩家选项分支。
    /// </summary>
    public sealed class DialogueSpeechNode : DialogueNode
    {
        #region 字段
        [FormerlySerializedAs("speakerName")]
        [DialogueSpeakerId]
        [SerializeField] private string speakerId;
        [DialogueSpeakerPortraitId]
        [SerializeField] private string portraitId;
        [SerializeField, TextArea(3, 8)] private string text;
        [SerializeField] private DialogueNode nextNode;
        [SerializeField] private List<DialogueChoiceNode> choices = new List<DialogueChoiceNode>();
        #endregion

        #region 属性
        /// <summary>
        /// 说话人 Id，由 Speaker 数据表提供下拉选择。
        /// </summary>
        public string SpeakerId
        {
            get => speakerId;
            set => speakerId = value;
        }

        /// <summary>
        /// 当前对白使用的头像 Id，等同于 Speaker 头像图集中的 Sprite 名称。
        /// </summary>
        public string PortraitId
        {
            get => portraitId;
            set => portraitId = value;
        }

        /// <summary>
        /// 当前屏幕显示的一段对白文本。
        /// </summary>
        public string Text
        {
            get => text;
            set => text = value;
        }

        /// <summary>
        /// 没有玩家选项时，点击继续进入的下一个节点。
        /// </summary>
        public DialogueNode NextNode
        {
            get => nextNode;
            set => nextNode = value;
        }

        /// <summary>
        /// 当前对白拥有的玩家选项列表。
        /// </summary>
        public IReadOnlyList<DialogueChoiceNode> Choices
        {
            get
            {
                EnsureChoiceList();
                return choices;
            }
        }
        #endregion

        #region 选项维护
        /// <summary>
        /// 添加一个 Choice 分支，重复添加同一节点时会被忽略。
        /// </summary>
        /// <param name="choice">要归属到当前对白的 Choice 节点。</param>
        public void AddChoice(DialogueChoiceNode choice)
        {
            if (choice == null) return;

            EnsureChoiceList();
            if (!choices.Contains(choice))
            {
                choices.Add(choice);
            }
        }

        /// <summary>
        /// 移除指定 Choice 分支。
        /// </summary>
        /// <param name="choice">要从当前对白移除的 Choice 节点。</param>
        public void RemoveChoice(DialogueChoiceNode choice)
        {
            if (choice == null) return;

            EnsureChoiceList();
            choices.RemoveAll(item => item == null || item == choice);
        }

        /// <summary>
        /// 清空当前对白拥有的所有 Choice 分支。
        /// </summary>
        public void ClearChoices()
        {
            EnsureChoiceList();
            choices.Clear();
        }

        /// <summary>
        /// 用指定列表替换当前 Choice 分支，常用于编辑器拆分对白时迁移原分支。
        /// </summary>
        /// <param name="newChoices">新的 Choice 分支列表。</param>
        public void SetChoices(IEnumerable<DialogueChoiceNode> newChoices)
        {
            EnsureChoiceList();
            choices.Clear();

            if (newChoices == null) return;

            foreach (DialogueChoiceNode choice in newChoices)
            {
                AddChoice(choice);
            }
        }

        /// <summary>
        /// 移除空引用，避免删除节点或 Undo/Redo 后留下无效选项。
        /// </summary>
        public void RemoveNullChoices()
        {
            EnsureChoiceList();
            choices.RemoveAll(choice => choice == null);
        }
        #endregion

        #region 工具方法
        private void EnsureChoiceList()
        {
            choices ??= new List<DialogueChoiceNode>();
        }
        #endregion
    }
}
