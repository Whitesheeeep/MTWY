using System;
using UnityEngine;
using WS_Modules.Singleton;
using WS_Modules.UIModule;

namespace GameData
{
    /// <summary>
    /// 对话系统全局入口，负责创建和管理当前正在播放的 DialogueSession。
    /// </summary>
    public sealed class DialogueManager : SingletonBase<DialogueManager>, IDisposable
    {
        #region 字段
        private DialogueSession currentSession;
        #endregion

        #region 事件
        /// <summary>
        /// 当前对话会话发生变化时触发。
        /// </summary>
        public event Action<DialogueSession> CurrentSessionChanged;

        /// <summary>
        /// 新对话会话开始后触发。
        /// </summary>
        public event Action<DialogueSession> DialogueStarted;

        /// <summary>
        /// 当前对话会话结束后触发。
        /// </summary>
        public event Action<DialogueSession> DialogueEnded;
        #endregion

        #region 初始化
        private DialogueManager()
        {
        }
        #endregion

        #region 属性
        /// <summary>
        /// 当前正在播放的对话会话。
        /// </summary>
        public DialogueSession CurrentSession => currentSession;

        /// <summary>
        /// 当前是否存在正在运行或等待清理的对话会话。
        /// </summary>
        public bool HasCurrentSession => currentSession != null;

        /// <summary>
        /// 当前对话会话持有的 Runner。
        /// </summary>
        public DialogueRunner CurrentRunner => currentSession?.Runner;
        #endregion

        #region 对话生命周期
        /// <summary>
        /// 使用默认空服务表开始一个新的对话会话。
        /// </summary>
        /// <param name="graph">要播放的对话图。</param>
        /// <param name="options">本次对话启动配置。</param>
        /// <returns>创建并启动后的对话会话。</returns>
        public DialogueSession StartDialogue(DialogueGraph_SO graph, DialogueStartOptions options = null)
        {
            return StartDialogue(graph, new DialogueServices(), options);
        }

        /// <summary>
        /// 使用指定服务表开始一个新的对话会话。
        /// </summary>
        /// <param name="graph">要播放的对话图。</param>
        /// <param name="services">Runner 使用的运行时服务表。</param>
        /// <param name="options">本次对话启动配置。</param>
        /// <returns>创建并启动后的对话会话。</returns>
        public DialogueSession StartDialogue(DialogueGraph_SO graph, IDialogueServices services, DialogueStartOptions options = null)
        {
            EndCurrentDialogue();

            DialogueSession startedSession = new DialogueSession(graph, services ?? new DialogueServices(), options);
            currentSession = startedSession;
            currentSession.StateChanged += OnCurrentSessionStateChanged;
            currentSession.Ended += OnCurrentSessionEnded;
            startedSession.Start();

            if (!ReferenceEquals(currentSession, startedSession))
            {
                return startedSession;
            }

            ShowDialogueWindow();
            DialogueStarted?.Invoke(startedSession);
            CurrentSessionChanged?.Invoke(startedSession);
            return startedSession;
        }

        /// <summary>
        /// 结束并释放当前对话会话。
        /// </summary>
        public void EndCurrentDialogue()
        {
            if (currentSession == null)
            {
                return;
            }

            DialogueSession endedSession = currentSession;
            currentSession = null;
            endedSession.StateChanged -= OnCurrentSessionStateChanged;
            endedSession.Ended -= OnCurrentSessionEnded;
            endedSession.Dispose();

            DestroyDialogueWindow();
            DialogueEnded?.Invoke(endedSession);
            CurrentSessionChanged?.Invoke(null);
        }

        /// <summary>
        /// 释放 Manager 当前持有的对话会话。
        /// </summary>
        public void Dispose()
        {
            EndCurrentDialogue();
            CurrentSessionChanged = null;
            DialogueStarted = null;
            DialogueEnded = null;
        }
        #endregion

        #region 对话推进
        /// <summary>
        /// 继续当前没有选项的对白。
        /// </summary>
        public void Continue()
        {
            if (!TryGetCurrentSession(out DialogueSession session))
            {
                return;
            }

            session.Continue();
        }

        /// <summary>
        /// 选择当前对话中的指定选项。
        /// </summary>
        /// <param name="choiceIndex">当前选项列表中的索引。</param>
        public void SelectChoice(int choiceIndex)
        {
            if (!TryGetCurrentSession(out DialogueSession session))
            {
                return;
            }

            session.SelectChoice(choiceIndex);
        }

        /// <summary>
        /// 设置当前会话中指定说话人的头像站位。
        /// </summary>
        /// <param name="speakerId">说话人 Id。</param>
        /// <param name="side">目标头像站位。</param>
        public void SetSpeakerSide(string speakerId, DialoguePortraitSide side)
        {
            if (!TryGetCurrentSession(out DialogueSession session))
            {
                return;
            }

            session.SetSpeakerSide(speakerId, side);
        }
        #endregion

        #region 查询
        /// <summary>
        /// 尝试获取当前对话会话。
        /// </summary>
        /// <param name="session">当前对话会话。</param>
        /// <returns>存在当前会话时返回 true。</returns>
        public bool TryGetCurrentSession(out DialogueSession session)
        {
            session = currentSession;
            if (session != null)
            {
                return true;
            }

            Debug.LogWarning("[DialogueManager] Current session is null.");
            return false;
        }
        #endregion

        #region 内部事件
        private void OnCurrentSessionStateChanged()
        {
            if (currentSession == null)
            {
                return;
            }

            CurrentSessionChanged?.Invoke(currentSession);
        }

        private void OnCurrentSessionEnded(DialogueSession session)
        {
            if (!ReferenceEquals(session, currentSession))
            {
                return;
            }

            EndCurrentDialogue();
        }

        private static void ShowDialogueWindow()
        {
            if (UIManager.Instance == null || !UIManager.Instance.IsInitialized)
            {
                Debug.LogWarning("[DialogueManager] UIManager is not initialized. DialogueWindow will not be opened.");
                return;
            }

            UIManager.Instance.PopUpWindow<DialogueWindow>();
        }

        private static void DestroyDialogueWindow()
        {
            if (UIManager.Instance == null || !UIManager.Instance.IsInitialized)
            {
                Debug.LogWarning("[DialogueManager] UIManager is not initialized. DialogueWindow will not be destroyed.");
                return;
            }

            UIManager.Instance.DestroyWindow<DialogueWindow>();
        }
        #endregion
    }
}
