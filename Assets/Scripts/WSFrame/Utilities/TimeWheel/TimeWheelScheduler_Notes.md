# TimeWheelScheduler 理解笔记

这份文档只解释当前 `TimeWheelScheduler` 的实现思路，方便以后回查。重点不是 API 用法，而是为什么代码会这样写。

## 1. 时间轮看的是同一条时间轴

多级时间轮不是把时间拆成：

- `level 0` 管前一段
- `level 1` 管后一段
- `level 2` 再管更后一段

当前实现不是这种“前后串接”关系，而是“同一条绝对时间轴的不同精度映射”。

也就是说：

- `level 0` 用最细粒度看时间
- `level 1` 用更粗粒度看同一条时间轴
- `level 2` 再用更粗粒度看同一条时间轴

高层不是低层后面的时间段，而是低层时间轴的粗粒度覆盖。

## 2. `slotCount`、`span`、`capacity` 分别是什么

### `slotCount`

某一层有多少个槽位。

例如：

```csharp
slotCounts = [256, 64, 64]
```

表示：

- `level 0` 有 256 个槽位
- `level 1` 有 64 个槽位
- `level 2` 有 64 个槽位

### `span`

某一层的每一个槽位，覆盖多少个基础 tick。

公式：

```text
span[0] = 1
span[level] = span[level - 1] * slotCount[level - 1]
```

等价于：

```text
span[level] = 前面所有层 slotCount 的乘积
```

例如：

```csharp
slotCounts = [256, 64, 64]
```

则：

- `span[0] = 1`
- `span[1] = 256`
- `span[2] = 256 * 64 = 16384`

含义是：

- `level 0` 每格 1 tick
- `level 1` 每格 256 tick
- `level 2` 每格 16384 tick

### `capacity`

某一层一整圈能覆盖多少个基础 tick。

公式：

```text
capacity[level] = span[level] * slotCount[level]
```

例如：

```csharp
slotCounts = [256, 64, 64]
```

则：

- `capacity[0] = 1 * 256 = 256`
- `capacity[1] = 256 * 64 = 16384`
- `capacity[2] = 16384 * 64 = 1048576`

可以把它理解成“这一层单独绕一圈能表示的时间范围”。

## 3. `_currentTick` 为什么一直递增

`_currentTick` 表示：

**从调度器启动到现在，已经走过了多少个基础 tick。**

它是绝对时间坐标，不是底层槽位下标。

因此：

- `_currentTick` 应该单调递增
- 底层槽位通过取模回绕
- 不能在底层转一圈后把 `_currentTick` 清零

原因有三个：

### 3.1 任务到期时间是绝对 tick

任务通常记录：

```text
dueTick = currentTick + delayTicks
```

如果 `_currentTick` 周期性清零，那么 `dueTick` 比较会变得混乱，需要额外处理跨圈逻辑。

### 3.2 高层推进依赖绝对边界

当前实现用：

```csharp
_currentTick % _levelSpans[level] == 0
```

来判断某一层是否走到了应该推进的边界。

比如 `level 1` 的 `span = 256`，那它就应该在：

- `256`
- `512`
- `768`

这些绝对 tick 上推进。

如果 `_currentTick` 被重置，高层将无法正确判断这些边界。

### 3.3 `pause / resume` 更稳定

暂停时可以直接算：

```text
remainingTicks = dueTick - currentTick
```

因为两者都在同一条绝对时间轴上，语义清晰。

## 4. `_currentTick` 和 `slot` 不是一回事

要区分：

- `_currentTick`：绝对时间
- `slot`：某一层环形数组里的槽位索引

例如 `level 0` 有 256 个槽位时：

```csharp
slot = _currentTick % 256
```

当：

- `_currentTick = 255` 时，`slot = 255`
- `_currentTick = 256` 时，`slot = 0`

回绕的是 `slot`，不是 `_currentTick`。

## 5. `GetSlot(level, tick)` 为什么先除再取模

当前代码：

```csharp
return (int)((tick / _levelSpans[level]) % _config.GetSlotCount(level));
```

这句分两步理解：

### 5.1 `tick / span`

先把绝对 tick 换算成“这一层的逻辑格号”。

例如：

```csharp
slotCounts = [256, 64, 64]
tick = 300
level = 1
span = 256
```

那么：

```csharp
300 / 256 = 1
```

表示：

`tick = 300` 落在 `level 1` 的第 1 个逻辑格子里。

因为 `level 1` 的格子区间是：

- 第 0 格：`[0, 255]`
- 第 1 格：`[256, 511]`
- 第 2 格：`[512, 767]`

所以 `300` 确实属于第 1 格。

### 5.2 `% slotCount`

