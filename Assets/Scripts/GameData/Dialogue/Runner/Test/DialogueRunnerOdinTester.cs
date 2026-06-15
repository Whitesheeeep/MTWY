#if UNITY_EDITOR
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 基于 Odin Inspector 的对话 Runner 手动测试组件，用于 Debug 验证对话图、条件过滤和动作执行。
    /// </summary>
    public sealed class DialogueRunnerOdinTester : MonoBehaviour, IDialogueRunnerController
    {
        #region 字段
        [Title("测试数据")]
        [SerializeField] private DialogueGraph_SO graph;
        [SerializeField] private int choiceIndex;

        [Title("Debug 时间服务")]
        [SerializeField] private int currentDay;
        [SerializeField, Range(0, 23)] private int currentHour;

        [Title("运行状态")]
        [ShowInInspector, ReadOnly] private DialogueRunnerState CurrentState => runner.GetState();
        [ShowInInspector, ReadOnly] private string CurrentNode => DialogueRunner.GetNodeName(runner.GetCurrentNode());
        [ShowInInspector, ReadOnly] private int CurrentChoiceCount => runner.GetCurrentChoices().Count;

        private readonly DialogueRunner runner = new DialogueRunner();
        private readonly DialogueServices services = new DialogueServices();
        private readonly DebugGameTimeService debugGameTimeService = new DebugGameTimeService();
        #endregion

        #region 属性
        /// <summary>
        /// 当前测试器绑定的对话图资源。
        /// </summary>
        public DialogueGraph_SO Graph => graph;

        /// <summary>
        /// 当前测试器持有的对话运行器实例。
        /// </summary>
        public DialogueRunner Runner => runner;
        #endregion

        #region Unity 生命周期
        private void Awake()
        {
            EnsureServices();
        }
        #endregion

        #region 对话操作
        /// <summary>
        /// 从当前 Graph 的 Start 节点开始运行对话。
        /// </summary>
        [Button("Start Dialogue", ButtonSizes.Large)]
        public void StartDialogue()
        {
            Debug.Log($"[DialogueRunnerOdinTester] Start Dialogue graph={GetGraphName()}");
            RunnerStart();
            PrintCurrentState();
        }

        /// <summary>
        /// 当前对白没有选项时，推进到下一个节点。
        /// </summary>
        [Button("Continue")]
        public void ContinueDialogue()
        {
            Debug.Log("[DialogueRunnerOdinTester] Continue");
            RunnerContinue();
            PrintCurrentState();
        }

        /// <summary>
        /// 选择当前 Choice 列表中的指定索引。
        /// </summary>
        [Button("Select Choice")]
        public void SelectChoice()
        {
            Debug.Log($"[DialogueRunnerOdinTester] Select Choice index={choiceIndex}");
            RunnerChoiceSelect(choiceIndex);
            PrintCurrentState();
        }

        /// <summary>
        /// 停止当前对话测试会话。
        /// </summary>
        [Button("Stop")]
        public void StopDialogue()
        {
            Debug.Log("[DialogueRunnerOdinTester] Stop");
            runner.Stop();
            PrintCurrentState();
        }

        /// <summary>
        /// 从测试器绑定的对话图开始运行。
        /// </summary>
        public void RunnerStart()
        {
            EnsureServices();
            runner.Start(graph);
        }

        /// <summary>
        /// 推进当前无选项的对白节点。
        /// </summary>
        public void RunnerContinue()
        {
            EnsureServices();
            runner.Continue();
        }

        /// <summary>
        /// 选择当前选项列表中的指定选项。
        /// </summary>
        /// <param name="choiceIndex">当前选项列表中的索引。</param>
        public void RunnerChoiceSelect(int choiceIndex)
        {
            EnsureServices();
            runner.SelectChoice(choiceIndex);
        }
        #endregion

        #region Debug
        /// <summary>
        /// 打印当前 Runner 状态、当前对白和可选分支。
        /// </summary>
        [Button("Print Current State")]
        public void PrintCurrentState()
        {
            EnsureServices();

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("[DialogueRunnerOdinTester] Current State");
            builder.AppendLine($"Graph: {GetGraphName()}");
            builder.AppendLine($"State: {runner.GetState()}");
            builder.AppendLine($"Node: {DialogueRunner.GetNodeName(runner.GetCurrentNode())}");
            builder.AppendLine($"Debug Time: Day {currentDay}, Hour {currentHour}");

            DialogueSpeechNode speech = runner.GetCurrentSpeech();
            if (speech != null)
            {
                builder.AppendLine($"Speech SpeakerId: {speech.SpeakerId}");
                builder.AppendLine($"Speech Text: {speech.Text}");
                builder.AppendLine($"Speech Next: {DialogueRunner.GetNodeName(speech.NextNode)}");
            }

            var choices = runner.GetCurrentChoices();
            builder.AppendLine($"Choices: {choices.Count}");
            for (int i = 0; i < choices.Count; i++)
            {
                DialogueChoiceNode choice = choices[i];
                string choiceText = choice == null ? "null" : choice.ChoiceText;
                string target = choice == null ? "null" : DialogueRunner.GetNodeName(choice.TargetNode);
                builder.AppendLine($"  [{i}] {choiceText} -> {target}");
            }

            Debug.Log(builder.ToString());
        }
        #endregion

        #region 服务
        private void EnsureServices()
        {
            debugGameTimeService.SetTime(currentDay, currentHour);
            services.Register<IGameTimeService>(debugGameTimeService);
            runner.SetServices(services);
        }
        #endregion

        #region 工具方法
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
