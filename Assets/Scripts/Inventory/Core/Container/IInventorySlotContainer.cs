using System;
using System.Collections.Generic;
using WS_Modules.CustomEventSystem;

namespace Inventory
{
    /// <summary>
    /// Inventory 槽位容器契约，用于描述单个可操作槽位集合的数据读写与变更通知能力。
    /// </summary>
    public interface IInventorySlotContainer
    {
        /// <summary>
        /// 容器容量。
        /// </summary>
        int Capacity { get; }

        /// <summary>
        /// 当前槽位数量。
        /// </summary>
        int SlotCount { get; }

        /// <summary>
        /// 获取指定槽位的快照。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <returns>槽位快照。</returns>
        InventorySlotData GetSlot(int index);

        /// <summary>
        /// 获取全部槽位快照。
        /// </summary>
        /// <returns>槽位快照列表。</returns>
        IReadOnlyList<InventorySlotData> GetSlotsSnapshot();

        /// <summary>
        /// 判断容器中是否存在指定物品。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <returns>存在至少一个该物品时返回 true。</returns>
        bool Contains(int itemId);

        /// <summary>
        /// 获取指定物品在容器中的总数量。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <returns>物品总数量。</returns>
        int GetCount(int itemId);

        /// <summary>
        /// 向容器中加入物品。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">加入数量。</param>
        /// <returns>未能放入容器的剩余数量。</returns>
        int AddItem(int itemId, int count);

        /// <summary>
        /// 尝试向容器中加入物品，只有全部放入时才提交数据变更。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">加入数量。</param>
        /// <returns>全部放入返回 true，空间不足返回 false。</returns>
        bool TryAddItem(int itemId, int count);

        /// <summary>
        /// 从容器中移除指定数量的物品。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">移除数量。</param>
        /// <returns>移除成功返回 true，数量不足返回 false。</returns>
        bool RemoveItem(int itemId, int count);

        /// <summary>
        /// 从指定槽位移除指定数量的物品。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <param name="count">移除数量。</param>
        /// <returns>移除成功返回 true。</returns>
        bool RemoveFromSlot(int index, int count);

        /// <summary>
        /// 设置指定物品在容器中的总数量。
        /// </summary>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">目标数量。</param>
        /// <returns>设置成功返回 true。</returns>
        bool SetCount(int itemId, int count);

        /// <summary>
        /// 设置指定槽位数据。
        /// </summary>
        /// <param name="index">槽位索引。</param>
        /// <param name="itemId">物品编号。</param>
        /// <param name="count">物品数量。</param>
        /// <returns>设置成功返回 true。</returns>
        bool SetSlot(int index, int itemId, int count);

        /// <summary>
        /// 移动容器内部槽位。
        /// </summary>
        /// <param name="fromIndex">来源槽位索引。</param>
        /// <param name="toIndex">目标槽位索引。</param>
        /// <returns>移动成功返回 true。</returns>
        bool MoveSlot(int fromIndex, int toIndex);

        /// <summary>
        /// 合并容器内部槽位。
        /// </summary>
        /// <param name="fromIndex">来源槽位索引。</param>
        /// <param name="toIndex">目标槽位索引。</param>
        /// <returns>合并成功返回 true。</returns>
        bool MergeSlots(int fromIndex, int toIndex);

        /// <summary>
        /// 从一个槽位拆分指定数量到另一个槽位。
        /// </summary>
        /// <param name="fromIndex">来源槽位索引。</param>
        /// <param name="count">拆分数量。</param>
        /// <param name="toIndex">目标槽位索引。</param>
        /// <returns>拆分成功返回 true。</returns>
        bool SplitSlot(int fromIndex, int count, int toIndex);

        /// <summary>
        /// 清空容器全部槽位。
        /// </summary>
        void Clear();

        /// <summary>
        /// 注册单个槽位变化事件。
        /// </summary>
        /// <param name="handler">事件处理函数。</param>
        /// <returns>注销句柄。</returns>
        IUnRegister RegisterSlotChanged(Action<InventorySlotChangedEventArgs> handler);

        /// <summary>
        /// 注册槽位列表整体变化事件。
        /// </summary>
        /// <param name="handler">事件处理函数。</param>
        /// <returns>注销句柄。</returns>
        IUnRegister RegisterSlotsChanged(Action<InventorySlotsChangedEventArgs> handler);
    }
}
