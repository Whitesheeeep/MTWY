// WSFrame WindowCode 生成规则（以此处说明为准）：
// 1. 本文件首次由生成器创建，创建后作为手写窗口逻辑入口。
// 2. 后续重新生成不会整体覆盖本文件。
// 3. 生命周期方法、API 方法、MVVM 绑定和业务逻辑不会被生成器修改。
// 4. UI 事件方法一旦存在，生成器不会覆盖其方法体。
// 5. 当 UI 新增可绑定事件组件时，生成器只会追加缺失的事件空方法。
// 6. 当 UI 删除、重命名或修改组件类型时，旧事件方法不会自动删除，请手动清理。
using UnityEngine;
using Inventory;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 全局 UI 窗口，负责绑定快捷栏 View 和窗口级 UI 事件。
    /// </summary>
    public partial class GlobalUIWindow : WindowBase
    {
        private InventoryBarViewModel barViewModel;

        #region 生命周期函数
        public override void OnAwake()
        {
            BindGeneratedComponents();
            base.OnAwake();
            UIManager.Instance.PreLoadWindow<DropWindow>();
            BindBarViewModel();
        }

        public override void OnShow()
        {
            base.OnShow();
            RefreshBarSlots();
            RefreshBarSelection();
        }

        public override void OnHide()
        {
            UIManager.Instance.GetWindow<BagWindow>()?.EndDragEdgeScroll();
            base.OnHide();
        }

        public override void OnDestroy()
        {
            UnbindBarViewModel();
            base.OnDestroy();
        }
        #endregion

        #region API Function
        #endregion

        #region UI组件事件
        /// <summary>
        /// 背包按钮点击事件。
        /// </summary>
        public void OnBagButtonClick()
        {
            UIManager.Instance.PopUpWindow<BagWindow>();
        }
        #endregion

        private void BindBarViewModel()
        {
            if (dataCompt?.BarFrameInventoryBarView == null || InventoryManager.Instance == null)
            {
                return;
            }

            barViewModel = InventoryViewModelLocator.GetBarViewModel();
            barViewModel.SlotChanged += RefreshBarSlot;
            barViewModel.SlotsChanged += RefreshBarSlots;
            barViewModel.SelectionChanged += RefreshBarSelection;

            dataCompt.BarFrameInventoryBarView.Initialize(
                OnBarSlotClicked,
                OnBarSlotDragStarted,
                OnBarSlotDragging,
                OnBarSlotDragEnded,
                OnBarSlotDragEntered,
                OnBarSlotDragExited,
                OnBarSlotDropped);
            dataCompt.BarFrameInventoryBarView.SetVisibleSlotCount(barViewModel.Slots.Count);
            RefreshBarSlots();
        }

        private void UnbindBarViewModel()
        {
            if (barViewModel == null)
            {
                return;
            }

            barViewModel.SlotChanged -= RefreshBarSlot;
            barViewModel.SlotsChanged -= RefreshBarSlots;
            barViewModel.SelectionChanged -= RefreshBarSelection;
            barViewModel = null;
        }

        private void RefreshBarSlot(int index)
        {
            if (barViewModel == null || dataCompt?.BarFrameInventoryBarView == null)
            {
                return;
            }

            if (index < 0 || index >= barViewModel.Slots.Count)
            {
                return;
            }

            dataCompt.BarFrameInventoryBarView.RefreshSlot(
                index,
                barViewModel.Slots[index],
                index == barViewModel.SelectedSlotIndex);
        }

        private void RefreshBarSlots()
        {
            if (barViewModel == null || dataCompt?.BarFrameInventoryBarView == null)
            {
                return;
            }

            dataCompt.BarFrameInventoryBarView.RefreshSlots(barViewModel.Slots, barViewModel.SelectedSlotIndex);
        }

        private void RefreshBarSelection()
        {
            if (barViewModel == null || dataCompt?.BarFrameInventoryBarView == null)
            {
                return;
            }

            dataCompt.BarFrameInventoryBarView.RefreshSelection(barViewModel.SelectedSlotIndex);
        }

        private void OnBarSlotClicked(int index)
        {
            barViewModel?.SelectSlot(index);
        }

        /// <summary>
        /// 清理 GlobalUIWindow 中全部快捷栏槽位的拖拽放置预览。
        /// </summary>
        public void ClearDropPreview()
        {
            dataCompt?.BarFrameInventoryBarView?.ClearDropPreview();
        }

        private void OnBarSlotDragStarted(InventorySlotDragEventArgs eventArgs)
        {
            if (barViewModel == null || eventArgs.Index < 0 || eventArgs.Index >= barViewModel.Slots.Count)
            {
                UIManager.Instance.GetWindow<BagWindow>()?.EndDragEdgeScroll();
                InventorySlotDragState.EndDrag();
                return;
            }

            if (barViewModel.Slots[eventArgs.Index].IsEmpty)
            {
                UIManager.Instance.GetWindow<BagWindow>()?.EndDragEdgeScroll();
                InventorySlotDragState.EndDrag();
                return;
            }

            barViewModel.SelectSlot(eventArgs.Index);
            UIManager.Instance.GetWindow<BagWindow>()?.BeginDragEdgeScroll();
            UIManager.Instance.PopUpWindow<DropWindow, DropWindowOpenContext>(
                new DropWindowOpenContext(barViewModel.Slots[eventArgs.Index].icon, eventArgs.ScreenPosition));
        }

        private void OnBarSlotDragging(InventorySlotDragEventArgs eventArgs)
        {
            UIManager.Instance.GetWindow<DropWindow>()?.MoveToScreenPosition(eventArgs.ScreenPosition);
            UIManager.Instance.GetWindow<BagWindow>()?.UpdateDragEdgeScroll(eventArgs.ScreenPosition);
        }

        private void OnBarSlotDropped(InventorySlotDropEventArgs eventArgs)
        {
            if (!InventorySlotDragState.HasActiveDrag) return;

            UIManager.Instance.GetWindow<BagWindow>()?.EndDragEdgeScroll();
            InventorySlotDragState.MarkDropHandled();
            ClearAllDropPreview();
            HideDropWindow();
            if (eventArgs.SourceArea == InventorySlotArea.Bar)
            {
                barViewModel?.MoveSlot(eventArgs.SourceIndex, eventArgs.TargetIndex);
                return;
            }

            if (eventArgs.SourceArea == InventorySlotArea.Bag)
            {
                InventoryViewModelLocator.GetBagViewModel()?.MoveToBar(eventArgs.SourceIndex, eventArgs.TargetIndex);
            }
        }

        private void OnBarSlotDragEntered(InventorySlotDragEventArgs eventArgs)
        {
            dataCompt?.BarFrameInventoryBarView?.RefreshDropPreview(eventArgs.Index, CanDropToSlot(eventArgs.Area, eventArgs.Index));
        }

        private void OnBarSlotDragExited(InventorySlotDragEventArgs eventArgs)
        {
            dataCompt?.BarFrameInventoryBarView?.RefreshDropPreview(eventArgs.Index, false);
        }

        private void OnBarSlotDragEnded(InventorySlotDragEventArgs eventArgs)
        {
            Debug.Log($"[GlobalUIWindow] Bar 拖拽结束 area={eventArgs.Area}, index={eventArgs.Index}, hasDrag={InventorySlotDragState.HasActiveDrag}, dropHandled={InventorySlotDragState.DropHandled}");
            if (!InventorySlotDragState.HasActiveDrag || eventArgs.Area != InventorySlotArea.Bar) return;

            UIManager.Instance.GetWindow<BagWindow>()?.EndDragEdgeScroll();
            if (InventorySlotDragState.DropHandled)
            {
                Debug.Log("[GlobalUIWindow] Bar 拖拽已被槽位接收，不执行丢弃。");
                HideDropWindow();
                ClearAllDropPreview();
                InventorySlotDragState.EndDrag();
                return;
            }

            DropActiveSlotToWorld();
            HideDropWindow();
            ClearAllDropPreview();
            InventorySlotDragState.EndDrag();
        }

        private void DropActiveSlotToWorld()
        {
            Debug.Log($"[GlobalUIWindow] 准备执行拖出 UI 丢弃 sourceArea={InventorySlotDragState.SourceArea}, sourceIndex={InventorySlotDragState.SourceIndex}");
            if (InventorySlotDragState.SourceArea == InventorySlotArea.Bar)
            {
                bool success = barViewModel?.DropSlotToWorld(InventorySlotDragState.SourceIndex) ?? false;
                Debug.Log($"[GlobalUIWindow] Bar 槽位拖出丢弃结果 index={InventorySlotDragState.SourceIndex}, success={success}");
            }
            else if (InventorySlotDragState.SourceArea == InventorySlotArea.Bag)
            {
                bool success = InventoryViewModelLocator.GetBagViewModel()?.DropSlotToWorld(InventorySlotDragState.SourceIndex) ?? false;
                Debug.Log($"[GlobalUIWindow] Bag 槽位从全局窗口拖出丢弃结果 index={InventorySlotDragState.SourceIndex}, success={success}");
            }
        }

        private static bool CanDropToSlot(InventorySlotArea targetArea, int targetIndex)
        {
            return InventorySlotDragState.HasActiveDrag &&
                   targetArea != InventorySlotArea.None &&
                   targetIndex >= 0 &&
                   (InventorySlotDragState.SourceArea != targetArea || InventorySlotDragState.SourceIndex != targetIndex);
        }

        private void ClearAllDropPreview()
        {
            ClearDropPreview();
            UIManager.Instance.GetWindow<BagWindow>()?.ClearDropPreview();
        }

        private static void HideDropWindow()
        {
            UIManager.Instance.GetWindow<DropWindow>()?.HideDropItem();
            UIManager.Instance.HideWindow<DropWindow>();
        }
    }
}
