using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 快捷栏单个槽位的 View 组件，负责刷新图标、数量和选中状态。
    /// </summary>
    public sealed class InventoryBarSlotView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TMP_Text itemCount;
        [SerializeField] private GameObject chosenIcon;

        private int slotIndex;
        private Action<int> onSlotClicked;

        /// <summary>
        /// 初始化槽位点击回调。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <param name="slotClickedCallback">槽位被点击时触发的回调。</param>
        public void Initialize(int index, Action<int> slotClickedCallback)
        {
            slotIndex = index;
            onSlotClicked = slotClickedCallback;

            EnsureReferences();
            if (button == null)
            {
                button = gameObject.AddComponent<Button>();
            }

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

            bool hasItem = !data.IsEmpty;
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
            if (chosenIcon != null)
            {
                chosenIcon.SetActive(selected);
            }
        }

        /// <summary>
        /// 清空槽位显示。
        /// </summary>
        public void Clear()
        {
            Refresh(InventorySlotViewData.Empty(slotIndex), false);
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnSlotClicked);
            }
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
    }
}
