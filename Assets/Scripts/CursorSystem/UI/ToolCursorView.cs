using CursorSystem;
using UnityEngine;
using UnityEngine.UI;
using WS_Modules.InputModule;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 工具鼠标指针 View，负责显示图标、颜色反馈和跟随鼠标。
    /// </summary>
    public sealed class ToolCursorView : MonoBehaviour
    {
        [SerializeField] private Image cursorImage;
        [SerializeField] private Sprite defaultSprite;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color interactableColor = Color.green;

        private ToolCursorViewModel viewModel;
        private RectTransform rectTransform;
        private Canvas canvas;

        /// <summary>
        /// 绑定工具鼠标指针 ViewModel。
        /// </summary>
        public void Bind(ToolCursorViewModel toolCursorViewModel)
        {
            Unbind();
            viewModel = toolCursorViewModel;
            viewModel.Changed += Refresh;
            EnsureReferences();
            Refresh();
        }

        /// <summary>
        /// 解绑当前 ViewModel。
        /// </summary>
        public void Unbind()
        {
            if (viewModel != null)
            {
                viewModel.Changed -= Refresh;
                viewModel = null;
            }
        }

        // 初始化时缓存 UI 引用。
        private void Awake()
        {
            EnsureReferences();
        }

        // 每帧让自定义指针跟随鼠标位置。
        private void Update()
        {
            FollowMouse();
        }

        // 销毁时解除 ViewModel 订阅。
        private void OnDestroy()
        {
            Unbind();
        }

        // 根据 ViewModel 当前状态刷新图标、显隐和颜色。
        private void Refresh()
        {
            EnsureReferences();
            if (cursorImage == null)
            {
                return;
            }

            bool useSelectedIcon = viewModel != null && viewModel.Visible;
            Sprite sprite = useSelectedIcon ? viewModel.Icon : defaultSprite;
            cursorImage.enabled = sprite != null;
            cursorImage.sprite = sprite;
            cursorImage.color = viewModel != null && viewModel.VisualState == CursorVisualState.Interactable
                ? interactableColor
                : normalColor;
        }

        // 将 UI 指针移动到当前鼠标屏幕坐标。
        private void FollowMouse()
        {
            EnsureReferences();
            if (rectTransform == null)
            {
                return;
            }

            Vector2 screenPosition = InputMgr.Instance.MouseScreenPosition;
            RectTransform parentRect = rectTransform.parent as RectTransform;
            if (parentRect == null)
            {
                rectTransform.position = screenPosition;
                return;
            }

            Camera eventCamera = GetCanvasCamera();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPosition, eventCamera, out Vector2 localPoint))
            {
                rectTransform.anchoredPosition = localPoint;
            }
        }

        // 缓存 RectTransform 和所在 Canvas。
        private void EnsureReferences()
        {
            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }

            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }
        }

        // 获取当前 Canvas 坐标转换所需的事件相机。
        private Camera GetCanvasCamera()
        {
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }
    }
}