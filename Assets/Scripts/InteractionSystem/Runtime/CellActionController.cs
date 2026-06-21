using CursorSystem;
using UnityEngine;
using WS_Modules.InputModule;

namespace InteractionSystem
{
    /// <summary>
    /// 监听玩家点击，并把当前光标交互状态交给实体或地图格子路由执行。
    /// </summary>
    public sealed class CellActionController : MonoBehaviour
    {
        [SerializeField] private bool logFailedActions;

        private readonly ItemCellActionRouter router = new ItemCellActionRouter();

        private void Update()
        {
            if (!InputMgr.Instance.LeftMouseWasPressedThisFrame)
            {
                return;
            }

            CursorManager cursorManager = CursorManager.Instance;
            if (cursorManager == null)
            {
                return;
            }

            TryHandle(cursorManager.CurrentState);
        }

        private bool TryHandle(CursorState state)
        {
            if (!state.CanInteract || !state.HasInteractionContext)
            {
                return false;
            }

            bool success;
            switch (state.TargetType)
            {
                case CursorTargetType.Entity:
                    success = state.Interactable != null &&
                              state.Interactable.TryInteract(state.InteractionContext);
                    break;
                case CursorTargetType.MapCell:
                    success = router.TryHandle(state.InteractionContext);
                    break;
                default:
                    success = false;
                    break;
            }

            if (!success && logFailedActions)
            {
                Debug.LogWarning($"[CellActionController] Interaction failed. TargetType={state.TargetType}, ItemType={state.InteractionContext.SelectedItemType}");
            }

            return success;
        }
    }
}
