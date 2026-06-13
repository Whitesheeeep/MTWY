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
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color interactableColor = Color.green;

        private ToolCursorViewModel viewModel;
        private RectTransform rectTransform;
        private Canvas canvas;

        public void Bind(ToolCursorViewModel toolCursorViewModel)
        {
            Unbind();
            viewModel = toolCursorViewModel;
            viewModel.Changed += Refresh;
            EnsureReferences();
            Refresh();
        }

        public void Unbind()
        {
            if (viewModel != null)
            {
                viewModel.Changed -= Refresh;
                viewModel = null;
            }
        }

        private void Awake()
        {
            EnsureReferences();
        }

        private void Update()
        {
            FollowMouse();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Refresh()
        {
            EnsureReferences();
            if (cursorImage == null || viewModel == null)
            {
                return;
            }

            bool visible = viewModel.Visible;
            cursorImage.enabled = visible;
            cursorImage.sprite = visible ? viewModel.Icon : null;
            cursorImage.color = viewModel.VisualState == CursorVisualState.Interactable
                ? interactableColor
                : normalColor;
        }

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
