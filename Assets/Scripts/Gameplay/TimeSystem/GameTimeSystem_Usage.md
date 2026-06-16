# GameTimeSystem 使用与扩展说明

这份文档说明 `Gameplay.TimeSystem` 的使用方式，以及后续作物、生物、机器等对象如何接入游戏时间。

## 核心职责

`GameTimeManager` 负责把真实时间推进成游戏时间，并提供三类能力：

- 当前时间状态：`CurrentTime`
- 时间变化事件：分钟、小时、天、月、季节、年份、倍率变化
- 游戏分钟调度：经过若干游戏分钟后执行回调

业务对象不应该直接依赖 `TimeWheelScheduler`。如果对象需要“经过 N 个游戏分钟后变化”，应该通过 `GameTimeManager` 的公开 API 注册任务。

## 当前时间

当前时间通过 `CurrentTime` 暴露：

```csharp
GameTimeData time = GameTimeManager.Instance.CurrentTime.Value;
```

UI 或其他需要立即刷新显示的模块可以使用：

```csharp
IUnRegister unRegister = GameTimeManager.Instance.CurrentTime.RegisterWithInitValue(OnTimeChanged);

void OnTimeChanged(GameTimeData time)
{
    Debug.Log(time.ToString());
}
```

`RegisterWithInitValue` 会在注册时立即回调一次当前值，适合 UI 初始刷新。

## 时间事件

`GameTimeManager` 提供显式注册方法：

```csharp
GameTimeManager.Instance.RegisterMinuteChanged(OnMinuteChanged);
GameTimeManager.Instance.RegisterHourChanged(OnHourChanged);
GameTimeManager.Instance.RegisterDayStarted(OnDayStarted);
GameTimeManager.Instance.RegisterMonthChanged(OnMonthChanged);
GameTimeManager.Instance.RegisterSeasonChanged(OnSeasonChanged);
GameTimeManager.Instance.RegisterYearChanged(OnYearChanged);
GameTimeManager.Instance.RegisterTimeScaleChanged(OnTimeScaleChanged);
```

事件参数包含变化前后的时间：

```csharp
void OnMinuteChanged(GameTimeChangedEventArgs args)
{
    GameTimeData previous = args.Previous;
    GameTimeData current = args.Current;
}
```

使用原则：

- UI 显示当前时间：优先使用 `CurrentTime.RegisterWithInitValue`。
- 关心跨小时、跨天、跨月等边界：使用对应事件。
- 作物、生物、机器等阶段变化：优先使用游戏分钟调度，而不是每分钟事件。

## 游戏分钟调度

如果业务对象需要在若干游戏分钟后执行一次：

```csharp
TimeWheelHandle handle = GameTimeManager.Instance.ScheduleAfterMinutes(
    minutes: 120,
    callback: OnFinished);
```

如果需要取消：

```csharp
GameTimeManager.Instance.CancelScheduledTask(handle);
```

如果需要重复执行：

```csharp
TimeWheelHandle handle = GameTimeManager.Instance.ScheduleRepeatMinutes(
    intervalMinutes: 60,
    callback: OnHourlyTick,
    repeatCount: -1);
```

`TimeWheelHandle` 只用于运行时取消，不应该写入存档。

## IGameTimeProgressable

`IGameTimeProgressable` 表示一个对象可以根据游戏时间推进自身状态。

```csharp
public interface IGameTimeProgressable
{
    GameTimeManager GameTimeManager { get; set; }

    void StartGameTimeProgress();
    void StopGameTimeProgress();
    void RestoreGameTimeProgress(GameTimeData currentTime);
}
```

适用对象：

- 作物：经过若干游戏分钟进入下一阶段
- 生物：经过若干游戏天/分钟成长、老化或改变状态
- 机器：经过若干游戏分钟完成加工
- 刷新点：经过若干游戏时间刷新资源

接口只定义“接入游戏时间”的能力，不定义作物阶段、动物阶段或机器状态。这些业务规则应该由具体对象自己管理。

## IGameTimeProgressState

`IGameTimeProgressState` 用于描述可存档的时间进度状态：

```csharp
public interface IGameTimeProgressState
{
    bool HasPendingProgress { get; }
    long NextProgressTotalMinutes { get; }
}
```

