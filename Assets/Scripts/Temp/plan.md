# Inventory View / Drag 通用化修订计划

## Summary

- 移除 `MoveToBag / MoveToBar` 等专用窗口 API。
- 拖拽系统改为基于 `containerId` 的通用容器间操作。
- 拖拽过程中通过鼠标 UI Raycast 判断当前指针下的目标容器，避免所有窗口同时接收拖拽指令。

## Key Changes

- ViewModel API 调整：

  - `InventoryBarViewModel` 移除 `MoveToBag`。
  - `InventoryBagViewModel` 移除 `MoveToBar`。
  - `InventorySlotContainerViewModel` 保留/新增通用方法：
    - `MoveSlot(int fromIndex, int toIndex)`
    - `MoveSlotTo(InventorySlotContainerViewModel target, int fromIndex, int toIndex)`
    - `DropSlotToWorld(int index)`
  - 跨容器移动由通用 transfer service 或 drag coordinator 调度，不再写 Bar/Bag 专用分支。
- 拖拽目标识别：

  - 使用 `EventSystem.current.RaycastAll` 根据当前鼠标屏幕坐标获取 UI 命中结果。
  - 从命中结果中向父级查找：
    - `InventorySlotView`：表示当前在某个槽位上。
    - `IInventorySlotContainerView` 或容器绑定组件：表示当前在某个槽位容器窗口区域内。
  - 只把拖拽中事件发送给当前命中的最上层 Inventory 容器。
  - 若未命中任何 Inventory 容器，拖拽结束时视为拖出 UI，执行丢弃。
- Drag Coordinator：

  - 新增 `InventorySlotDragCoordinator` 统一管理拖拽流程。
  - Window 只注册自己的 `containerId + View + ViewModel`。
  - Coordinator 负责：
    - 打开/移动/隐藏 `DropWindow`
    - 查询当前鼠标下目标容器
    - 刷新目标槽位 Drop Preview
    - 调用同容器或跨容器移动
    - 拖出 UI 时调用来源容器丢弃
    - 通知当前目标 Bag 容器执行边缘滚动
  - `GlobalUIWindow` / `BagWindow` 不再互相 `GetWindow` 调用来转发拖拽。
- View 绑定：

  - `InventorySlotView` 保存 `containerId` 和 `slotIndex`，拖拽事件只上报这两个值。
  - `InventoryBarView` / `InventoryBagView` 初始化时传入自己的 `containerId`。
  - `IInventorySlotContainerView` 增加必要的容器标识暴露，例如 `string ContainerId { get; }`，或通过绑定上下文传给 Coordinator。
- 初始化修复：

  - `InventoryManager` 增加 `IsInitialized` 和初始化完成通知。
  - Window 在 Manager 初始化完成后再绑定 ViewModel。
  - 避免 `OnAwake` 过早创建 ViewModel，导致拖拽回调没有绑定。

## Test Plan

- Bar 内拖拽移动、合并、交换正常。
- Bag 内拖拽移动、合并、交换正常。
- Bar ↔ Bag 拖拽正常，不再调用 `MoveToBag / MoveToBar`。
- 拖拽时只有鼠标下方的目标容器显示 Drop Preview。
- 鼠标在 Bag 边缘时只有 Bag 执行边缘滚动。
- 拖出所有 Inventory 容器后释放，执行整格丢弃到世界。
- `rg "MoveToBag|MoveToBar"` 无剩余生产调用。
- Window 之间不再互相转发拖拽事件。

## Assumptions

- 第一版目标识别使用 Unity UI Raycast，不额外维护窗口 Rect 列表。
- `DropWindow` 的 Image 保持 `raycastTarget = false`，避免挡住底下的目标槽位。
- 本轮仍只接入 Bar 和 Bag，但结构允许后续 Chest/Shop 通过注册容器接入。
