# SceneTransitionSystem Guide

`SceneTransitionSystem` 是 `SceneSystem` 之上的轻量场景转换层，用于“角色进入 2D 触发器 -> 切换到目标场景 -> 移动到目标场景指定落点”的流程。

它不负责加载 UI、过场动画事件、进度事件或业务剧情事件。加载进度、成功、失败仍然通过 `SceneSystem.RegisterLoad...` 监听。

## 职责边界

`SceneTransitionSystem` 负责：

- 按 `routeId` 从运行时 Route 查找表解析 `SceneTransitionRoute`。
- 调用 `SceneSystem.LoadSceneAsync(route.TargetSceneName, LoadSceneMode.Single)` 加载目标场景。
- 在目标场景中查找 `SceneSpawnRoot`。
- 按 `TargetSpawnId` 找到落点 `Transform`。
- 移动 traveler 到落点位置，并在 Route 开启时应用落点旋转。
- 使用 `IsTransitioning` 防止重复转场。

`SceneTransitionSystem` 不负责：

- 不新增 `RegisterTransition...` 事件。
- 不绕过 `SceneSystem` 直接发布加载状态。
- 不扫描项目里的 `.unity` 场景资源。
- 不同步外部直接使用 `SceneManager` 做的场景状态。
- 不在运行时从 `SceneTransitionConfig` 做编辑器校验或刷新。

## 核心数据关系

```text
SceneTransitionTrigger2D
    stores routeId only
        |
        v
SceneTransitionSystem
    routeId -> SceneTransitionRoute
        |
        v
SceneSystem.LoadSceneAsync(targetSceneName, Single)
        |
        v
SceneSpawnRoot
    targetSpawnId -> Transform
```

三个核心对象的分工：

- `SceneTransitionConfig`：全局 Route 配置资产，描述“去哪个场景、去哪个落点”。
- `SceneTransitionTrigger2D`：场景中的触发器，只保存 `routeId`。
- `SceneSpawnRoot`：目标场景中的落点表，维护 `TargetSpawnId -> Transform`。

## 初始化

全局配置由 `WSFrameSetting.SceneTransitionSettings.TransitionConfig` 持有。

框架启动时，`WSFrameRoot` 会调用：

```csharp
SceneTransitionSystem.Initialize(frameSetting.SceneTransitionSettings.TransitionConfig);
```

初始化行为：

- `config == null` 时允许初始化为空。
- 运行时真正触发转场时，如果没有 Config，会抛出明确异常。
- 空 `RouteId` 会被跳过。
- 重复 `RouteId` 会输出 warning，并保留第一条。

## 配置流程

### 1. 配置目标场景落点

在目标场景放置 `SceneSpawnRoot`，推荐使用：

```text
Assets/Scripts/WSFrame/SceneSystem/SceneTransition/Prefabs/SceneSpawnRoot.prefab
```

`SceneSpawnRoot.SpawnEntries` 中每条配置包含：

- `TargetSpawnId`：落点 Id，例如 `NorthGate`。
- `SpawnTransform`：实际落点位置。

子物体名称只用于层级整理，不作为运行时查找依据。

### 2. 同步 Routes

打开包含 `SceneSpawnRoot` 的目标场景后，在 `SceneTransitionConfig` Inspector 中点击：

```text
Refresh Routes From Open Scene Spawns
```

同步规则：

- 当前打开场景中的有效 `SpawnEntries` 会生成或更新 Routes。
- 已有 Route 会保留原 `routeId`。
- 新 Route 默认使用 `{SceneName}_{TargetSpawnId}` 作为 `routeId`。
- 不会自动删除旧 Route。

如果目标场景已经打开，但某条 Route 的 `TargetSpawnId` 在场景 Root 中不存在，可以点击：

```text
Remove Invalid Routes From Open Scenes
```

这个按钮只删除“目标场景当前已打开，并且落点确实不存在”的 Route。未打开目标场景的 Route 不会被判定为失效。

### 3. 配置触发器

在入口场景中放置 `SceneTransitionTrigger2D`：

