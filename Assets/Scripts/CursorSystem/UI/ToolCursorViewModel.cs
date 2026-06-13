using System;
using CursorSystem;
using UnityEngine;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 工具鼠标指针 ViewModel，把 CursorManager 状态投影成 UI 可显示数据。
    /// </summary>
    public sealed class ToolCursorViewModel : IDisposable
    {
        private readonly CursorManager cursorManager;

        public ToolCursorViewModel(CursorManager cursorManager)
        {
            this.cursorManager = cursorManager ?? throw new ArgumentNullException(nameof(cursorManager));
            this.cursorManager.CursorChanged += HandleCursorChanged;
        }

        public event Action Changed;

        public Sprite Icon => cursorManager.CurrentState.Icon;
        public CursorVisualState VisualState => cursorManager.CurrentState.VisualState;
        public bool Visible => Icon != null;

        public void Dispose()
        {
            cursorManager.CursorChanged -= HandleCursorChanged;
        }

        private void HandleCursorChanged()
        {
            Changed?.Invoke();
        }
    }
}
