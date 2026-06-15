#if UNITY_EDITOR
using System.Text;
using Cysharp.Threading.Tasks;
using Gameplay.TimeSystem;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WS_Modules.UIModule;

namespace GameData
{
    /// <summary>
    /// 基于 Odin Inspector 的对话窗口手动测试组件，用于验证 DialogueManager、DialogueWindow 和 Choice 槽位刷新链路。
    /// </summary>
    public sealed class DialogueWindowOdinTester : MonoBehaviour
    {
        #region 字段
        [Title("正式管线")]
        [SerializeField] private DialogueConfig dialogueConfig;

        [Title("直接 Graph 测试")]
        [SerializeField] private DialogueGraph_SO graph;
        [SerializeField] private DialogueStartOptions startOptions = new DialogueStartOptions();
        [SerializeField] private int choiceIndex;

        [Title("测试参数")]
        [SerializeField, Min(0)] private int uiLoadWaitMilliseconds = 300;

        [Title("Condition 服务")]
        [SerializeField] private bool useGameTimeManager = true;
        [SerializeField] private bool fallbackToDebugTime = true;
        [SerializeField, Min(0)] private int debugCurrentDay;
        [SerializeField, Range(0, 23)] private int debugCurrentHour;

        private readonly DebugGameTimeService debugGameTimeService = new DebugGameTimeService();
        #endregion

        #region 运行状态
        /// <summary>
        /// 当前是否存在 DialogueManager 管理的对话会话。
        /// </summary>
        [ShowInInspector, ReadOnly]
        public bool HasCurrentSession => DialogueManager.Instance.HasCurrentSession;

        /// <summary>
        /// 当前 Runner 状态。
        /// </summary>
        [ShowInInspector, ReadOnly]
        public DialogueRunnerState CurrentRunnerState => DialogueManager.Instance.CurrentRunner?.GetState() ?? DialogueRunnerState.Idle;

        /// <summary>
        /// 当前对话窗口是否已经被 UIManager 加载。
        /// </summary>
        [ShowInInspector, ReadOnly]
        public bool HasDialogueWindow => TryGetDialogueWindow(out _);
        #endregion

        #region Manager 测试入口
        /// <summary>
        /// 通过场景中的 DialogueConfig 启动对话，完整走资源加载、服务工厂和 DialogueManager 管线。
        /// </summary>
        [Button("Start Dialogue From Config", ButtonSizes.Large)]
        public async void StartDialogueFromConfig()
        {
            if (!CanRunConfigOperation("Start Dialogue From Config"))
            {
                return;
            }

            Debug.Log($"[DialogueWindowOdinTester] Start Dialogue From Config address={dialogueConfig.DialogueGraphAddress}");
            dialogueConfig.StartDialogue();
            await WaitForWindowOperation();
            PrintCurrentState();
        }

        /// <summary>
        /// 通过 DialogueManager 启动对话，并等待窗口异步打开后打印状态。
        /// </summary>
        [Button("Start Dialogue With Direct Graph")]
        public async void StartDialogueWithWindow()
        {
            if (!CanRunManagerOperation("Start Dialogue"))
            {
                return;
            }

            Debug.Log($"[DialogueWindowOdinTester] Start Dialogue graph={GetGraphName()}");
            DialogueManager.Instance.StartDialogue(graph, CreateServices(), startOptions);
            await WaitForWindowOperation();
            PrintCurrentState();
        }

        /// <summary>
        /// 通过 DialogueManager 推进当前无选项对白。
        /// </summary>
        [Button("Continue")]
        public async void ContinueDialogue()
        {
            if (!CanRunManagerOperation("Continue"))
            {
                return;
            }

            Debug.Log("[DialogueWindowOdinTester] Continue");
            DialogueManager.Instance.Continue();
            await WaitForWindowOperation();
            PrintCurrentState();
        }

        /// <summary>
        /// 通过 DialogueManager 选择当前选项。
        /// </summary>
        [Button("Select Choice")]
        public async void SelectChoice()
        {
            if (!CanRunManagerOperation("Select Choice"))
            {
                return;
            }

            Debug.Log($"[DialogueWindowOdinTester] Select Choice index={choiceIndex}");
            DialogueManager.Instance.SelectChoice(choiceIndex);
            await WaitForWindowOperation();
            PrintCurrentState();
        }