- Collider2D 会在 `Reset/OnValidate` 中自动设置为 Trigger。
- `Traveler Layer Mask` 用于限制哪些对象能触发转场。
- `Route` 通过 Inspector 菜单选择，运行时只保存 `routeId`。

触发时，Trigger 会解析出一个 traveler `Transform` 并传给 `SceneTransitionSystem`。`SceneTransitionSystem` 不关心这个 Transform 是否来自 Rigidbody2D，也不会通过 Rigidbody2D 改变位置。

## 运行时调用

通常不需要业务代码直接调用，触发器会自动调用：

```csharp
await SceneTransitionSystem.TransitionAsync(traveler, routeId);
```

如果确实需要手动触发：

```csharp
await SceneTransitionSystem.TransitionAsync(playerTransform, "Town_NorthGate");
```

移动语义：

- `SceneTransitionSystem` 直接修改传入 traveler 的 `Transform.position`。
- `ApplySpawnRotation` 为 true 时，直接修改 `Transform.rotation`。
- 不处理 `Rigidbody2D.velocity`、`angularVelocity` 或物理同步。

调用前提：

- `SceneTransitionSystem.Initialize(...)` 已执行。
- 全局 Config 不为空。
- `routeId` 存在。
- 目标场景在 Build Settings 中启用。
- 目标场景加载后存在 `SceneSpawnRoot`。
- `SceneSpawnRoot` 中存在匹配的 `TargetSpawnId`。

## 事件与 Loading UI

转场系统不新增事件。Loading UI、Fader、日志或调试面板应继续监听 `SceneSystem`：

```csharp
private IUnRegister progressUnRegister;
private IUnRegister failedUnRegister;

private void OnEnable()
{
    progressUnRegister = SceneSystem.RegisterLoadProgressChanged(args =>
    {
        Debug.Log($"Loading: {args.Progress}");
    });

    failedUnRegister = SceneSystem.RegisterLoadFailed(args =>
    {
        Debug.LogException(args.Exception);
    });
}

private void OnDisable()
{
    progressUnRegister?.UnRegister();
    progressUnRegister = null;

    failedUnRegister?.UnRegister();
    failedUnRegister = null;
}
```

## 常见错误

`SceneTransitionSystem has not been initialized.`

- 没有通过 `WSFrameRoot` 初始化框架，或手动调用转场早于框架初始化。

`SceneTransitionSystem has no SceneTransitionConfig.`

- `WSFrameSetting.SceneTransitionSettings.TransitionConfig` 没有配置。

`SceneTransitionTrigger2D has no route id.`

- Trigger 没有选择 Route。

`SceneTransitionConfig does not contain route id '...'`

- Trigger 保存的 `routeId` 在全局 Config 中不存在。
- 常见于手动删除 Route 后，没有重新选择 Trigger。

`Scene '...' does not contain a SceneSpawnRoot.`

- 目标场景缺少 `SceneSpawnRoot`。

`SceneSpawnRoot ... does not contain TargetSpawnId '...'`

- Config 中的 `TargetSpawnId` 与目标场景 Root 中的 SpawnEntries 不一致。
- 打开目标场景后刷新 Config Routes。

## 当前限制

- v1 只支持 `LoadSceneMode.Single` 转场。
- v1 面向 2D Trigger 工作流。
- Unity 场景加载不可真正取消，取消语义由底层 `SceneSystem` 处理。
- 当前运行时移动只直接修改 traveler 的 `Transform.position`，并在 `ApplySpawnRotation` 为 true 时修改 `Transform.rotation`。
- `SceneTransitionSystem` 不依赖 `Rigidbody2D` 做位置改变，也不负责清理刚体速度。

## 维护建议

- Route 配置只从 `SceneTransitionConfig` 进入运行时，不要让 Trigger 保存目标场景名或落点 Id。
- `SceneSpawnRoot.SpawnEntries` 是 `TargetSpawnId` 的权威来源。
- 不要在运行时扫描目标场景资产来修正配置。
- 不要给转场层新增事件，避免和 `SceneSystem` 的加载事件职责重叠。
- 修改 Root 落点后，打开相关场景并刷新 Config Routes。
