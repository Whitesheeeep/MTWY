using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using WS_Modules.MVVM;

namespace GameData
{
    /// <summary>
    /// 对话运行时 UI 的 ViewModel，把 DialogueSession 当前状态投影为 DialogueViewData。
    /// </summary>
    public sealed class DialogueViewModel : IViewModel
    {
        #region 字段
        private readonly DialogueSession session;
        private readonly DialoguePortraitLoader portraitLoader;
        private readonly IDialogueSpeakerDatabase speakerDatabase;

        private DialogueViewData currentDialogue;
        private int refreshVersion;
        #endregion

        #region 事件
        /// <summary>
        /// 当前对白显示数据发生变化时触发。
        /// </summary>
        public event Action DialogueChanged;
        #endregion

        #region 初始化
        /// <summary>
        /// 创建对话 UI ViewModel。
        /// </summary>
        /// <param name="session">当前对话会话。</param>
        public DialogueViewModel(DialogueSession session)
            : this(session, new DialoguePortraitLoader(), ResolveSpeakerDatabase())
        {
        }

        /// <summary>
        /// 创建对话 UI ViewModel。
        /// </summary>
        /// <param name="session">当前对话会话。</param>
        /// <param name="portraitLoader">头像加载器。</param>
        /// <param name="speakerDatabase">Speaker 数据库。</param>
        public DialogueViewModel(
            DialogueSession session,
            DialoguePortraitLoader portraitLoader,
            IDialogueSpeakerDatabase speakerDatabase)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.portraitLoader = portraitLoader ?? new DialoguePortraitLoader();
            this.speakerDatabase = speakerDatabase;

            this.session.StateChanged += OnSessionStateChanged;
            RefreshDialogueFromSessionAsync().Forget();
        }
        #endregion

        #region 属性
        /// <summary>
        /// 当前对白显示数据。
        /// </summary>
        public DialogueViewData CurrentDialogue => currentDialogue;
        #endregion

        #region 生命周期
        /// <summary>
        /// 释放 ViewModel 订阅和本次头像资源。
        /// </summary>
        public void Dispose()
        {
            session.StateChanged -= OnSessionStateChanged;
            portraitLoader.ReleaseAll();
            DialogueChanged = null;
        }
        #endregion

        #region Model
        private void OnSessionStateChanged()
        {
            RefreshDialogueFromSessionAsync().Forget();
        }

        private async UniTaskVoid RefreshDialogueFromSessionAsync()
        {
            int version = ++refreshVersion;
            DialogueSpeechNode speech = session.Runner.GetCurrentSpeech();
            if (speech == null)
            {
                currentDialogue = null;
                DialogueChanged?.Invoke();
                return;
            }

            DialogueSpeakerData speaker = ResolveSpeaker(speech.SpeakerId);
            Sprite portraitSprite = await portraitLoader.LoadPortraitAsync(speaker, speech.PortraitId);
            if (version != refreshVersion)
            {
                return;
            }

            currentDialogue = CreateViewData(speech, speaker, portraitSprite);
            DialogueChanged?.Invoke();
        }

        private DialogueViewData CreateViewData(DialogueSpeechNode speech, DialogueSpeakerData speaker, Sprite portraitSprite)
        {
            List<DialogueChoiceViewData> choices = CreateChoiceViewData();
            string speakerName = speaker == null || string.IsNullOrWhiteSpace(speaker.speakerName)
                ? speech.SpeakerId
                : speaker.speakerName;

            return new DialogueViewData(
                speech.SpeakerId,
                speakerName,
                portraitSprite,
                session.IsLeftSpeaker(speech.SpeakerId),
                speech.Text,
                session.Runner.GetState() == DialogueRunnerState.Speech,
                choices);
        }

        private List<DialogueChoiceViewData> CreateChoiceViewData()
        {
            IReadOnlyList<DialogueChoiceNode> choices = session.Runner.GetCurrentChoices();
            List<DialogueChoiceViewData> viewData = new List<DialogueChoiceViewData>(choices.Count);

            for (int i = 0; i < choices.Count; i++)
            {
                DialogueChoiceNode choice = choices[i];
                if (choice == null)
                {
                    continue;
                }

                bool isInteractable = IsChoiceInteractable(choice, out string disabledReason);
                viewData.Add(new DialogueChoiceViewData(i, choice.ChoiceText, isInteractable, disabledReason));
            }

            return viewData;
        }

        private bool IsChoiceInteractable(DialogueChoiceNode choice, out string disabledReason)
        {
            disabledReason = string.Empty;
            if (choice == null)
            {
                disabledReason = "Choice is missing.";
                return false;
            }

            foreach (DialogueCondition condition in choice.Conditions)
            {
                if (condition == null)
                {
                    continue;
                }

                try
                {
                    if (!condition.IsMet(session.Runner.Services, out string failedReason))
                    {
                        disabledReason = string.IsNullOrWhiteSpace(failedReason)
                            ? condition.name
                            : failedReason;
                        return false;
                    }
                }
                catch (Exception exception)
                {
                    disabledReason = exception.Message;
                    Debug.LogError($"[DialogueViewModel] Condition '{condition.name}' failed on choice '{choice.ChoiceText}'.\n{exception}");
                    return false;
                }
            }

            return true;
        }

        private DialogueSpeakerData ResolveSpeaker(string speakerId)
        {
            if (speakerDatabase != null && speakerDatabase.TryGet(speakerId, out DialogueSpeakerData speaker))
            {
                return speaker;
            }

            return null;
        }

        private static IDialogueSpeakerDatabase ResolveSpeakerDatabase()
        {
            return GameDatabase.TryGet(out IDialogueSpeakerDatabase database) ? database : null;
        }
        #endregion

        #region View
        /// <summary>
        /// 继续当前没有选项的对白。
        /// </summary>
        public void Continue()
        {
            session.Continue();
        }

        /// <summary>
        /// 选择当前可见选项。
        /// </summary>
        /// <param name="choiceIndex">当前选项列表中的索引。</param>
        public void SelectChoice(int choiceIndex)
        {
            session.SelectChoice(choiceIndex);
        }

        /// <summary>
        /// 停止当前对话。
        /// </summary>
        public void Stop()
        {
            session.Stop();
        }
        #endregion
    }
}
