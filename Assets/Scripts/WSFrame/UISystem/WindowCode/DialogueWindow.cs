// WSFrame WindowCode 生成规则：
// 1. 本文件首次由生成器创建，创建后作为手写窗口逻辑入口。
// 2. 后续重新生成不会整体覆盖本文件。
// 3. 生命周期方法、API 方法、MVVM 绑定和业务逻辑不会被生成器修改。
// 4. UI 事件方法一旦存在，生成器不会覆盖其方法体。
// 5. 当 UI 新增可绑定事件组件时，生成器只会追加缺失的事件空方法。
// 6. 当 UI 删除、重命名或修改组件类型时，旧事件方法不会自动删除，请手动清理。
using System.Collections.Generic;
using GameData;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WS_Modules.ResLoadModule;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 对话窗口，负责把 DialogueViewModel 的显示数据刷新到运行时 UI。
    /// </summary>
    public partial class DialogueWindow : WindowBase
    {
        #region 常量
        private const int InitialChoiceSlotCount = 4;
        private static readonly Color ActivePortraitColor = Color.white;
        private static readonly Color InactivePortraitColor = new Color(0f, 0f, 0f, 0.45f);
        #endregion

        #region 字段
        private readonly List<Choice> choiceSlots = new List<Choice>();

        private DialogueViewModel viewModel;
        private EventTrigger dialoguePointerTrigger;
        private EventTrigger.Entry dialoguePointerEntry;
        private TMProTypeWriter typeWriter;
        private int textRevealVersion;
        private bool hasWarnedChoiceExpansion;
        #endregion

        #region 生命周期函数
        /// <summary>
        /// 调用机制与 Mono Awake 一致。
        /// </summary>
        public override void OnAwake()
        {
            BindGeneratedComponents();
            base.OnAwake();
            RegisterDialoguePointerDown();
            // 让 Text 的不接受射线检测，避免干扰到点击继续的事件穿透。
            ConfigureDialogueTextRaycast();
            ResolveTypeWriter();
            EnsureChoiceSlotCount(InitialChoiceSlotCount);
            ClearChoiceSlots();
            ClearPortraits();
        }

        /// <summary>
        /// 物体显示时绑定当前对话 Session。
        /// </summary>
        public override void OnShow()
        {
            base.OnShow();
            BindDialogueViewModel();
        }

        /// <summary>
        /// 物体隐藏时解绑 ViewModel，并清理临时显示状态。
        /// </summary>
        public override void OnHide()
        {
            CancelTextReveal();
            UnbindDialogueViewModel();
            ClearChoiceSlots();
            base.OnHide();
        }

        /// <summary>
        /// 物体销毁时释放全部监听和 Choice 槽位。
        /// </summary>
        public override void OnDestroy()
        {
            UnregisterDialoguePointerDown();
            CancelTextReveal();
            UnbindDialogueViewModel();
            DisposeChoiceSlots();
            base.OnDestroy();
        }
        #endregion

        #region API Function
        #endregion

        #region MVVM 绑定
        private void BindDialogueViewModel()
        {
            UnbindDialogueViewModel();

            DialogueSession session = DialogueManager.Instance.CurrentSession;
            if (session == null)
            {
                RefreshEmptyDialogue();
                return;
            }

            viewModel = new DialogueViewModel(session);
            viewModel.DialogueChanged += RefreshDialogue;
            RefreshDialogue();
        }

        private void UnbindDialogueViewModel()
        {
            CancelTextReveal();

            if (viewModel == null)
            {
                return;
            }

            viewModel.DialogueChanged -= RefreshDialogue;
            viewModel.Dispose();
            viewModel = null;
        }
        #endregion

        #region UI 刷新
        private void RefreshDialogue()
        {
            DialogueViewData viewData = viewModel?.CurrentDialogue;
            if (viewData == null)
            {
                RefreshEmptyDialogue();
                return;
            }

            RefreshPortraits(viewData);
            ClearChoiceSlots();
            RefreshDialogueTextAsync(viewData).Forget();
        }

        private void RefreshEmptyDialogue()
        {
            CancelTextReveal(true);

            if (dataCompt?.DialogueTMPTMP_Text != null)
            {
                dataCompt.DialogueTMPTMP_Text.text = string.Empty;
            }

            ClearPortraits();
            ClearChoiceSlots();
        }

        private async UniTaskVoid RefreshDialogueTextAsync(DialogueViewData viewData)
        {
            if (dataCompt?.DialogueTMPTMP_Text == null)
            {
                return;
            }

            int version = BeginTextReveal();
            string text = viewData.Text ?? string.Empty;

            if (typeWriter == null)
            {
                dataCompt.DialogueTMPTMP_Text.text = text;
                RefreshChoicesIfCurrent(viewData, version);
                return;
            }

            try
            {
                await typeWriter.ShowText(text);
            }
            finally
            {
                RefreshChoicesIfCurrent(viewData, version);
            }
        }

        private void RefreshChoicesIfCurrent(DialogueViewData viewData, int version)
        {
            if (version != textRevealVersion || viewData != viewModel?.CurrentDialogue)
            {
                return;
            }

            RefreshChoices(viewData);
        }

        private void RefreshPortraits(DialogueViewData viewData)
        {
            Image activeImage = viewData.IsLeftPortrait ? dataCompt?.LeftPortraitImage : dataCompt?.RightPortraitImage;
            Image inactiveImage = viewData.IsLeftPortrait ? dataCompt?.RightPortraitImage : dataCompt?.LeftPortraitImage;

            if (activeImage != null)
            {
                activeImage.sprite = viewData.PortraitSprite;
                activeImage.enabled = viewData.PortraitSprite != null;
                activeImage.color = ActivePortraitColor;
            }

            if (inactiveImage != null)
            {
                inactiveImage.color = InactivePortraitColor;
            }
        }

        private void ClearPortraits()
        {
            ClearPortraitImage(dataCompt?.LeftPortraitImage);
            ClearPortraitImage(dataCompt?.RightPortraitImage);
        }

        private static void ClearPortraitImage(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = null;
            image.enabled = false;
            image.color = InactivePortraitColor;
        }

        private void RefreshChoices(DialogueViewData viewData)
        {
            int choiceCount = viewData.Choices?.Count ?? 0;
            EnsureChoiceSlotCount(choiceCount);

            for (int i = 0; i < choiceSlots.Count; i++)
            {
                Choice choiceSlot = choiceSlots[i];
                if (choiceSlot == null)
                {
                    continue;
                }

                if (i >= choiceCount)
                {
                    choiceSlot.ClearItemData();
                    continue;
                }

                DialogueChoiceViewData choice = viewData.Choices[i];
                choiceSlot.SetItemData(choice.Index, choice.ChoiceText, choice.IsInteractable, choice.DisabledReason);
                choiceSlot.RegisterButtonClicked(OnChoiceClicked);
            }

            if (dataCompt?.ChoicesTransform != null)
            {
                dataCompt.ChoicesTransform.gameObject.SetActive(choiceCount > 0);
            }
        }
        #endregion

        #region TMPro TypeWriter
        private void ResolveTypeWriter()
        {
            TMP_Text dialogueText = dataCompt?.DialogueTMPTMP_Text;
            if (dialogueText == null)
            {
                typeWriter = null;
                return;
            }

            typeWriter = dialogueText.GetComponent<TMProTypeWriter>() ?? dialogueText.gameObject.AddComponent<TMProTypeWriter>();
        }

        private int BeginTextReveal()
        {
            CancelTextReveal();
            return ++textRevealVersion;
        }

        private void CancelTextReveal(bool clearText = false)
        {
            textRevealVersion++;
            typeWriter?.StopReveal(clearText);
        }
        #endregion

        #region Choice 槽位

        private void EnsureChoiceSlotCount(int requiredCount)
        {
            if (requiredCount > InitialChoiceSlotCount && !hasWarnedChoiceExpansion)
            {
                Debug.LogWarning($"[DialogueWindow] Choice count {requiredCount} exceeds initial slot count {InitialChoiceSlotCount}. Extra slots will be instantiated.");
                hasWarnedChoiceExpansion = true;
            }

            while (choiceSlots.Count < requiredCount)
            {
                Choice choice = CreateChoiceSlot();
                if (choice == null)
                {
                    break;
                }

                choiceSlots.Add(choice);
            }
        }

        private Choice CreateChoiceSlot()
        {
            if (dataCompt?.ChoicesTransform == null || string.IsNullOrWhiteSpace(dataCompt.ChoiceItemPrefabKey))
            {
                Debug.LogWarning("[DialogueWindow] Choice root or Choice prefab key is missing.");
                return null;
            }

            GameObject choiceObject = ResSystem.Instance.Instantiate(dataCompt.ChoiceItemPrefabKey, dataCompt.ChoicesTransform);
            if (choiceObject == null)
            {
                Debug.LogWarning($"[DialogueWindow] Failed to instantiate Choice prefab: {dataCompt.ChoiceItemPrefabKey}");
                return null;
            }

            Choice choice = choiceObject.GetComponent<Choice>();
            if (choice == null)
            {
                Debug.LogWarning($"[DialogueWindow] Choice prefab has no Choice component: {dataCompt.ChoiceItemPrefabKey}");
                Object.Destroy(choiceObject);
                return null;
            }

            choice.OnInitialize();
            choice.ClearItemData();
            return choice;
        }

        private void ClearChoiceSlots()
        {
            foreach (Choice choice in choiceSlots)
            {
                choice?.ClearItemData();
            }

            if (dataCompt?.ChoicesTransform != null)
            {
                dataCompt.ChoicesTransform.gameObject.SetActive(false);
            }
        }

        private void DisposeChoiceSlots()
        {
            foreach (Choice choice in choiceSlots)
            {
                choice?.OnDispose();
            }

            choiceSlots.Clear();
        }
        #endregion

        #region Continue 点击
        private void RegisterDialoguePointerDown()
        {
            if (dataCompt?.DialogueBKImage == null)
            {
                return;
            }

            dialoguePointerTrigger = dataCompt.DialogueBKImage.GetComponent<EventTrigger>();
            if (dialoguePointerTrigger == null)
            {
                dialoguePointerTrigger = dataCompt.DialogueBKImage.gameObject.AddComponent<EventTrigger>();
            }

            dialoguePointerEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            dialoguePointerEntry.callback.AddListener(OnDialoguePointerDown);
            dialoguePointerTrigger.triggers.Add(dialoguePointerEntry);
        }

        private void UnregisterDialoguePointerDown()
        {
            if (dialoguePointerTrigger != null && dialoguePointerEntry != null)
            {
                dialoguePointerTrigger.triggers.Remove(dialoguePointerEntry);
            }

            dialoguePointerTrigger = null;
            dialoguePointerEntry = null;
        }

        private void ConfigureDialogueTextRaycast()
        {
            TMP_Text dialogueText = dataCompt?.DialogueTMPTMP_Text;
            if (dialogueText != null)
            {
                dialogueText.raycastTarget = false;
            }
        }

        private void OnDialoguePointerDown(BaseEventData eventData)
        {
            DialogueViewData viewData = viewModel?.CurrentDialogue;
            if (IsTextRevealing())
            {
                typeWriter.Skip();
                return;
            }

            if (viewData == null || !viewData.CanContinue || HasVisibleChoices(viewData))
            {
                return;
            }

            viewModel.Continue();
        }

        private static bool HasVisibleChoices(DialogueViewData viewData)
        {
            return viewData.Choices != null && viewData.Choices.Count > 0;
        }

        private bool IsTextRevealing()
        {
            return typeWriter != null && typeWriter.CurrentState == TMProTypeWriter.WriterState.Revealing;
        }
        #endregion

        #region UI组件事件
        private void OnChoiceClicked(int choiceIndex)
        {
            viewModel?.SelectChoice(choiceIndex);
        }

        /// <summary>
        /// UI 事件方法。生成器只在方法缺失时追加，后续不会覆盖方法体。
        /// </summary>
        public void OnBagButtonClick()
        {
        }
        #endregion
    }
}
