// WSFrame WindowCode 生成规则（以此处说明为准）：
// 1. 本文件首次由生成器创建，创建后作为手写窗口逻辑入口。
// 2. 后续重新生成不会整体覆盖本文件。
// 3. 生命周期方法、API 方法、MVVM 绑定和业务逻辑不会被生成器修改。
// 4. UI 事件方法一旦存在，生成器不会覆盖其方法体。
// 5. 当 UI 新增可绑定事件组件时，生成器只会追加缺失的事件空方法。
// 6. 当 UI 删除、重命名或修改组件类型时，旧事件方法不会自动删除，请手动清理。
using Inventory;
using UnityEngine;

namespace WS_Modules.UIModule
{
    public partial class BagWindow : WindowBase
    {
        #region 字段
        private InventoryBagViewModel bagViewModel;
        #endregion

        #region 生命周期函数
        //调用机制与Mono Awake一致
        public override void OnAwake()
        {
            BindGeneratedComponents();
            base.OnAwake();
            BindBagViewModel();
        }

        //物体显示时执行
        public override void OnShow()
        {
            base.OnShow();
            if (bagViewModel == null) BindBagViewModel();
            RefreshBagSlots();
            RefreshBagSelection();
        }

        //物体隐藏时执行
        public override void OnHide()
        {
            EndDragEdgeScroll();
            base.OnHide();
        }

        //物体销毁时执行
        public override void OnDestroy()
        {
            UnbindBagViewModel();
            base.OnDestroy();
        }
        #endregion

        #region API Function
        #endregion

        #region MVVM 绑定
        private void BindBagViewModel()
        {
            if (dataCompt?.BagContentBlockInventoryBagView == null || InventoryManager.Instance == null) return;

            bagViewModel = InventoryViewModelLocator.GetBagViewModel();
            bagViewModel.SlotChanged += RefreshBagSlot;
            bagViewModel.SlotsChanged += RefreshBagSlots;
            bagViewModel.SelectionChanged += RefreshBagSelection;

            dataCompt.BagContentBlockInventoryBagView.Initialize(
                OnBagSlotClicked,
                OnBagSlotDragStarted,
                OnBagSlotDragging,
                OnBagSlotDragEnded,
                OnBagSlotDragEntered,
                OnBagSlotDragExited,
                OnBagSlotDropped);
            RefreshBagSlots();
        }

        private void UnbindBagViewModel()
        {
            if (bagViewModel == null) return;

            bagViewModel.SlotChanged -= RefreshBagSlot;
            bagViewModel.SlotsChanged -= RefreshBagSlots;
            bagViewModel.SelectionChanged -= RefreshBagSelection;
            bagViewModel = null;
        }
        #endregion

        #region 刷新
        private void RefreshBagSlot(int index)
        {
            if (bagViewModel == null || dataCompt?.BagContentBlockInventoryBagView == null) return;
            if (index < 0 || index >= bagViewModel.Slots.Count) return;

            dataCompt.BagContentBlockInventoryBagView.RefreshSlot(
                index,
                bagViewModel.Slots[index],
                index == bagViewModel.SelectedSlotIndex,
                bagViewModel.UnlockedSlotCount);
        }

        private void RefreshBagSlots()
        {
            if (bagViewModel == null || dataCompt?.BagContentBlockInventoryBagView == null) return;

            dataCompt.BagContentBlockInventoryBagView.RefreshSlots(
                bagViewModel.Slots,
                bagViewModel.SelectedSlotIndex,
                bagViewModel.UnlockedSlotCount);
        }

        private void RefreshBagSelection()
        {
            if (bagViewModel == null || dataCompt?.BagContentBlockInventoryBagView == null) return;

            dataCompt.BagContentBlockInventoryBagView.RefreshSelection(bagViewModel.SelectedSlotIndex);
        }
        #endregion

        #region UI组件事件
        /// <summary>
        /// 清理 BagWindow 中全部可见槽位的拖拽放置预览。
        /// </summary>
        public void ClearDropPreview()
        {
            dataCompt?.BagContentBlockInventoryBagView?.ClearDropPreview();
        }

        /// <summary>
        /// 开始 Bag 拖拽边缘滚动检测。
        /// </summary>
        public void BeginDragEdgeScroll()
        {
            dataCompt?.BagContentBlockInventoryBagView?.BeginDragEdgeScroll();
        }

        /// <summary>
        /// 根据鼠标位置更新 Bag 拖拽边缘滚动。
        /// </summary>
        /// <param name="screenPosition">鼠标屏幕坐标。</param>
        public void UpdateDragEdgeScroll(Vector2 screenPosition)
        {
            dataCompt?.BagContentBlockInventoryBagView?.UpdateDragEdgeScroll(screenPosition);
        }

