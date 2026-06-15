using Gameplay.TimeSystem;
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 对话 Runner 接入 GameTimeManager 的最小运行时示例。
    /// </summary>
    public sealed class DialogueRunnerGameTimeExample : MonoBehaviour, IDialogueRunnerController
    {
        #region 字段
        [SerializeField] private DialogueGraph_SO graph;
        [SerializeField] private GameTimeManager timeManager;

        private readonly DialogueRunner runner = new DialogueRunner();
        private readonly DialogueServices services = new DialogueServices();

        private GameTimeDialogueService gameTimeService;
        #endregion

        #region 属性
        /// <summary>
        /// 当前示例绑定的对话图资源。
        /// </summary>
        public DialogueGraph_SO Graph => graph;

        /// <summary>
        /// 当前示例持有的对话运行器。
        /// </summary>
        public DialogueRunner Runner => runner;
        #endregion

        #region Unity 生命周期
        private void Start()
        {
            BuildServices();
        }
        #endregion

        #region 对话控制
        /// <summary>
        /// 使用当前对话图启动 Runner。
        /// </summary>
        public void RunnerStart()
        {
            BuildServices();
            runner.Start(graph);
        }

        /// <summary>
        /// 当前对白没有可见选项时继续到下一个节点。
        /// </summary>
        public void RunnerContinue()
        {
            runner.Continue();
        }

        /// <summary>
        /// 选择当前可见选项中的指定索引。
        /// </summary>
        /// <param name="choiceIndex">当前可见选项索引。</param>
        public void RunnerChoiceSelect(int choiceIndex)
        {
            runner.SelectChoice(choiceIndex);
        }
        #endregion

        #region 服务
        private void BuildServices()
        {
            timeManager = timeManager != null ? timeManager : GameTimeManager.Instance;
            gameTimeService = new GameTimeDialogueService(timeManager);
            services.Register<IGameTimeService>(gameTimeService);
            runner.SetServices(services);
        }
        #endregion
    }
}
