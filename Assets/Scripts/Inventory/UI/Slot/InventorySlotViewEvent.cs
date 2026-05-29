using System;
using WS_Modules.CustomEventSystem;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// Inventory 槽位 View 输入事件类型。
    /// </summary>
    public enum InventorySlotViewEventType
    {
        Clicked = 1,
        DragStarted = 2,
        DragEnded = 4,
        DragEntered = 5,
        DragExited = 6,
        Dropped = 7
    }

    /// <summary>
    /// Inventory 槽位点击事件参数。
    /// </summary>
    public readonly struct InventorySlotClickedEventArgs
    {
        /// <summary>
        /// 槽位索引。
        /// </summary>
        public readonly int Index;

        /// <summary>
        /// 创建槽位点击事件参数。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        public InventorySlotClickedEventArgs(int index)
        {
            Index = index;
        }
    }

    /// <summary>
    /// Inventory 槽位 View 局部输入事件模块。
    /// </summary>
    public sealed class InventorySlotViewEventModule
    {
        private readonly EventCenterModule<int> eventModule = new EventCenterModule<int>();

        /// <summary>
        /// 注册槽位点击事件。
        /// </summary>
        public IUnRegister RegisterClicked(Action<InventorySlotClickedEventArgs> handler)
        {
            return eventModule.Register((int)InventorySlotViewEventType.Clicked, handler);
        }

        /// <summary>
        /// 注册槽位开始拖拽事件。
        /// </summary>
        public IUnRegister RegisterDragStarted(Action<InventorySlotDragEventArgs> handler)
        {
            return eventModule.Register((int)InventorySlotViewEventType.DragStarted, handler);
        }

        /// <summary>
        /// 注册槽位拖拽结束事件。
        /// </summary>
        public IUnRegister RegisterDragEnded(Action<InventorySlotDragEventArgs> handler)
        {
            return eventModule.Register((int)InventorySlotViewEventType.DragEnded, handler);
        }

        /// <summary>
        /// 注册拖拽进入槽位事件。
        /// </summary>
        public IUnRegister RegisterDragEntered(Action<InventorySlotDragEventArgs> handler)
        {
            return eventModule.Register((int)InventorySlotViewEventType.DragEntered, handler);
        }

        /// <summary>
        /// 注册拖拽离开槽位事件。
        /// </summary>
        public IUnRegister RegisterDragExited(Action<InventorySlotDragEventArgs> handler)
        {
            return eventModule.Register((int)InventorySlotViewEventType.DragExited, handler);
        }

        /// <summary>
        /// 注册拖拽释放到槽位事件。
        /// </summary>
        public IUnRegister RegisterDropped(Action<InventorySlotDropEventArgs> handler)
        {
            return eventModule.Register((int)InventorySlotViewEventType.Dropped, handler);
        }

        /// <summary>
        /// 触发槽位点击事件。
        /// </summary>
        public void TriggerClicked(int index)
        {
            eventModule.EventTrigger((int)InventorySlotViewEventType.Clicked, new InventorySlotClickedEventArgs(index));
        }

        /// <summary>
        /// 触发槽位开始拖拽事件。
        /// </summary>
        public void TriggerDragStarted(InventorySlotDragEventArgs eventArgs)
        {
            eventModule.EventTrigger((int)InventorySlotViewEventType.DragStarted, eventArgs);
        }

        /// <summary>
        /// 触发槽位拖拽结束事件。
        /// </summary>
        public void TriggerDragEnded(InventorySlotDragEventArgs eventArgs)
        {
            eventModule.EventTrigger((int)InventorySlotViewEventType.DragEnded, eventArgs);
        }

        /// <summary>
        /// 触发拖拽进入槽位事件。
        /// </summary>
        public void TriggerDragEntered(InventorySlotDragEventArgs eventArgs)
        {
            eventModule.EventTrigger((int)InventorySlotViewEventType.DragEntered, eventArgs);
        }

        /// <summary>
        /// 触发拖拽离开槽位事件。
        /// </summary>
        public void TriggerDragExited(InventorySlotDragEventArgs eventArgs)
        {
            eventModule.EventTrigger((int)InventorySlotViewEventType.DragExited, eventArgs);
        }

        /// <summary>
        /// 触发拖拽释放到槽位事件。
        /// </summary>
        public void TriggerDropped(InventorySlotDropEventArgs eventArgs)
        {
            eventModule.EventTrigger((int)InventorySlotViewEventType.Dropped, eventArgs);
        }

        /// <summary>
        /// 清理全部槽位输入事件。
        /// </summary>
        public void Clear()
        {
            eventModule.Clear();
        }
    }
}
