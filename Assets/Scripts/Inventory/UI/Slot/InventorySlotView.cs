using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// Inventory 槽位 View 组件，负责刷新图标、数量、选中状态，并触发槽位输入事件。
    /// </summary>
    public sealed class InventorySlotView : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IDropHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        #region Fields
        [SerializeField] private Button button;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TMP_Text itemCount;
        [SerializeField] private GameObject chosenIcon;

        private int slotIndex;
        private InventorySlotViewEventModule eventModule;
        private bool selected;
        private bool dropPreview;
        private bool hasItem;
        #endregion

        #region Public Methods
        /// <summary>
        /// 初始化槽位输入上下文。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <param name="eventModule">槽位输入事件模块。</param>
        public void Initialize(int index, InventorySlotViewEventModule eventModule)
        {
            slotIndex = index;
            this.eventModule = eventModule;

            if (button == null) button = gameObject.AddComponent<Button>();

            button.onClick.RemoveListener(OnSlotClicked);
            button.onClick.AddListener(OnSlotClicked);
        }

        /// <summary>
        /// 刷新当前槽位显示状态。
        /// </summary>
        /// <param name="data">槽位显示数据。</param>
        /// <param name="selected">当前槽位是否处于选中状态。</param>
        public void Refresh(InventorySlotViewData data, bool selected)
        {
            hasItem = !data.IsEmpty;
            if (itemIcon != null)
            {
                itemIcon.enabled = hasItem && data.icon != null;
                itemIcon.sprite = hasItem ? data.icon : null;
            }

            if (itemCount != null)
            {
                bool showCount = hasItem && data.count > 1;
                itemCount.gameObject.SetActive(showCount);
                itemCount.text = showCount ? data.count.ToString() : string.Empty;
            }

            RefreshSelection(selected);
        }

        /// <summary>
        /// 刷新当前槽位选中状态。
        /// </summary>
        /// <param name="selected">是否选中。</param>
        public void RefreshSelection(bool selected)
        {
            this.selected = selected;
            RefreshChosenIcon();
        }

        /// <summary>
        /// 刷新当前槽位的拖拽放置预览状态。
        /// </summary>
        /// <param name="canDrop">是否显示为可放置。</param>
        public void RefreshDropPreview(bool canDrop)
        {
            dropPreview = canDrop;
            RefreshChosenIcon();
        }

        /// <summary>
        /// 清理当前槽位的拖拽放置预览状态。
        /// </summary>
        public void ClearDropPreview()
        {
            RefreshDropPreview(false);
        }

        /// <summary>
        /// 清空槽位显示。
        /// </summary>
        public void Clear()
        {
            dropPreview = false;
            Refresh(InventorySlotViewData.Empty(slotIndex), false);
        }
        #endregion

        #region Unity Events
        /// <summary>
        /// 处理槽位开始拖拽事件。
        /// </summary>
        /// <param name="eventData">Unity 指针事件数据。</param>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!hasItem) return;

            eventModule?.TriggerDragStarted(new InventorySlotDragEventArgs(slotIndex, eventData.position));
        }

        /// <summary>
        /// 维持 Unity EventSystem 的拖拽目标识别，拖拽中表现由 Coordinator 自行更新。
        /// </summary>
        /// <param name="eventData">Unity 指针事件数据。</param>
        public void OnDrag(PointerEventData eventData)
        {
        }

        /// <summary>
        /// 处理槽位拖拽结束事件。
        /// </summary>
        /// <param name="eventData">Unity 指针事件数据。</param>
        public void OnEndDrag(PointerEventData eventData)
        {
            eventModule?.TriggerDragEnded(new InventorySlotDragEventArgs(slotIndex, eventData.position));
        }

        /// <summary>
        /// 处理其他槽位释放到当前槽位的事件。
        /// </summary>
        /// <param name="eventData">Unity 指针事件数据。</param>
        public void OnDrop(PointerEventData eventData)
        {
            eventModule?.TriggerDropped(new InventorySlotDropEventArgs(slotIndex, eventData.position));
        }

        /// <summary>
        /// 处理拖拽指针进入当前槽位的事件。
        /// </summary>
        /// <param name="eventData">Unity 指针事件数据。</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            Vector2 targetScreenSize = GetSelfScreenSize(eventData);
            eventModule?.TriggerDragEntered(new InventorySlotDragEventArgs(slotIndex, eventData.position, targetScreenSize));
        }

        /// <summary>
        /// 处理拖拽指针离开当前槽位的事件。
        /// </summary>
        /// <param name="eventData">Unity 指针事件数据。</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            eventModule?.TriggerDragExited(new InventorySlotDragEventArgs(slotIndex, eventData.position));
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnSlotClicked);
        }
        #endregion

        #region Input
        private void OnSlotClicked()
        {
            eventModule?.TriggerClicked(slotIndex);
        }
        #endregion

        #region Tools
        private void RefreshChosenIcon()
        {
            if (chosenIcon != null) chosenIcon.SetActive(selected || dropPreview);
        }

        private Vector2 GetSelfScreenSize(PointerEventData eventData)
        {
            Camera eventCamera = GetEventCamera(eventData);
            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform == null) return Vector2.zero;

            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
            Vector2 max = min;
            for (int i = 1; i < corners.Length; i++)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[i]);
                min = Vector2.Min(min, screenPoint);
                max = Vector2.Max(max, screenPoint);
            }

            return max - min;
        }

        private Camera GetEventCamera(PointerEventData eventData)
        {
            if (eventData?.enterEventCamera != null) return eventData.enterEventCamera;
            if (eventData?.pressEventCamera != null) return eventData.pressEventCamera;

            return GetUICamera();
        }

        private Camera GetUICamera()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;

            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }
        #endregion
    }
}
