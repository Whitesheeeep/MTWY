# 未来计划记录

## 背包扩容与未解锁格子显示策略

### 背景

当前背包 UI 准备采用 MVVM 架构处理，`InventoryManager` 负责统一管理 Bar 和 Bag 数据，ViewModel 分别面向 Bar 和 Bag 提供 UI 状态，View 只负责前端显示与输入转发。

后续可能加入购买扩容背包的功能，因此需要提前确定背包容量变化后 UI 槽位的显示策略。

### 结论

- 背包扩容采用“隐藏未解锁格子”的方向。
- 运行时只显示当前已解锁容量对应的格子。
- 购买扩容后，再根据新的容量增加显示槽位。
- View 不直接决定背包容量，只根据 ViewModel 提供的槽位数量创建或复用 prefab。

### 推荐数据流

```text
购买扩容
-> InventoryManager.ExpandBagCapacity
-> InventoryData 容量变化
-> BagSlotsChanged
-> InventoryBagViewModel.RefreshSlotsFromModel
-> InventoryBagView.SetVisibleSlotCount
-> View 动态显示新增槽位
```

### 暂不实现项

- 暂不显示未解锁格子。
- 暂不实现锁定格子显示状态。
- 暂不实现背包分页。
- 暂不实现虚拟列表或复杂对象池。
- 暂不实现购买扩容的具体业务逻辑。

### 未来触发条件

- 当需要接入购买扩容功能时，实现 `InventoryManager.ExpandBagCapacity` 或等价接口。
- 当需要展示未来容量或付费引导时，再为 `InventorySlotViewData` 增加锁定状态。
- 当背包容量明显变大并出现性能压力时，再考虑分页、虚拟列表或对象池方案。