逻辑格号会不断增大，但数组槽位是环形的，所以还要映射回实际数组下标。

例如 `level 1` 有 64 个槽位：

```csharp
(tick / span) % 64
```

如果逻辑格号增长到 70，那么实际槽位就是：

```csharp
70 % 64 = 6
```

所以：

- `/ _levelSpans[level]` 是做层级尺度转换
- `% slotCount` 是做环形槽位映射

## 6. 为什么 `tick = 300` 在 `level 1` 是 `slot 1`

这是当前实现最容易误解的地方。

如果：

```csharp
slotCounts = [256, 64, 64]
```

则 `level 1` 每格 256 tick。

它的格子区间是：

- `slot 0` 对应绝对区间 `[0, 255]`
- `slot 1` 对应绝对区间 `[256, 511]`
- `slot 2` 对应绝对区间 `[512, 767]`

因此：

```text
tick = 300
```

必须落在 `slot 1`，而不是 `slot 0`。

如果觉得它像是应该放在 `slot 0`，通常是把问题想成了“距离现在还有多少时间”，也就是相对时间分桶。

但当前实现用的是：

- 绝对 `dueTick`
- 绝对时间区间分桶
- 高层在边界时 cascade 到低层

所以这里不是按“还剩几轮”，而是按“绝对时间落在哪个区间”来放桶。

## 7. `GetSlot` 不负责选层

`GetSlot(level, tick)` 只回答一个问题：

**已知任务要放在 `level` 这一层，那么它应该落到哪个槽位。**

它不负责回答：

**这个任务到底应该进哪一层。**

当前实现是先选层，再算槽位：

1. `Insert(task)` 根据 `dueTick - currentTick` 判断该进哪一层
2. `GetSlot(level, dueTick)` 根据这一层的刻度算槽位

所以不要把“层选择”和“槽位计算”混成一件事。

## 8. `CascadeDueLevels()` 为什么是 `% _levelSpans[level]`

代码里有这样的判断：

```csharp
if (_currentTick % _levelSpans[level] != 0)
{
    break;
}
```

它的意思不是“算这个层的槽位”，而是：

**判断当前绝对 tick 是否走到了该层应该推进的边界。**

例如：

- `level 0` 的 `span = 1`，每个 tick 都推进
- `level 1` 的 `span = 256`，每 256 个基础 tick 推进一次
- `level 2` 的 `span = 16384`，每 16384 个基础 tick 推进一次

所以：

```csharp
_currentTick % _levelSpans[level] == 0
```

表示：

“当前时刻刚好落在这一层的大格边界上，这一层现在可以 cascade 了。”

这和 `GetSlot()` 里的取模不是同一种用途：

- `% _levelSpans[level]`：判断这一层是否该推进
- `% slotCount`：把逻辑格号映射到环形数组下标

## 9. 一个完整的小例子

假设：

```csharp
slotCounts = [4, 4, 4]
```

则：

- `span[0] = 1`
- `span[1] = 4`
- `span[2] = 16`

表示：

- `level 0` 每格 1 tick
- `level 1` 每格 4 tick
- `level 2` 每格 16 tick

如果任务的：

```text
dueTick = 10
```

那么：

- 在 `level 0` 看，它是第 10 个基础 tick
- 在 `level 1` 看，它属于第 `10 / 4 = 2` 个逻辑格
- 在 `level 2` 看，它属于第 `10 / 16 = 0` 个逻辑格

如果此时任务因为比较远而先被放到高层，那么后续随着 `_currentTick` 推进，到达对应层级边界时，它会被重新分发到更低层，直到最终落到 `level 0`，并在 `dueTick = 10` 时触发。

这说明：

- 高层只是粗粒度暂存
- 低层负责最终精确触发
- 各层始终映射的是同一个绝对到期时间

## 10. 常见误区

### 误区 1：高层是低层后面的时间段

不是。高层和低层覆盖的是同一条时间轴，只是粒度不同。

### 误区 2：`_currentTick` 应该每圈清零

不是。清零会破坏绝对时间语义，让 `dueTick` 比较、层级边界判断和暂停恢复都变复杂。

### 误区 3：`GetSlot()` 里的 `/ span` 表示所有任务都走这个层

不是。是否走某层由 `Insert()` 决定。`GetSlot()` 只是对“已经选中这一层”的任务计算槽位。

### 误区 4：`tick = 300` 在 `level 1` 应该进 `slot 0`

不是。当前实现按绝对时间区间分桶，`300` 属于 `[256, 511]`，因此在 `slot 1`。

### 误区 5：`% _levelSpans[level]` 是在算槽位

不是。它是在判断这一层是否到达推进边界。真正的槽位计算是：

```csharp
(tick / span) % slotCount
```
