using UnityEngine;
using WS_Modules.InputModule;
using WS_Modules.LogModule;
using WS_Modules.MonoSystem;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// Inventory 槽位拖拽协调器，负责单次拖拽会话、跨 ViewModel 数据移动和拖出 UI 丢弃。
    /// </summary>
    public static class InventorySlotDragCoordinator
    {
        #region Types
        private struct DragSession
        {
            public InventorySlotContainerViewModel SourceViewModel;
            public int SourceIndex;
            public bool IsActive;
        }
        #endregion

        #region Fields
        private static DragSession session;
        private static bool updateRegistered;
        private static bool pendingReleaseFallback;
        #endregion

        #region Properties
        /// <summary>
        /// 当前是否存在有效拖拽会话。
        /// </summary>
        public static bool HasActiveDrag => session.IsActive;

        /// <summary>
        /// 当前拖拽来源槽位索引。
        /// </summary>
        public static int CurrentSourceIndex => session.IsActive ? session.SourceIndex : -1;
        #endregion

        #region Drag Session
        /// <summary>
        /// 处理槽位拖拽开始事件。
        /// </summary>
        /// <param name="sourceViewModel">拖拽来源 ViewModel。</param>
        /// <param name="sourceIndex">拖拽来源槽位索引。</param>
        /// <param name="screenPosition">当前屏幕坐标。</param>
        public static void HandleDragStarted(
            InventorySlotContainerViewModel sourceViewModel,
            int sourceIndex,
            Vector2 screenPosition)
        {
            if (!IsValidSlot(sourceViewModel, sourceIndex) || sourceViewModel.Slots[sourceIndex].IsEmpty)
            {
                EndDragSession();
                return;
            }

            session = new DragSession
            {
                SourceViewModel = sourceViewModel,
                SourceIndex = sourceIndex,
                IsActive = true
            };
            pendingReleaseFallback = false;

            sourceViewModel.SelectSlot(sourceIndex);
            UIManager.Instance.PopUpWindow<DropWindow, DropWindowOpenContext>(
                new DropWindowOpenContext(sourceViewModel.Slots[sourceIndex].icon, screenPosition));
            RegisterDragUpdate();
        }

        /// <summary>
        /// 处理拖拽释放到槽位事件。
        /// </summary>
        /// <param name="targetViewModel">释放目标 ViewModel。</param>
        /// <param name="targetIndex">释放目标槽位索引。</param>
        /// <param name="screenPosition">当前屏幕坐标。</param>
        public static void HandleDropped(
            InventorySlotContainerViewModel targetViewModel,
            int targetIndex,
            Vector2 screenPosition)
        {
            if (!session.IsActive) return;

            InventorySlotContainerViewModel sourceViewModel = session.SourceViewModel;
            if (!IsValidSlot(sourceViewModel, session.SourceIndex) || targetViewModel == null)
            {
                EndDragSession();
                return;
            }

            bool isSameViewModel = ReferenceEquals(sourceViewModel, targetViewModel);
            bool success = isSameViewModel
                ? sourceViewModel.MoveSlot(session.SourceIndex, targetIndex)
                : sourceViewModel.MoveSlotTo(targetViewModel, session.SourceIndex, targetIndex);
            if (success)
            {
                sourceViewModel.SelectSlot(isSameViewModel ? targetIndex : -1);
                targetViewModel.SelectSlot(targetIndex);
            }

            EndDragSession();
        }

        /// <summary>
        /// 处理槽位拖拽结束事件。
        /// </summary>
        /// <param name="sourceViewModel">结束拖拽的来源 ViewModel。</param>
        /// <param name="sourceIndex">结束拖拽的来源槽位索引。</param>
        /// <param name="screenPosition">当前屏幕坐标。</param>
        public static void HandleDragEnded(
            InventorySlotContainerViewModel sourceViewModel,
            int sourceIndex,
            Vector2 screenPosition)
        {
            if (!session.IsActive ||
                !ReferenceEquals(sourceViewModel, session.SourceViewModel) ||
                sourceIndex != session.SourceIndex)
                return;

            DropActiveSlotToWorld();
            EndDragSession();
        }

        /// <summary>
        /// 判断当前拖拽会话是否可以显示目标槽位放置预览。
        /// </summary>
        /// <param name="targetViewModel">目标 ViewModel。</param>
        /// <param name="targetIndex">目标槽位索引。</param>
        public static bool CanDropToSlot(InventorySlotContainerViewModel targetViewModel, int targetIndex)
        {
            return session.IsActive &&
                   targetViewModel != null &&
                   targetIndex >= 0 &&
                   (!ReferenceEquals(session.SourceViewModel, targetViewModel) || session.SourceIndex != targetIndex);
        }

        /// <summary>
        /// 取消指定 ViewModel 发起的当前拖拽会话。
        /// </summary>
        /// <param name="sourceViewModel">可能持有当前拖拽会话的 ViewModel。</param>
        public static void CancelDragForViewModel(InventorySlotContainerViewModel sourceViewModel)
        {
            if (!session.IsActive || !ReferenceEquals(session.SourceViewModel, sourceViewModel)) return;

            EndDragSession();
        }
        #endregion

        #region Tools
        private static bool IsValidSlot(InventorySlotContainerViewModel viewModel, int index)
        {
            return viewModel != null && index >= 0 && index < viewModel.Slots.Count;
        }

        private static void RegisterDragUpdate()
        {
            if (updateRegistered) return;

            PublicMono.Instance.RegisterUpdate(UpdateDragSession);
            updateRegistered = true;
        }

        private static void UnregisterDragUpdate()
        {
            if (!updateRegistered) return;

            PublicMono.Instance.UnRegisterUpdate(UpdateDragSession);
            updateRegistered = false;
        }

        private static void UpdateDragSession()
        {
            if (!session.IsActive)
            {
                UnregisterDragUpdate();
                return;
            }

            MoveDropWindow(InputMgr.Instance.MouseScreenPosition);
            if (pendingReleaseFallback)
            {
                DropActiveSlotToWorld();
                EndDragSession();
                return;
            }

            if (InputMgr.Instance.LeftMouseWasReleasedThisFrame)
                pendingReleaseFallback = true;
        }

        private static void MoveDropWindow(Vector2 screenPosition)
        {
            UIManager.Instance.GetWindow<DropWindow>()?.MoveToScreenPosition(screenPosition);
        }

        private static void DropActiveSlotToWorld()
        {
            if (!session.IsActive || !IsValidSlot(session.SourceViewModel, session.SourceIndex)) return;

            bool success = session.SourceViewModel.DropSlotToWorld(session.SourceIndex);
            if (success)
            {
                RefreshBarSelectionAfterWorldDrop();
            }

            WSLog.Log($"[InventorySlotDragCoordinator] 槽位拖出丢弃结果 index={session.SourceIndex}, success={success}");
        }

        // 快捷栏槽位拖出丢弃后，重新派发当前槽位选择事件，让 Player 同步清空手持物品。
        private static void RefreshBarSelectionAfterWorldDrop()
        {
            if (session.SourceViewModel is not InventoryBarViewModel)
            {
                return;
            }

            session.SourceViewModel.SelectSlot(session.SourceIndex);
        }
        private static void HideDropWindow()
        {
            UIManager.Instance.GetWindow<DropWindow>()?.HideDropItem();
            UIManager.Instance.HideWindow<DropWindow>();
        }

        private static void EndDragSession()
        {
            HideDropWindow();
            UnregisterDragUpdate();
            pendingReleaseFallback = false;
            session = default;
        }
        #endregion
    }
}
