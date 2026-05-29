// WSFrame WindowCode 生成规则（以此处说明为准）：
// 1. 本文件首次由生成器创建，创建后作为手写窗口逻辑入口。
// 2. 后续重新生成不会整体覆盖本文件。
// 3. 生命周期函数、API 方法、MVVM 绑定和业务逻辑不会被生成器修改。
// 4. UI 事件方法一旦存在，生成器不会覆盖其方法体。
// 5. 当 UI 新增可绑定事件组件时，生成器只会追加缺失的事件空方法。
// 6. 当 UI 删除、重命名或修改组件类型时，旧事件方法不会自动删除，请手动清理。
using Inventory;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 背包窗口，负责绑定背包 View 和窗口生命周期。
    /// </summary>
    public partial class BagWindow : WindowBase
    {
        #region Fields
        private InventoryBagViewModel bagViewModel;
        #endregion

        #region LifeCycle
        public override void OnAwake()
        {
            BindGeneratedComponents();
            base.OnAwake();
            BindBagViewModel();
        }

        public override void OnShow()
        {
            base.OnShow();
            BindBagViewModel();
        }

        public override void OnHide()
        {
            dataCompt?.BagContentBlockInventoryBagView?.EndDragEdgeScroll();
            base.OnHide();
        }

        public override void OnDestroy()
        {
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.Initialized -= BindBagViewModel;

            dataCompt?.BagContentBlockInventoryBagView?.Unbind();
            bagViewModel?.Dispose();
            bagViewModel = null;
            base.OnDestroy();
        }
        #endregion

        #region UI Events
        /// <summary>
        /// 清理 BagWindow 中全部可见槽位的拖拽放置预览。
        /// </summary>
        public void ClearDropPreview()
        {
            dataCompt?.BagContentBlockInventoryBagView?.ClearDropPreview();
        }
        #endregion

        #region MVVM Binding
        private void BindBagViewModel()
        {
            InventoryManager manager = InventoryManager.Instance;
            if (dataCompt?.BagContentBlockInventoryBagView == null || manager == null) return;

            if (!manager.IsInitialized)
            {
                manager.Initialized -= BindBagViewModel;
                manager.Initialized += BindBagViewModel;
                return;
            }

            manager.Initialized -= BindBagViewModel;
            bagViewModel ??= new InventoryBagViewModel(manager.BagContainer, manager.ItemDatabase);
            dataCompt.BagContentBlockInventoryBagView.Bind(bagViewModel);
        }
        #endregion
    }
}
