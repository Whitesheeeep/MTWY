using System;
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 单次对话播放会话，负责持有 Runner、启动配置和本次 UI 站位上下文，以及本次对话的相关引用的生命周期。
    /// </summary>
    public sealed class DialogueSession : IDialogueRunnerController, IDisposable
    {
        #region 字段
        private readonly DialogueRunner runner;
        private readonly DialogueServices ownedServices;

        private DialogueGraph_SO graph;
        private string leftSpeakerId;
        private string rightSpeakerId;
        private bool hasEnded;
        #endregion

        #region 事件
        /// <summary>
        /// Runner 状态或当前节点发生变化时触发。
        /// </summary>
        public event Action StateChanged;
        public event Action<DialogueSession> Ended;
        #endregion

        #region 初始化
        /// <summary>
        /// 创建一个使用空服务表的对话会话。
        /// </summary>
        /// <param name="graph">本次会话运行的对话图。</param>
        /// <param name="options">本次会话启动配置。</param>
        public DialogueSession(DialogueGraph_SO graph, DialogueStartOptions options = null)
            : this(graph, new DialogueServices(), options)
        {
            ownedServices = runner.Services as DialogueServices;
        }

        /// <summary>
        /// 创建一个使用指定服务表的对话会话。
        /// </summary>
        /// <param name="graph">本次会话运行的对话图。</param>
        /// <param name="services">Runner 使用的运行时服务表。</param>
        /// <param name="options">本次会话启动配置。</param>
        public DialogueSession(DialogueGraph_SO graph, IDialogueServices services, DialogueStartOptions options = null)
        {
            this.graph = graph;
            runner = new DialogueRunner(services);
            ApplyStartOptions(options);
        }
        #endregion

        #region 属性
        /// <summary>
        /// 当前会话绑定的对话图资源。
        /// </summary>
        public DialogueGraph_SO Graph => graph;

        /// <summary>
        /// 当前会话持有的对话 Runner。
        /// </summary>
        public DialogueRunner Runner => runner;

        /// <summary>
        /// 当前左侧头像位绑定的说话人 Id。
        /// </summary>
        public string LeftSpeakerId => leftSpeakerId;

        /// <summary>
        /// 当前右侧头像位绑定的说话人 Id。
        /// </summary>
        public string RightSpeakerId => rightSpeakerId;
        #endregion

        #region 对话控制
        /// <summary>
        /// 从当前对话图开始运行本次会话。
        /// </summary>
        public void Start()
        {
            runner.Start(graph);
            NotifyRunnerStateChanged();
        }

        /// <summary>
        /// 继续当前没有选项的对白。
        /// </summary>
        public void Continue()
        {
            runner.Continue();
            NotifyRunnerStateChanged();
        }

        /// <summary>
        /// 选择当前可见选项。
        /// </summary>
        /// <param name="choiceIndex">当前选项列表中的索引。</param>
        public void SelectChoice(int choiceIndex)
        {
            runner.SelectChoice(choiceIndex);
            NotifyRunnerStateChanged();
        }

        /// <summary>
        /// 停止当前对话会话。
        /// </summary>
        public void Stop()
        {
            runner.Stop();
            NotifyRunnerStateChanged(true);
        }

        /// <summary>
        /// 从当前图开始运行对话。
        /// </summary>
        public void RunnerStart()
        {
            Start();
        }

        /// <summary>
        /// 推进当前无选项对白。
        /// </summary>
        public void RunnerContinue()
        {
            Continue();
        }

        /// <summary>
        /// 选择当前选项列表中的指定选项。
        /// </summary>
        /// <param name="choiceIndex">当前选项列表中的索引。</param>
        public void RunnerChoiceSelect(int choiceIndex)
        {
            SelectChoice(choiceIndex);
        }
        #endregion

        #region 站位
        /// <summary>
        /// 设置某个说话人在本次会话中的头像站位。
        /// </summary>
        /// <param name="speakerId">说话人 Id。</param>
        /// <param name="side">目标站位。</param>
        public void SetSpeakerSide(string speakerId, DialoguePortraitSide side)
        {
            if (string.IsNullOrWhiteSpace(speakerId))
            {
                return;
            }

            switch (side)
            {
                case DialoguePortraitSide.Left:
                    leftSpeakerId = speakerId;
                    if (rightSpeakerId == speakerId)
                    {
                        rightSpeakerId = null;
                    }

                    break;
                case DialoguePortraitSide.Right:
                    rightSpeakerId = speakerId;
                    if (leftSpeakerId == speakerId)
                    {
                        leftSpeakerId = null;
                    }

                    break;
                case DialoguePortraitSide.None:
                    ClearSpeakerSide(speakerId);
                    break;
            }

            RaiseStateChanged();
        }

        /// <summary>
        /// 判断指定说话人是否应该显示在左侧头像位。
        /// </summary>
        /// <param name="speakerId">说话人 Id。</param>
        /// <returns>应该显示在左侧时返回 true；右侧时返回 false。</returns>
        public bool IsLeftSpeaker(string speakerId)
        {
            if (string.IsNullOrWhiteSpace(speakerId))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(leftSpeakerId) && leftSpeakerId == speakerId)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(rightSpeakerId) && rightSpeakerId == speakerId)
            {
                return false;
            }

            Debug.LogWarning($"[DialogueSession] Speaker '{speakerId}' has no portrait side. Default to left.");
            return true;
        }
        #endregion

        #region 生命周期
        /// <summary>
        /// 释放当前会话。
        /// </summary>
        public void Dispose()
        {
            StateChanged = null;
            Ended = null;
            ownedServices?.Clear();
        }
        #endregion

        #region 内部工具
        private void ApplyStartOptions(DialogueStartOptions options)
        {
            if (options == null)
            {
                return;
            }

            leftSpeakerId = options.LeftSpeakerId;
            rightSpeakerId = options.RightSpeakerId;

            if (!string.IsNullOrWhiteSpace(leftSpeakerId) && leftSpeakerId == rightSpeakerId)
            {
                Debug.LogWarning($"[DialogueSession] Left and right speaker are both '{leftSpeakerId}'. Right side will be cleared.");
                rightSpeakerId = null;
            }
        }

        private void ClearSpeakerSide(string speakerId)
        {
            if (leftSpeakerId == speakerId)
            {
                leftSpeakerId = null;
            }

            if (rightSpeakerId == speakerId)
            {
                rightSpeakerId = null;
            }
        }

        private void RaiseStateChanged()
        {
            StateChanged?.Invoke();
        }

        private void NotifyRunnerStateChanged(bool forceEnded = false)
        {
            RaiseStateChanged();

            if (forceEnded || runner.GetState() == DialogueRunnerState.Ended)
            {
                RaiseEnded();
            }
        }

        private void RaiseEnded()
        {
            if (hasEnded)
            {
                return;
            }

            hasEnded = true;
            Ended?.Invoke(this);
        }
        #endregion
    }
}
