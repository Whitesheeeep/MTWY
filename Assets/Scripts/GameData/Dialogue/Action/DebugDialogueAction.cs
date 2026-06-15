using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 对话 Debug 动作，用于在选择 Choice 后向 Console 输出一条测试日志。
    /// </summary>
    [CreateAssetMenu(fileName = "DebugDialogueAction", menuName = "GameData/Dialogue/Action/Debug Log", order = 1)]
    public sealed class DebugDialogueAction : DialogueAction
    {
        #region 字段
        [SerializeField] private string message = "Dialogue action executed.";
        [SerializeField] private bool includeGameTime = true;
        #endregion

        #region 动作执行
        /// <inheritdoc />
        public override void Execute(IDialogueServices services)
        {
            if (!includeGameTime || services == null || !services.TryGet(out IGameTimeService timeService))
            {
                Debug.Log($"[DebugDialogueAction] {message}");
                return;
            }

            Debug.Log($"[DebugDialogueAction] {message} Time=Day {timeService.CurrentDay}, Hour {timeService.CurrentHour}");
        }
        #endregion
    }
}
