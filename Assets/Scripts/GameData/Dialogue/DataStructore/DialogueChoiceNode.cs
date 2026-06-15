using System.Collections.Generic;
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 玩家可选择的分支节点，由 DialogueSpeechNode 持有，并指向选择后的目标节点。
    /// </summary>
    public sealed class DialogueChoiceNode : DialogueNode
    {
        #region 字段
        [SerializeField] private string choiceText;
        [SerializeField] private DialogueNode targetNode;
        [SerializeField] private List<DialogueCondition> conditions = new List<DialogueCondition>();
        [SerializeField] private List<DialogueAction> actions = new List<DialogueAction>();
        #endregion

        #region 属性
        /// <summary>
        /// 玩家在对话 UI 中看到的选项文本。
        /// </summary>
        public string ChoiceText
        {
            get => choiceText;
            set => choiceText = value;
        }

        /// <summary>
        /// 选择该选项后进入的目标节点。
        /// </summary>
        public DialogueNode TargetNode
        {
            get => targetNode;
            set => targetNode = value;
        }

        /// <summary>
        /// 该选项需要满足的条件列表；所有非空条件满足时选项才可见。
        /// </summary>
        public IReadOnlyList<DialogueCondition> Conditions
        {
            get
            {
                EnsureConditionList();
                return conditions;
            }
        }

        /// <summary>
        /// 选择该选项后执行的动作列表。
        /// </summary>
        public IReadOnlyList<DialogueAction> Actions
        {
            get
            {
                EnsureActionList();
                return actions;
            }
        }
        #endregion

        #region 数据维护
        /// <summary>
        /// 移除条件和动作列表中的空引用。
        /// </summary>
        public void RemoveNullExtensions()
        {
            EnsureConditionList();
            EnsureActionList();
            conditions.RemoveAll(condition => condition == null);
            actions.RemoveAll(action => action == null);
        }
        #endregion

        #region 工具方法
        private void EnsureConditionList()
        {
            conditions ??= new List<DialogueCondition>();
        }

        private void EnsureActionList()
        {
            actions ??= new List<DialogueAction>();
        }
        #endregion
    }
}
