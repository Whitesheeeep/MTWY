// WSFrame WindowCode 生成规则：
// 1. 本文件首次由生成器创建，创建后作为手写窗口逻辑入口。
// 2. 后续重新生成不会整体覆盖本文件。
// 3. 生命周期方法、API 方法、MVVM 绑定和业务逻辑不会被生成器修改。
// 4. UI 事件方法一旦存在，生成器不会覆盖其方法体。
// 5. 当 UI 新增可绑定事件组件时，生成器只会追加缺失的事件空方法。
// 6. 当 UI 删除、重命名或修改组件类型时，旧事件方法不会自动删除，请手动清理。

using UnityEngine.UI;
using UnityEngine;
using WS_Modules.InputModule;

namespace WS_Modules.UIModule
{
    public readonly struct ItemTipContext
    {
        public string Name { get; }
        public string Type { get; }
        public string Description { get; }
        public string Money { get; }

        public ItemTipContext(string name, string type, string description, string money)
        {
            Name = name;
            Type = type;
            Description = description;
            Money = money;
        }
    }

    public partial class ItemTipWindow : WindowBase, IWindowWithOpenContext<ItemTipContext>
    {
        #region Fields
        // 屏幕坐标中的 Padding，不是 Unity 单位
        private static readonly Vector2 PanelPadding = new Vector2(10f, 10f);
        #endregion

        #region 生命周期函数
        //调用机制与Mono Awake一致
        public override void OnAwake()
        {
            BindGeneratedComponents();
            base.OnAwake();
        }

        //物体显示时执行
        public override void OnShow()
        {
            base.OnShow();
        }

        //物体隐藏时执行
        public override void OnHide()
        {
            base.OnHide();
        }

        //物体销毁时执行
        public override void OnDestroy()
        {
            base.OnDestroy();
        }
        #endregion

        #region API Function
        public void ApplyOpenContext(ItemTipContext context)
        {
            if (dataCompt == null) return;

            dataCompt.NameTMP_Text.text = context.Name;
            dataCompt.TypeTMP_Text.text = context.Type;
            dataCompt.DescriptionTMP_Text.text = context.Description;
            dataCompt.MoneyCountTMP_Text.text = context.Money;
        }

        public void SetPanelPosition(Vector2 pos)
        {
            dataCompt.IntroPanelRectTransform.anchoredPosition = pos;
        }

        /// <summary>
        /// 根据鼠标屏幕坐标设置提示面板位置。
        /// </summary>
        /// <param name="targetScreenSize">目标槽位的屏幕尺寸，只用于计算避让距离。</param>
        public void SetPanelPositionByPointer(Vector2 targetScreenSize)
        {
            if (dataCompt?.IntroPanelRectTransform == null) return;

            RectTransform panelRect = dataCompt.IntroPanelRectTransform;
            RectTransform parentRect = panelRect.parent as RectTransform;
            if (parentRect == null) return;

            // 让 Description 的 ContentSizeFilter 先更新布局，以获取正确的 panelRect 大小。
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);

            Camera uiCamera = GetUICamera();
            Vector2 mouseScreenPosition = InputMgr.Instance.MouseScreenPosition;
            // 将鼠标屏幕坐标转换为父级 RectTransform 的本地坐标，以便后续计算面板位置。
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    mouseScreenPosition,
                    uiCamera,
                    out Vector2 mouseLocalPosition))
                return;

            // 将
            Vector2 panelLocalPosition = CalculatePanelLocalPositionByPointer(
                mouseScreenPosition,
                mouseLocalPosition,
                targetScreenSize,
                panelRect,
                parentRect,
                uiCamera);
            panelRect.localPosition = panelLocalPosition;
            // panelRect.anchoredPosition = LocalPointToAnchoredPosition(panelLocalPosition, panelRect, parentRect);
        }
        #endregion

        #region UI组件事件
        #endregion

        #region Tools
        private Camera GetUICamera()
        {
            if (Canvas == null || Canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
            return Canvas.worldCamera != null ? Canvas.worldCamera : UIManager.Instance.Camera;
        }

        private static Vector2 CalculatePanelLocalPositionByPointer(
            Vector2 mouseScreenPosition,
            Vector2 mouseLocalPosition,
            Vector2 targetScreenSize,
            RectTransform panelRect,
            RectTransform parentRect,
            Camera uiCamera)
        {
            Vector2 targetLocalSize = UIRectUtility.ScreenSizeToLocalSize(
                parentRect,
                targetScreenSize,
                uiCamera,
                mouseScreenPosition);
            Vector2 paddingLocalSize = UIRectUtility.ScreenSizeToLocalSize(
                parentRect,
                PanelPadding,
                uiCamera,
                mouseScreenPosition);
            Rect panelScreenRect = UIRectUtility.GetScreenRect(panelRect, uiCamera);
            Vector2 panelLocalSize = UIRectUtility.ScreenSizeToLocalSize(
                parentRect,
                panelScreenRect.size,
                uiCamera,
                mouseScreenPosition);
            Vector2 pivot = panelRect.pivot;
            float directionX = mouseScreenPosition.x < Screen.width * 0.5f ? 1f : -1f;
            float directionY = mouseScreenPosition.y < Screen.height * 0.5f ? 1f : -1f;

            float x = directionX > 0f
                ? mouseLocalPosition.x + targetLocalSize.x + paddingLocalSize.x + panelLocalSize.x * pivot.x
                : mouseLocalPosition.x - targetLocalSize.x - paddingLocalSize.x - panelLocalSize.x * (1f - pivot.x);
            float y = directionY > 0f
                ? mouseLocalPosition.y + targetLocalSize.y + paddingLocalSize.y + panelLocalSize.y * pivot.y
                : mouseLocalPosition.y - targetLocalSize.y - paddingLocalSize.y - panelLocalSize.y * (1f - pivot.y);

            return new Vector2(x, y);
        }

        private static Vector2 LocalPointToAnchoredPosition(
            Vector2 localPosition,
            RectTransform panelRect,
            RectTransform parentRect)
        {
            Rect parentArea = parentRect.rect;
            Vector2 anchorMinPosition = new Vector2(
                Mathf.Lerp(parentArea.xMin, parentArea.xMax, panelRect.anchorMin.x),
                Mathf.Lerp(parentArea.yMin, parentArea.yMax, panelRect.anchorMin.y));
            Vector2 anchorMaxPosition = new Vector2(
                Mathf.Lerp(parentArea.xMin, parentArea.xMax, panelRect.anchorMax.x),
                Mathf.Lerp(parentArea.yMin, parentArea.yMax, panelRect.anchorMax.y));
            Vector2 anchorReferencePosition = new Vector2(
                Mathf.Lerp(anchorMinPosition.x, anchorMaxPosition.x, panelRect.pivot.x),
                Mathf.Lerp(anchorMinPosition.y, anchorMaxPosition.y, panelRect.pivot.y));
            return localPosition - anchorReferencePosition;
        }

        #endregion
    }
}
