using System;
using System.Collections.Generic;
using UnityEngine;
using WS_Modules.CustomEventSystem;
using WS_Modules.MVVM;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// Inventory 槽位容器 View 非泛型核心基类，承载 Unity 可序列化字段和槽位基础刷新。
    /// </summary>
    public abstract class InventorySlotContainerViewBase : MonoBehaviour, IInventorySlotContainerView
    {
        #region Fields
        [SerializeField] private InventorySlotView slotPrefab;
        [SerializeField] private Transform slotRoot;
        private readonly InventorySlotViewEventModule slotEventModule = new InventorySlotViewEventModule();
        #endregion

        #region Properties
        /// <summary>
        /// 槽位预制体。
        /// </summary>
        protected InventorySlotView SlotPrefab
        {
            get => slotPrefab;
            set => slotPrefab = value;
        }

        /// <summary>
        /// 槽位根节点。
        /// </summary>
        protected Transform SlotRoot
        {
            get
            {
                slotRoot ??= transform;
                return slotRoot;
            }
            set => slotRoot = value;
        }

        /// <summary>
        /// 当前显示槽位数量。
        /// </summary>
        /// <summary>
        /// 当前 View 使用的槽位布局。
        /// </summary>
        protected abstract IInventorySlotViewLayout SlotLayout { get; }
        #endregion

        #region Refresh
        /// <summary>
        /// 设置当前可显示槽位数量。
        /// </summary>
        /// <param name="slotCount">槽位数量。</param>
        /// <inheritdoc />
        public virtual void RefreshSlot(int index, InventorySlotViewData data, bool selected)
        {
            SlotLayout?.RefreshSlot(index, data, selected);
        }

        /// <inheritdoc />
        public virtual void RefreshSlots(IReadOnlyList<InventorySlotViewData> slotDataList, int selectedSlotIndex)
        {
            SlotLayout?.RefreshSlots(slotDataList, selectedSlotIndex);
        }

        /// <inheritdoc />
        public virtual void RefreshSelection(int selectedSlotIndex)
        {
            SlotLayout?.RefreshSelection(selectedSlotIndex);
        }

        /// <inheritdoc />
        public virtual void RefreshDropPreview(int index, bool canDrop)
        {
            SlotLayout?.RefreshDropPreview(index, canDrop);
        }

        /// <inheritdoc />
        public virtual void ClearDropPreview()
        {
            SlotLayout?.ClearDropPreview();
        }

        /// <summary>
        /// 清空槽位显示。
        /// </summary>
        public virtual void ClearSlots()
        {
            SlotLayout?.ClearSlots();
        }
        #endregion

        #region Slot Events
        /// <summary>
        /// 注册槽位点击事件。
        /// </summary>
        protected IUnRegister RegisterSlotClicked(Action<InventorySlotClickedEventArgs> handler)
        {
            return slotEventModule.RegisterClicked(handler);
        }

        /// <summary>
        /// 注册槽位开始拖拽事件。
        /// </summary>
        protected IUnRegister RegisterSlotDragStarted(Action<InventorySlotDragEventArgs> handler)
        {
            return slotEventModule.RegisterDragStarted(handler);
        }

        /// <summary>
        /// 注册槽位拖拽结束事件。
        /// </summary>
        protected IUnRegister RegisterSlotDragEnded(Action<InventorySlotDragEventArgs> handler)
        {
            return slotEventModule.RegisterDragEnded(handler);
        }

        /// <summary>
        /// 注册拖拽进入槽位事件。
        /// </summary>
        protected IUnRegister RegisterSlotDragEntered(Action<InventorySlotDragEventArgs> handler)
        {
            return slotEventModule.RegisterDragEntered(handler);
        }

        /// <summary>
        /// 注册拖拽离开槽位事件。
        /// </summary>
        protected IUnRegister RegisterSlotDragExited(Action<InventorySlotDragEventArgs> handler)
        {
            return slotEventModule.RegisterDragExited(handler);
        }

        /// <summary>
        /// 注册拖拽释放到槽位事件。
        /// </summary>
        protected IUnRegister RegisterSlotDropped(Action<InventorySlotDropEventArgs> handler)
        {
            return slotEventModule.RegisterDropped(handler);
        }

        /// <summary>
        /// 清理槽位输入事件。
        /// </summary>
        protected void ClearSlotEvents()
        {
            slotEventModule.Clear();
        }

        protected virtual void ConfigureLayout()
        {
            SlotLayout?.SetContext(
                slotPrefab,
                SlotRoot,
                slotEventModule);
        }
        #endregion
    }

    /// <summary>
    /// Inventory 槽位容器 View 泛型基类，统一处理 ViewModel 绑定和事件订阅。
    /// </summary>
    /// <typeparam name="TViewModel">槽位容器 ViewModel 类型。</typeparam>
    public abstract class InventorySlotContainerViewBase<TViewModel> : InventorySlotContainerViewBase, IView<TViewModel>
        where TViewModel : InventorySlotContainerViewModel
    {
        #region Fields
        private TViewModel viewModel;
        #endregion

        #region Properties
        /// <summary>
        /// 当前绑定的 ViewModel。
        /// </summary>
        protected TViewModel ViewModel => viewModel;

        #endregion

        #region LifeCycle
        /// <summary>
        /// 绑定 ViewModel 并完成初始刷新。
        /// </summary>
        /// <param name="viewModel">槽位容器 ViewModel。</param>
        public virtual void Bind(TViewModel viewModel)
        {
            if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));

            Unbind();
            this.viewModel = viewModel;
            this.viewModel.SlotChanged += RefreshSlotFromViewModel;
            this.viewModel.SlotsChanged += RefreshSlotsFromViewModel;
            this.viewModel.SelectionChanged += RefreshSelectionFromViewModel;
            RegisterSlotInputEvents();

            ConfigureLayout();
            RefreshSlotsFromViewModel();
            RefreshSelectionFromViewModel();
        }

        /// <summary>
        /// 解除 ViewModel 绑定并注销拖拽协调器。
        /// </summary>
        public virtual void Unbind()
        {
            HideItemTipWindow();
            if (viewModel != null)
            {
                InventorySlotDragCoordinator.CancelDragForViewModel(viewModel);
                viewModel.SlotChanged -= RefreshSlotFromViewModel;
                viewModel.SlotsChanged -= RefreshSlotsFromViewModel;
                viewModel.SelectionChanged -= RefreshSelectionFromViewModel;
                viewModel = null;
            }

            ClearSlotEvents();
            SlotLayout?.ClearDropPreview();
        }

        protected virtual void OnDestroy()
        {
            Unbind();
        }
        #endregion

        #region Model Events
        protected virtual void RefreshSlotFromViewModel(int index)
        {
            if (viewModel == null || index < 0 || index >= viewModel.Slots.Count) return;

            RefreshSlot(index, viewModel.Slots[index], index == viewModel.SelectedSlotIndex);
        }

        protected virtual void RefreshSlotsFromViewModel()
        {
            if (viewModel == null) return;

            ConfigureLayout();
            RefreshSlots(viewModel.Slots, viewModel.SelectedSlotIndex);
        }

        protected virtual void RefreshSelectionFromViewModel()
        {
            if (viewModel == null) return;

            RefreshSelection(viewModel.SelectedSlotIndex);
        }
        #endregion

        #region Slot Input
        private void RegisterSlotInputEvents()
        {
            RegisterSlotClicked(OnSlotClicked);
            RegisterSlotDragStarted(OnSlotDragStarted);
            RegisterSlotDragEnded(OnSlotDragEnded);
            RegisterSlotDragEntered(OnSlotDragEntered);
            RegisterSlotDragExited(OnSlotDragExited);
            RegisterSlotDropped(OnSlotDropped);
        }

        private void OnSlotClicked(InventorySlotClickedEventArgs eventArgs)
        {
            viewModel?.SelectSlot(eventArgs.Index);
        }

        private void OnSlotDragStarted(InventorySlotDragEventArgs eventArgs)
        {
            HideItemTipWindow();
            InventorySlotDragCoordinator.HandleDragStarted(viewModel, eventArgs.Index, eventArgs.ScreenPosition);
        }

        private void OnSlotDragEnded(InventorySlotDragEventArgs eventArgs)
        {
            HideItemTipWindow();
            InventorySlotDragCoordinator.HandleDragEnded(viewModel, eventArgs.Index, eventArgs.ScreenPosition);
        }

        private void OnSlotDragEntered(InventorySlotDragEventArgs eventArgs)
        {
            if (!InventorySlotDragCoordinator.HasActiveDrag)
            {
                ShowItemTipWindow(eventArgs.Index, eventArgs.TargetScreenSize);
                return;
            }

            RefreshDropPreview(
                eventArgs.Index,
                InventorySlotDragCoordinator.CanDropToSlot(viewModel, eventArgs.Index));
        }

        private void OnSlotDragExited(InventorySlotDragEventArgs eventArgs)
        {
            RefreshDropPreview(eventArgs.Index, false);
            HideItemTipWindow();
        }

        private void OnSlotDropped(InventorySlotDropEventArgs eventArgs)
        {
            HideItemTipWindow();
            ClearDropPreview();
            InventorySlotDragCoordinator.HandleDropped(viewModel, eventArgs.TargetIndex, eventArgs.ScreenPosition);
        }

        private void ShowItemTipWindow(int index, Vector2 targetScreenSize)
        {
            if (viewModel == null) return;
            if (!viewModel.TryGetItemTipContext(index, out ItemTipContext context)) return;

            UIManager.Instance.PopUpWindow<ItemTipWindow, ItemTipContext>(context);
            UIManager.Instance.GetWindow<ItemTipWindow>()?.SetPanelPositionByPointer(targetScreenSize);
        }

        private static void HideItemTipWindow()
        {
            UIManager.Instance.HideWindow<ItemTipWindow>();
        }
        #endregion
    }
}
