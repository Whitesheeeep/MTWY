/*---------------------------------
 *Title:UI表现层脚本自动化生成工具
 *Description:UI 表现层只负责界面的交互、表现相关的更新，不编写业务数据逻辑。
 *注意:以下文件由自动生成工具创建，手动追加的 MVVM 绑定逻辑请避免被后续生成覆盖。
---------------------------------*/
using UnityEngine;
using Inventory;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 全局 UI 窗口，负责绑定快捷栏 View 和窗口级 UI 事件。
    /// </summary>
    public class GlobalUIWindow : WindowBase
    {
        public GlobalUIWindowDataComponent dataCompt;

        private InventoryBarViewModel barViewModel;

        #region 生命周期函数
        public override void OnAwake()
        {
            dataCompt = GameObject.GetComponent<GlobalUIWindowDataComponent>();
            dataCompt.InitComponent(this);
            base.OnAwake();
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

            dataCompt.BarFrameInventoryBarView.Initialize(OnBarSlotClicked);
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
    }
}