        /// <summary>
        /// 通过 DialogueManager 结束当前对话，并验证窗口销毁状态。
        /// </summary>
        [Button("End Dialogue")]
        public async void EndDialogue()
        {
            Debug.Log("[DialogueWindowOdinTester] End Dialogue");
            DialogueManager.Instance.EndCurrentDialogue();
            await WaitForWindowOperation();
            PrintCurrentState();
        }
        #endregion

        #region Debug
        /// <summary>
        /// 打印当前 DialogueManager、Runner 和 DialogueWindow 的关键状态。
        /// </summary>
        [Button("Print Current State")]
        public void PrintCurrentState()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("[DialogueWindowOdinTester] Current State");
            AppendManagerState(builder);
            AppendWindowState(builder);
            Debug.Log(builder.ToString());
        }

        private void AppendManagerState(StringBuilder builder)
        {
            DialogueSession session = DialogueManager.Instance.CurrentSession;
            DialogueRunner runner = DialogueManager.Instance.CurrentRunner;

            AppendConfigState(builder);
            builder.AppendLine("Manager:");
            builder.AppendLine($"  UIManager Initialized: {IsUIManagerReady()}");
            builder.AppendLine($"  Has Session: {session != null}");
            builder.AppendLine($"  Graph: {GetGraphName()}");
            builder.AppendLine($"  Runner State: {(runner == null ? "null" : runner.GetState().ToString())}");
            builder.AppendLine($"  Current Node: {(runner == null ? "null" : DialogueRunner.GetNodeName(runner.GetCurrentNode()))}");
            builder.AppendLine($"  Current Choices: {(runner == null ? 0 : runner.GetCurrentChoices().Count)}");
            AppendTimeServiceState(builder, runner?.Services);

            DialogueSpeechNode speech = runner?.GetCurrentSpeech();
            if (speech == null)
            {
                return;
            }

            builder.AppendLine($"  Speech SpeakerId: {speech.SpeakerId}");
            builder.AppendLine($"  Speech PortraitId: {speech.PortraitId}");
            builder.AppendLine($"  Speech Text: {speech.Text}");
        }

        private void AppendWindowState(StringBuilder builder)
        {
            builder.AppendLine("Window:");
            if (!TryGetDialogueWindow(out DialogueWindow window))
            {
                builder.AppendLine("  DialogueWindow: null");
                return;
            }

            DialogueWindowDataComponent dataCompt = window.dataCompt;
            builder.AppendLine($"  Visible: {window.Visible}");
            builder.AppendLine($"  Dialogue Text: {dataCompt?.DialogueTMPTMP_Text?.text}");
            AppendPortraitState(builder, "Left", dataCompt?.LeftPortraitImage);
            AppendPortraitState(builder, "Right", dataCompt?.RightPortraitImage);
            AppendChoiceState(builder, dataCompt?.ChoicesTransform);
        }

        private static void AppendPortraitState(StringBuilder builder, string label, Image image)
        {
            if (image == null)
            {
                builder.AppendLine($"  {label} Portrait: null");
                return;
            }

            string spriteName = image.sprite == null ? "null" : image.sprite.name;
            builder.AppendLine($"  {label} Portrait: enabled={image.enabled}, sprite={spriteName}, color={image.color}");
        }

        private static void AppendChoiceState(StringBuilder builder, Transform choicesTransform)
        {
            if (choicesTransform == null)
            {
                builder.AppendLine("  Choices Root: null");
                return;
            }

            int activeCount = 0;
            builder.AppendLine($"  Choices Root Active: {choicesTransform.gameObject.activeSelf}");
            builder.AppendLine($"  Choice Slot Count: {choicesTransform.childCount}");

            for (int i = 0; i < choicesTransform.childCount; i++)
            {
                Transform child = choicesTransform.GetChild(i);
                if (!child.gameObject.activeSelf)
                {
                    continue;
                }

                activeCount++;
                TMP_Text choiceText = child.GetComponentInChildren<TMP_Text>(true);
                builder.AppendLine($"    [{i}] {choiceText?.text}");
            }

            builder.AppendLine($"  Active Choice Slots: {activeCount}");
        }