推荐存档：

- 当前阶段
- 下一次变化发生时的累计游戏分钟：`NextProgressTotalMinutes`
- 是否还有等待中的变化：`HasPendingProgress`

不推荐存档：

- `TimeWheelHandle`
- `TimeWheelScheduler` 内部状态
- 回调委托

## 示例：作物成长

下面是一个简化示例，用于说明接口使用方式。实际作物可以把阶段、时长、季节限制等数据放到自己的物种配置中。

```csharp
using Gameplay.TimeSystem;
using WS_Modules.Utilities;

public sealed class CropGrowthRuntime : IGameTimeProgressable, IGameTimeProgressState
{
    private TimeWheelHandle progressHandle;
    private int stage;

    public GameTimeManager GameTimeManager { get; set; }

    public bool HasPendingProgress { get; private set; }
    public long NextProgressTotalMinutes { get; private set; }

    public void StartGameTimeProgress()
    {
        StopGameTimeProgress();

        int minutesToNextStage = GetMinutesToNextStage();
        if (minutesToNextStage <= 0)
        {
            return;
        }

        GameTimeData currentTime = GameTimeManager.CurrentTime.Value;
        NextProgressTotalMinutes = currentTime.TotalMinutes + minutesToNextStage;
        HasPendingProgress = true;

        progressHandle = GameTimeManager.ScheduleAfterMinutes(minutesToNextStage, OnProgressDue);
    }

    public void StopGameTimeProgress()
    {
        if (progressHandle.IsValid && GameTimeManager != null)
        {
            GameTimeManager.CancelScheduledTask(progressHandle);
        }

        progressHandle = default;
        HasPendingProgress = false;
    }

    public void RestoreGameTimeProgress(GameTimeData currentTime)
    {
        StopGameTimeProgress();

        if (!HasPendingProgress)
        {
            return;
        }

        long remainingMinutes = NextProgressTotalMinutes - currentTime.TotalMinutes;
        if (remainingMinutes <= 0)
        {
            OnProgressDue();
            return;
        }

        progressHandle = GameTimeManager.ScheduleAfterMinutes((int)remainingMinutes, OnProgressDue);
    }

    private void OnProgressDue()
    {
        progressHandle = default;
        HasPendingProgress = false;

        stage++;

        if (!IsFinalStage())
        {
            StartGameTimeProgress();
        }
    }

    private int GetMinutesToNextStage()
    {
        return 120;
    }

    private bool IsFinalStage()
    {
        return stage >= 3;
    }
}
```

## 读档恢复注意点

读档时不要尝试恢复旧的 `TimeWheelHandle`。正确流程是：

1. 从存档恢复对象阶段和 `NextProgressTotalMinutes`。
2. 获取当前游戏时间 `GameTimeManager.Instance.CurrentTime.Value`。
3. 调用对象的 `RestoreGameTimeProgress(currentTime)`。
4. 对象内部计算剩余时间并重新注册调度。

如果读档时已经超过目标时间，对象可以立即结算一次；如果一次结算后仍然还有阶段变化，由对象继续安排下一次调度。

## 生命周期建议

对象创建或放置到世界时：

```csharp
progressable.GameTimeManager = GameTimeManager.Instance;
progressable.StartGameTimeProgress();
```

对象销毁、收割、移除或禁用时：

```csharp
progressable.StopGameTimeProgress();
```

对象读档恢复时：

```csharp
progressable.GameTimeManager = GameTimeManager.Instance;
progressable.RestoreGameTimeProgress(GameTimeManager.Instance.CurrentTime.Value);
```

## 常见误区

### 每个对象都监听 MinuteChanged

不推荐。大量对象每分钟都收到事件会造成不必要的开销。阶段变化类对象更适合使用 `ScheduleAfterMinutes`。

### 把 TimeWheelHandle 写入存档

不推荐。`TimeWheelHandle` 是运行时取消凭证，只在当前调度器生命周期内有效。

### 把阶段事件放进 GameTimeManager

不推荐。`GameTimeManager` 只负责时间本身。作物阶段、生物成长、机器完成等业务事件应该由各自系统定义和派发。
