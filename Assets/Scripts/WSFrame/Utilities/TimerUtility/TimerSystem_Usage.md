# Timer System 使用说明

`TimerManager` 是一个全局计时器系统，用于替代简单延迟、循环 Tick、UI 倒计时、技能冷却等场景。当前实现按 Timer 类型分两条调度路径：

- 无 `OnUpdate` 的 Timer 进入最小堆，只在到期时处理。
- 设置了 `OnUpdate` 的 Timer 进入逐帧列表，用于进度回调。

业务层不直接持有 `Timer`，而是持有 `TimerHandle`。`TimerHandle` 内部通过 `TimerManager + TimerId` 校验有效性，Timer 被回收后旧 Handle 不会误操作新 Timer。

## 快速开始

### 一次性延迟

```csharp
TimerManager.Register(2f, () =>
{
    Debug.Log("2 秒后执行");
});
```

### 循环指定次数

`SetLoop(count)` 表示完成回调总共触发 `count` 次。每次计时结束触发一次 `onComplete`，直到次数耗尽后自动回收。

```csharp
TimerManager.Register(1f, () => Debug.Log("Tick"))
    .SetLoop(3); // 第 1、2、3 秒各触发一次，然后结束
```

无限循环使用 `-1`：

```csharp
TimerManager.Register(1f, () => Debug.Log("Forever"))
    .SetLoop(-1);
```

### 进度回调

设置 `OnUpdate` 后 Timer 会进入逐帧更新列表，`progress` 范围是 `0 ~ 1`。

```csharp
TimerManager.Register(3f, () => Debug.Log("Done"))
    .OnUpdate(progress =>
    {
        progressBar.fillAmount = progress;
    });
```

### 不受 Time.timeScale 影响

UI 动画、加载提示、暂停菜单倒计时通常使用 unscaled time。

```csharp
TimerManager.Register(0.5f, OnAnimComplete)
    .SetUnscaledTime(true);
```

### 局部时间缩放

`SetTimeScale` 只影响当前 Timer，不修改 Unity 的 `Time.timeScale`。

```csharp
TimerManager.Register(5f, () => Debug.Log("Fast"))
    .SetTimeScale(2f); // 以 2 倍速度推进

TimerManager.Register(5f, () => Debug.Log("Slow"))
    .SetTimeScale(0.5f); // 以 0.5 倍速度推进
```

`SetTimeScale(0f)` 会冻结该 Timer 的推进，后续设回大于 0 的值会继续。

## TimerHandle

`Register` 返回 `TimerHandle`，可以链式配置，也可以保存起来做后续控制。

```csharp
private TimerHandle _cooldown;

private void StartCooldown()
{
    _cooldown = TimerManager.Register(5f, OnCooldownFinished)
        .SetTag(TimerManager.TimerTags.Test)
        .OnUpdate(progress => Debug.Log($"CD: {progress:P0}"));
}

private void PauseCooldown()
{
    if (_cooldown.IsValid)
    {
        _cooldown.Pause();
    }
}
```

常用属性：

- `IsValid`：Timer 是否仍由 Manager 管理。
- `Duration`：总时长。
- `TimeElapsed`：当前已推进时间。
- `TimeRemaining`：剩余时间。
- `Progress`：当前进度，范围 `0 ~ 1`。

注意：`Progress` 只是查询属性，不会让 Timer 主动逐帧通知外界。没有设置 `OnUpdate` 的 Timer 会走最小堆调度，只在到期时执行完成回调；如果要驱动进度条、冷却 UI 或其它实时显示，应注册时设置 `OnUpdate`。

常用控制：

- `Pause()` / `Resume()`
- `Cancel()`
- `ResetTime()` / `ResetTime(newDuration)`
- `SetTag(tag)`
- `SetLoop(count)`
- `SetUnscaledTime(true)`
- `SetTimeScale(scale)`
- `OnUpdate(callback)`

## Tag 批量控制

`TimerTags` 使用 `[Flags]`，一个 Timer 可以拥有多个标签。

```csharp
TimerManager.Register(10f, OnComplete)
    .SetTag(TimerManager.TimerTags.Test);
```

批量操作：

```csharp
TimerManager.PauseByTag(TimerManager.TimerTags.Test);
TimerManager.ResumeByTag(TimerManager.TimerTags.Test);
TimerManager.CancelByTag(TimerManager.TimerTags.Test);
TimerManager.SetTimeScaleByTag(TimerManager.TimerTags.Test, 0.5f);
```

当前项目内置标签：

```csharp
[Flags]
public enum TimerTags
{
    None = 0,
    Test = 1 << 0,
    All = ~0
}
```

需要更多业务标签时，可以继续扩展 `TimerTags`，例如 `UI`、`Battle`、`Buff`、`Cooldown`。

## 调度模式

内部 `TimerScheduleMode` 用于描述 Timer 当前在哪条调度路径上：

- `Pending`：刚注册后暂存一帧，等待链式配置完成。
- `UpdateList`：设置了 `OnUpdate`，逐帧推进。
- `HeapScaled`：无 `OnUpdate`，使用 `Time.time` 的最小堆。
- `HeapUnscaled`：无 `OnUpdate`，使用 `Time.unscaledTime` 的最小堆。
- `Paused`：暂停，不在调度容器中。
- `Executing`：正在执行完成回调。
- `Detached`：容器迁移中的临时状态。
- `Recycled`：已回收。

## 注意事项

1. `OnUpdate` 会让 Timer 进入逐帧列表，因此只在确实需要进度时使用。
2. 回调中可以 `Cancel`、`ResetTime`、`Pause`，系统会根据当前调度状态处理。
3. 场景切换时 `TimerManager` 已订阅 `SceneSystem.OnLoadSceneStart` 调用 `CancelAll`，避免旧场景回调继续执行。
4. 如果业务对象销毁时仍持有 `TimerHandle`，建议主动调用 `Cancel()`。
5. `TimerHandle` 是值类型，可以保存；但不要假设旧 Handle 永远有效，操作前可检查 `IsValid`。