        private void AppendConfigState(StringBuilder builder)
        {
            builder.AppendLine("Config:");
            if (dialogueConfig == null)
            {
                builder.AppendLine("  DialogueConfig: null");
                return;
            }

            builder.AppendLine($"  Address: {dialogueConfig.DialogueGraphAddress}");
            builder.AppendLine($"  Is Loading: {dialogueConfig.IsLoading}");
            builder.AppendLine($"  Loaded Graph: {(dialogueConfig.LoadedGraph == null ? "null" : dialogueConfig.LoadedGraph.name)}");
        }
        #endregion

        #region 服务
        private DialogueServices CreateServices()
        {
            DialogueServices services = new DialogueServices();
            IGameTimeService timeService = CreateGameTimeService();
            if (timeService != null)
            {
                services.Register(timeService);
            }

            return services;
        }

        private IGameTimeService CreateGameTimeService()
        {
            if (useGameTimeManager && GameTimeManager.Instance != null)
            {
                Debug.Log("[DialogueWindowOdinTester] Register IGameTimeService from GameTimeManager.Instance.");
                return new GameTimeDialogueService(GameTimeManager.Instance);
            }

            if (!fallbackToDebugTime)
            {
                Debug.LogWarning("[DialogueWindowOdinTester] GameTimeManager is missing and debug fallback is disabled. TimeReachedCondition will fail.");
                return null;
            }

            debugGameTimeService.SetTime(debugCurrentDay, debugCurrentHour);
            Debug.Log($"[DialogueWindowOdinTester] Register debug IGameTimeService. Day={debugCurrentDay}, Hour={debugCurrentHour}");
            return debugGameTimeService;
        }

        private static void AppendTimeServiceState(StringBuilder builder, IDialogueServices services)
        {
            if (services == null || !services.TryGet(out IGameTimeService timeService))
            {
                builder.AppendLine("  Time Service: null");
                return;
            }

            builder.AppendLine($"  Time Service: Day {timeService.CurrentDay}, Hour {timeService.CurrentHour}");
        }
        #endregion

        #region 工具方法
        private async UniTask WaitForWindowOperation()
        {
            if (uiLoadWaitMilliseconds <= 0)
            {
                await UniTask.Yield();
                return;
            }

            await UniTask.Delay(uiLoadWaitMilliseconds);
        }

        private bool CanRunManagerOperation(string operationName)
        {
            if (!IsUIManagerReady())
            {
                Debug.LogWarning($"[DialogueWindowOdinTester] {operationName} failed. UIManager is not initialized.");
                return false;
            }

            if (graph == null)
            {
                Debug.LogWarning($"[DialogueWindowOdinTester] {operationName} failed. Graph is null.");
                return false;
            }

            return true;
        }

        private bool CanRunConfigOperation(string operationName)
        {
            if (!IsUIManagerReady())
            {
                Debug.LogWarning($"[DialogueWindowOdinTester] {operationName} failed. UIManager is not initialized. 请确认场景中 WSFrameRoot 已经初始化。");
                return false;
            }

            if (dialogueConfig == null)
            {
                Debug.LogWarning($"[DialogueWindowOdinTester] {operationName} failed. DialogueConfig is null.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(dialogueConfig.DialogueGraphAddress))
            {
                Debug.LogWarning($"[DialogueWindowOdinTester] {operationName} failed. DialogueConfig graph address is empty.");
                return false;
            }

            return true;
        }

        private static bool TryGetDialogueWindow(out DialogueWindow window)
        {
            window = null;
            if (!IsUIManagerReady())
            {
                return false;
            }

            return UIManager.Instance.TryGetWindow(out window);
        }

        private static bool IsUIManagerReady()
        {
            return UIManager.Instance != null && UIManager.Instance.IsInitialized;
        }

        private string GetGraphName()
        {
            return graph == null ? "null" : graph.name;
        }
        #endregion

        #region Debug 服务
        private sealed class DebugGameTimeService : IGameTimeService
        {
            #region 属性
            /// <inheritdoc />
            public int CurrentDay { get; private set; }

            /// <inheritdoc />
            public int CurrentHour { get; private set; }
            #endregion

            #region 时间维护
            /// <summary>
            /// 设置测试用游戏时间。
            /// </summary>
            /// <param name="day">当前天数。</param>
            /// <param name="hour">当前小时。</param>
            public void SetTime(int day, int hour)
            {
                CurrentDay = Mathf.Max(0, day);
                CurrentHour = Mathf.Clamp(hour, 0, 23);
            }
            #endregion
        }
        #endregion
    }
}
#endif
