using Inventory;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 背包 ViewModel 定位器，为不同窗口提供同一份 Bar/Bag ViewModel 实例。
    /// </summary>
    public static class InventoryViewModelLocator
    {
        private static InventoryManager cachedManager;
        private static InventoryBarViewModel barViewModel;
        private static InventoryBagViewModel bagViewModel;

        /// <summary>
        /// 获取 Bar ViewModel。
        /// </summary>
        /// <returns>Bar ViewModel 实例。</returns>
        public static InventoryBarViewModel GetBarViewModel()
        {
            EnsureViewModels();
            return barViewModel;
        }

        /// <summary>
        /// 获取 Bag ViewModel。
        /// </summary>
        /// <returns>Bag ViewModel 实例。</returns>
        public static InventoryBagViewModel GetBagViewModel()
        {
            EnsureViewModels();
            return bagViewModel;
        }

        /// <summary>
        /// 释放当前缓存的 ViewModel。
        /// </summary>
        public static void Dispose()
        {
            barViewModel?.Dispose();
            bagViewModel?.Dispose();
            barViewModel = null;
            bagViewModel = null;
            cachedManager = null;
        }

        private static void EnsureViewModels()
        {
            InventoryManager manager = InventoryManager.Instance;
            if (cachedManager == manager && barViewModel != null && bagViewModel != null)
            {
                return;
            }

            Dispose();
            cachedManager = manager;
            barViewModel = new InventoryBarViewModel(manager);
            bagViewModel = new InventoryBagViewModel(manager);
        }
    }
}