        /// <summary>
        /// 结束 Bag 拖拽边缘滚动检测。
        /// </summary>
        public void EndDragEdgeScroll()
        {
            dataCompt?.BagContentBlockInventoryBagView?.EndDragEdgeScroll();
        }

        private void OnBagSlotClicked(int index)
        {
            bagViewModel?.SelectSlot(index);
        }

        private void OnBagSlotDragStarted(InventorySlotDragEventArgs eventArgs)
        {
            if (bagViewModel == null || eventArgs.Index < 0 || eventArgs.Index >= bagViewModel.Slots.Count)
            {
                EndDragEdgeScroll();
                InventorySlotDragState.EndDrag();
                return;
            }

            if (bagViewModel.Slots[eventArgs.Index].IsEmpty)
            {
                EndDragEdgeScroll();
                InventorySlotDragState.EndDrag();
                return;
            }

            bagViewModel.SelectSlot(eventArgs.Index);
            BeginDragEdgeScroll();
            UIManager.Instance.PopUpWindow<DropWindow, DropWindowOpenContext>(
                new DropWindowOpenContext(bagViewModel.Slots[eventArgs.Index].icon, eventArgs.ScreenPosition));
        }

        private void OnBagSlotDragging(InventorySlotDragEventArgs eventArgs)
        {
            UIManager.Instance.GetWindow<DropWindow>()?.MoveToScreenPosition(eventArgs.ScreenPosition);
            UpdateDragEdgeScroll(eventArgs.ScreenPosition);
        }

        private void OnBagSlotDropped(InventorySlotDropEventArgs eventArgs)
        {
            if (!InventorySlotDragState.HasActiveDrag) return;

            EndDragEdgeScroll();
            InventorySlotDragState.MarkDropHandled();
            ClearAllDropPreview();
            HideDropWindow();
            if (eventArgs.SourceArea == InventorySlotArea.Bag)
            {
                bagViewModel?.MoveSlot(eventArgs.SourceIndex, eventArgs.TargetIndex);
                return;
            }

            if (eventArgs.SourceArea == InventorySlotArea.Bar)
            {
                InventoryViewModelLocator.GetBarViewModel()?.MoveToBag(eventArgs.SourceIndex, eventArgs.TargetIndex);
            }
        }

        private void OnBagSlotDragEntered(InventorySlotDragEventArgs eventArgs)
        {
            dataCompt?.BagContentBlockInventoryBagView?.RefreshDropPreview(eventArgs.Index, CanDropToSlot(eventArgs.Area, eventArgs.Index));
        }

        private void OnBagSlotDragExited(InventorySlotDragEventArgs eventArgs)
        {
            dataCompt?.BagContentBlockInventoryBagView?.RefreshDropPreview(eventArgs.Index, false);
        }

        private void OnBagSlotDragEnded(InventorySlotDragEventArgs eventArgs)
        {
            Debug.Log($"[BagWindow] Bag 拖拽结束 area={eventArgs.Area}, index={eventArgs.Index}, hasDrag={InventorySlotDragState.HasActiveDrag}, dropHandled={InventorySlotDragState.DropHandled}");
            if (!InventorySlotDragState.HasActiveDrag || eventArgs.Area != InventorySlotArea.Bag) return;

            EndDragEdgeScroll();
            if (InventorySlotDragState.DropHandled)
            {
                Debug.Log("[BagWindow] Bag 拖拽已被槽位接收，不执行丢弃。");
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
            Debug.Log($"[BagWindow] 准备执行拖出 UI 丢弃 sourceArea={InventorySlotDragState.SourceArea}, sourceIndex={InventorySlotDragState.SourceIndex}");
            if (InventorySlotDragState.SourceArea == InventorySlotArea.Bag)
            {
                bool success = bagViewModel?.DropSlotToWorld(InventorySlotDragState.SourceIndex) ?? false;
                Debug.Log($"[BagWindow] Bag 槽位拖出丢弃结果 index={InventorySlotDragState.SourceIndex}, success={success}");
            }
            else if (InventorySlotDragState.SourceArea == InventorySlotArea.Bar)
            {
                bool success = InventoryViewModelLocator.GetBarViewModel()?.DropSlotToWorld(InventorySlotDragState.SourceIndex) ?? false;
                Debug.Log($"[BagWindow] Bar 槽位从 BagWindow 拖出丢弃结果 index={InventorySlotDragState.SourceIndex}, success={success}");
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
            UIManager.Instance.GetWindow<GlobalUIWindow>()?.ClearDropPreview();
        }

        private static void HideDropWindow()
        {
            UIManager.Instance.GetWindow<DropWindow>()?.HideDropItem();
            UIManager.Instance.HideWindow<DropWindow>();
        }
        #endregion
    }
}
