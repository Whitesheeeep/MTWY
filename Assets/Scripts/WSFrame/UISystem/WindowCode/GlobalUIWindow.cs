// WSFrame WindowCode 生成规则（以此处说明为准）：
// 1. 本文件首次由生成器创建，创建后作为手写窗口逻辑入口。
// 2. 后续重新生成不会整体覆盖本文件。
// 3. 生命周期函数、API 方法、MVVM 绑定和业务逻辑不会被生成器修改。
// 4. UI 事件方法一旦存在，生成器不会覆盖其方法体。
// 5. 当 UI 新增可绑定事件组件时，生成器只会追加缺失的事件空方法。
// 6. 当 UI 删除、重命名或修改组件类型时，旧事件方法不会自动删除，请手动清理。
using Inventory;
using CursorSystem;
using Gameplay.TimeSystem;
using WS_Modules.LogModule;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 全局 UI 窗口，负责绑定快捷栏 View 和窗口级 UI 事件。
    /// </summary>
    public partial class GlobalUIWindow : WindowBase
    {
        #region Fields
        private InventoryBarViewModel barViewModel;
        private ToolCursorViewModel toolCursorViewModel;
        #endregion

        #region LifeCycle
        public override void OnAwake()
        {
            BindGeneratedComponents();
            base.OnAwake();
            PreloadWindow();
            BindBarViewModel();
            BindToolCursorViewModel();
            BindTimeUIView();
        }

        public override void OnShow()
        {
            base.OnShow();
            BindBarViewModel();
            BindToolCursorViewModel();
            BindTimeUIView();
        }

        public override void OnDestroy()
        {
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.Initialized -= BindBarViewModel;

            dataCompt?.BarFrameInventoryBarView?.Unbind();
            barViewModel?.Dispose();
            barViewModel = null;
            UnbindToolCursorViewModel();
            UnbindTimeUIView();
            base.OnDestroy();
        }
        #endregion

        #region UI Events
        /// <summary>
        /// 背包按钮点击事件。
        /// </summary>
        public void OnBagButtonClick()
        {
            if (UIManager.Instance.TryGetWindow(out BagWindow bagWindow) && bagWindow.Visible)
            {
                UIManager.Instance.HideWindow<BagWindow>();
            }
            else
                UIManager.Instance.PopUpWindow<BagWindow>();
        }

        /// <summary>
        /// 清理 GlobalUIWindow 中全部快捷栏槽位的拖拽放置预览。
        /// </summary>
        public void ClearDropPreview()
        {
            dataCompt?.BarFrameInventoryBarView?.ClearDropPreview();
        }
        #endregion

        #region MVVM Binding
        private static void PreloadWindow()
        {
            UIManager.Instance.PreLoadWindow<DropWindow>();
            UIManager.Instance.PreLoadWindow<BagWindow>();
            UIManager.Instance.PreLoadWindow<ItemTipWindow>();
        }

        private void BindBarViewModel()
        {
            InventoryManager manager = InventoryManager.Instance;
            if (dataCompt?.BarFrameInventoryBarView == null || manager == null) return;

            if (!manager.IsInitialized)
            {
                manager.Initialized -= BindBarViewModel;
                manager.Initialized += BindBarViewModel;
                return;
            }

            manager.Initialized -= BindBarViewModel;
            barViewModel ??= new InventoryBarViewModel(manager.BarContainer, manager.ItemDatabase);
            dataCompt.BarFrameInventoryBarView.Bind(barViewModel);
        }

        private void BindToolCursorViewModel()
        {
            CursorManager cursorManager = CursorManager.Instance;
            ToolCursorView toolCursorView = dataCompt?.ToolCursorView;
            if (toolCursorView == null || cursorManager == null)
            {
                return;
            }

            toolCursorViewModel ??= new ToolCursorViewModel(cursorManager);
            toolCursorView.Bind(toolCursorViewModel);
        }

        private void UnbindToolCursorViewModel()
        {
            dataCompt?.ToolCursorView?.Unbind();
            toolCursorViewModel?.Dispose();
            toolCursorViewModel = null;
        }

        private void BindTimeUIView()
        {
            TimeUIView timeUIView = dataCompt?.TimeUIView;
            GameTimeManager manager = GameTimeManager.Instance;
            if (timeUIView == null || manager == null)
            {
                WSLog.LogError($"[GlobalUIWindow] TimeUIView or GameTimeManager is null, cannot bind TimeUIView.");
                return;
            }

            timeUIView.Bind(manager);
        }

        private void UnbindTimeUIView()
        {
            dataCompt?.TimeUIView?.Unbind();
        }
        #endregion
    }
}
