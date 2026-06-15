using Gameplay.TimeSystem;
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// GameTimeManager 对话服务工厂，负责注册 IGameTimeService。
    /// </summary>
    [CreateAssetMenu(fileName = "GameTimeDialogueServiceFactory", menuName = "GameData/Dialogue/Service Factory/Game Time", order = 1)]
    public sealed class GameTimeDialogueServiceFactory : DialogueServiceFactory
    {
        #region 服务安装
        /// <inheritdoc />
        public override void Install(DialogueServices services)
        {
            if (services == null)
            {
                Debug.LogWarning("[GameTimeDialogueServiceFactory] DialogueServices is null.");
                return;
            }

            GameTimeManager timeManager = GameTimeManager.Instance;
            if (timeManager == null)
            {
                Debug.LogWarning("[GameTimeDialogueServiceFactory] GameTimeManager.Instance is null. IGameTimeService will not be registered.");
                return;
            }

            services.Register<IGameTimeService>(new GameTimeDialogueService(timeManager));
        }
        #endregion
    }
}
