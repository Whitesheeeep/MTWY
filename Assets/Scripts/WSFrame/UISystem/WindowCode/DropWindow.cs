// WSFrame WindowCode 生成规则：
// 1. 本文件首次由生成器创建，创建后作为手写窗口逻辑入口。
// 2. 后续重新生成不会整体覆盖本文件。
// 3. 生命周期方法、API 方法、MVVM 绑定和业务逻辑不会被生成器修改。
// 4. UI 事件方法一旦存在，生成器不会覆盖其方法体。
// 5. 当 UI 新增可绑定事件组件时，生成器只会追加缺失的事件空方法。
// 6. 当 UI 删除、重命名或修改组件类型时，旧事件方法不会自动删除，请手动清理。
using UnityEngine.UI;
using UnityEngine;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// DropWindow 打开参数，用于传入拖拽物品图标和初始屏幕坐标。
    /// </summary>
    public readonly struct DropWindowOpenContext
    {
        /// <summary>
        /// 拖拽物品图标。
        /// </summary>
        public Sprite Icon { get; }

        /// <summary>
        /// 初始屏幕坐标。
        /// </summary>
        public Vector2 StartScreenPosition { get; }

        /// <summary>
        /// 创建 DropWindow 打开参数。
        /// </summary>
        public DropWindowOpenContext(Sprite icon, Vector2 startScreenPosition)
        {
            Icon = icon;
            StartScreenPosition = startScreenPosition;
        }
    }

	public partial class DropWindow:WindowBase, IWindowWithOpenContext<DropWindowOpenContext>
	{
        private RectTransform dropItemRect;

		 #region 生命周期函数
		 //调用机制与Mono Awake一致
		 public override void OnAwake()
		 {
			 SetDoAnimation(false);
			 BindGeneratedComponents();
			 base.OnAwake();
             EnsureDropItemReferences();
             HideDropItem();
		 }
		 //物体显示时执行
		 public override void OnShow()
		 {
			 base.OnShow();
		 }
		 //物体隐藏时执行
		 public override void OnHide()
		 {
             HideDropItem();
			 base.OnHide();
		 }
		 //物体销毁时执行
		 public override void OnDestroy()
		 {
			 base.OnDestroy();
		 }
		 #endregion
		 #region API Function
         /// <summary>
         /// 应用本次打开时传入的拖拽物品图标和初始位置。
         /// </summary>
         /// <param name="context">DropWindow 打开参数。</param>
         public void ApplyOpenContext(DropWindowOpenContext context)
         {
             EnsureDropItemReferences();
             ShowDropItem(context.Icon, context.StartScreenPosition);
         }

         /// <summary>
         /// 显示拖拽物品图标。
         /// </summary>
         /// <param name="icon">拖拽物品图标。</param>
         /// <param name="screenPosition">当前屏幕坐标。</param>
         public void ShowDropItem(Sprite icon, Vector2 screenPosition)
         {
             EnsureDropItemReferences();
             if (dataCompt?.DropItemImage == null) return;

             dataCompt.DropItemImage.sprite = icon;
             dataCompt.DropItemImage.enabled = icon != null;
             MoveToScreenPosition(screenPosition);
         }

         /// <summary>
         /// 将拖拽物品图标移动到指定屏幕坐标。
         /// </summary>
         /// <param name="screenPosition">目标屏幕坐标。</param>
         public void MoveToScreenPosition(Vector2 screenPosition)
         {
             EnsureDropItemReferences();
             if (dropItemRect == null) return;

             RectTransform canvasRect = Canvas != null ? Canvas.transform as RectTransform : null;
             Camera uiCamera = Canvas != null && Canvas.worldCamera != null
                 ? Canvas.worldCamera
                 : UIManager.Instance.Camera;
             if (canvasRect != null &&
                 RectTransformUtility.ScreenPointToLocalPointInRectangle(
                     canvasRect,
                     screenPosition,
                     uiCamera,
                     out Vector2 localPosition))
             {
                 dropItemRect.anchoredPosition = localPosition;
                 return;
             }

             dropItemRect.position = screenPosition;
         }

         /// <summary>
         /// 隐藏拖拽物品图标。
         /// </summary>
         public void HideDropItem()
         {
             EnsureDropItemReferences();
             if (dataCompt?.DropItemImage == null) return;

             dataCompt.DropItemImage.enabled = false;
             dataCompt.DropItemImage.sprite = null;
         }

         private void EnsureDropItemReferences()
         {
             if (dataCompt?.DropItemImage == null) return;

             dropItemRect ??= dataCompt.DropItemImage.rectTransform;
             dataCompt.DropItemImage.raycastTarget = false;
         }
		 #endregion
		 #region UI组件事件
		 #endregion
	}
}
