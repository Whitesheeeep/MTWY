using System;
using CursorSystem;
using GameData;
using UnityEngine;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 工具鼠标指针 ViewModel，把 CursorManager 状态投影成 UI 可显示数据。
    /// </summary>
    public sealed class ToolCursorViewModel : IDisposable
    {
        private readonly CursorManager cursorManager;

        /// <summary>
        /// 创建工具鼠标指针 ViewModel 并订阅 CursorManager 状态变化。
        /// </summary>
        public ToolCursorViewModel(CursorManager cursorManager)
        {
            this.cursorManager = cursorManager ?? throw new ArgumentNullException(nameof(cursorManager));
            this.cursorManager.CursorChanged += HandleCursorChanged;
        }

        /// <summary>
        /// 当指针显示数据变化时派发。
        /// </summary>
        public event Action Changed;

        /// <summary>
        /// 当前自定义指针要显示的图标。
        /// </summary>
        public Sprite Icon => cursorManager.CurrentState.Icon;

        /// <summary>
        /// 当前指针交互视觉状态。
        /// </summary>
        public CursorVisualState VisualState => cursorManager.CurrentState.VisualState;

        /// <summary>
        /// 当前是否应该显示选中物品的自定义指针图标。
        /// </summary>
        public bool Visible
        {
            get
            {
                ItemData selectedItemData = cursorManager.CurrentState.SelectedItemData;
                return selectedItemData != null && ToolTypeUtility.IsTool(selectedItemData.itemType) && Icon != null;
            }
        }

        /// <summary>
        /// 释放 ViewModel 并取消 CursorManager 订阅。
        /// </summary>
        public void Dispose()
        {
            cursorManager.CursorChanged -= HandleCursorChanged;
        }

        // Cursor 状态变化时通知 View 重新读取显示数据。
        private void HandleCursorChanged()
        {
            Changed?.Invoke();
        }
    }
}