using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 背包槽位 View 组件，负责刷新图标、数量、选中状态和拖拽事件转发。
    /// </summary>
    public sealed class InventorySlotView : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IDropHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TMP_Text itemCount;
        [SerializeField] private GameObject chosenIcon;

        private int slotIndex;
        private InventorySlotArea slotArea = InventorySlotArea.None;
        private Action<int> onSlotClicked;
        private Action<InventorySlotDragEventArgs> onSlotDragStarted;
        private Action<InventorySlotDragEventArgs> onSlotDragging;
        private Action<InventorySlotDragEventArgs> onSlotDragEnded;
        private Action<InventorySlotDragEventArgs> onSlotDragEntered;
        private Action<InventorySlotDragEventArgs> onSlotDragExited;
        private Action<InventorySlotDropEventArgs> onSlotDropped;
        private bool selected;
        private bool dropPreview;
        private bool hasItem;

        /// <summary>
        /// 初始化槽位点击回调。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <param name="slotClickedCallback">槽位被点击时触发的回调。</param>
        public void Initialize(int index, Action<int> slotClickedCallback)
        {
            Initialize(index, InventorySlotArea.None, slotClickedCallback, null, null, null, null, null, null);
        }

        /// <summary>
        /// 初始化槽位点击与拖拽回调。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <param name="area">槽位所属区域。</param>
        /// <param name="slotClickedCallback">槽位被点击时触发的回调。</param>
        /// <param name="slotDragStartedCallback">槽位开始拖拽时触发的回调。</param>
        /// <param name="slotDraggingCallback">槽位拖拽过程中触发的回调。</param>
        /// <param name="slotDragEndedCallback">槽位拖拽结束时触发的回调。</param>
        /// <param name="slotDragEnteredCallback">拖拽进入当前槽位时触发的回调。</param>
        /// <param name="slotDragExitedCallback">拖拽离开当前槽位时触发的回调。</param>
        /// <param name="slotDroppedCallback">拖拽释放到当前槽位时触发的回调。</param>
        public void Initialize(
            int index,
            InventorySlotArea area,
            Action<int> slotClickedCallback,
            Action<InventorySlotDragEventArgs> slotDragStartedCallback,
            Action<InventorySlotDragEventArgs> slotDraggingCallback,
            Action<InventorySlotDragEventArgs> slotDragEndedCallback,
            Action<InventorySlotDragEventArgs> slotDragEnteredCallback,
            Action<InventorySlotDragEventArgs> slotDragExitedCallback,
            Action<InventorySlotDropEventArgs> slotDroppedCallback)
        {
            slotIndex = index;
            slotArea = area;
            onSlotClicked = slotClickedCallback;
            onSlotDragStarted = slotDragStartedCallback;
            onSlotDragging = slotDraggingCallback;
            onSlotDragEnded = slotDragEndedCallback;
            onSlotDragEntered = slotDragEnteredCallback;
            onSlotDragExited = slotDragExitedCallback;
            onSlotDropped = slotDroppedCallback;

            EnsureReferences();
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
            EnsureReferences();

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
            EnsureReferences();
            this.selected = selected;
            RefreshChosenIcon();
        }

        /// <summary>
        /// 刷新当前槽位的拖拽放置预览状态。
        /// </summary>
        /// <param name="canDrop">是否显示为可放置。</param>
        public void RefreshDropPreview(bool canDrop)
        {
            EnsureReferences();
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

        /// <summary>
        /// 处理槽位开始拖拽事件。
        /// </summary>
        /// <param name="eventData">Unity 指针事件数据。</param>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (slotArea == InventorySlotArea.None || !hasItem) return;

            InventorySlotDragState.BeginDrag(slotArea, slotIndex);
            onSlotDragStarted?.Invoke(new InventorySlotDragEventArgs(slotArea, slotIndex, eventData.position));
        }

        /// <summary>
        /// 处理槽位拖拽过程事件。
        /// </summary>
        /// <param name="eventData">Unity 指针事件数据。</param>
        public void OnDrag(PointerEventData eventData)
        {
            if (!InventorySlotDragState.HasActiveDrag || slotArea == InventorySlotArea.None) return;

            onSlotDragging?.Invoke(new InventorySlotDragEventArgs(slotArea, slotIndex, eventData.position));
        }

        /// <summary>
        /// 处理槽位拖拽结束事件。
        /// </summary>
        /// <param name="eventData">Unity 指针事件数据。</param>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!InventorySlotDragState.HasActiveDrag || slotArea == InventorySlotArea.None) return;

            onSlotDragEnded?.Invoke(new InventorySlotDragEventArgs(slotArea, slotIndex, eventData.position));
        }

        /// <summary>
        /// 处理其他槽位释放到当前槽位的事件。
        /// </summary>
        /// <param name="eventData">Unity 指针事件数据。</param>
        public void OnDrop(PointerEventData eventData)
        {
            if (!InventorySlotDragState.HasActiveDrag || slotArea == InventorySlotArea.None) return;

            onSlotDropped?.Invoke(new InventorySlotDropEventArgs(
                InventorySlotDragState.SourceArea,
                InventorySlotDragState.SourceIndex,
                slotArea,
                slotIndex,
                eventData.position));
        }

        /// <summary>
        /// 处理拖拽指针进入当前槽位的事件。
        /// </summary>
        /// <param name="eventData">Unity 指针事件数据。</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!InventorySlotDragState.HasActiveDrag || slotArea == InventorySlotArea.None) return;

            onSlotDragEntered?.Invoke(new InventorySlotDragEventArgs(slotArea, slotIndex, eventData.position));
        }

        /// <summary>
        /// 处理拖拽指针离开当前槽位的事件。
        /// </summary>
        /// <param name="eventData">Unity 指针事件数据。</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!InventorySlotDragState.HasActiveDrag || slotArea == InventorySlotArea.None) return;

            onSlotDragExited?.Invoke(new InventorySlotDragEventArgs(slotArea, slotIndex, eventData.position));
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnSlotClicked);
        }

        private void OnSlotClicked()
        {
            onSlotClicked?.Invoke(slotIndex);
        }

        private void EnsureReferences()
        {
            button ??= GetComponent<Button>();
            itemIcon ??= transform.Find("ItemIcon")?.GetComponent<Image>();
            itemCount ??= transform.Find("ItemCount")?.GetComponent<TMP_Text>();
            chosenIcon ??= transform.Find("ChosenIcon")?.gameObject;
        }

        private void RefreshChosenIcon()
        {
            if (chosenIcon != null) chosenIcon.SetActive(selected || dropPreview);
        }
    }
}
