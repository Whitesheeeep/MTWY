# Scene System (场景加载系统)

`WS_Modules.SceneModule.SceneSystem` 是一个基于 Unity `SceneManager` 和 `UniTask` 的场景加载封装模块。它提供了一套简洁的 API 来处理同步和异步场景加载，特别支持异步加载时的进度回调和“加载后手动激活”的功能。

## 1. 特性 (Features)

*   **UniTask 支持**: 所有异步方法均返回 `UniTask`，支持 `await` 等待，方便整合进异步工作流。
*   **进度监控**: 提供加载进度的实时回调 (0.0 ~ 1.0) 以及全局事件。
*   **手动激活**: 支持场景加载到 90% 后暂停，等待业务逻辑（如过场动画结束、用户点击）确认后再激活进入场景。
*   **接口统一**: 同时支持 `string` (场景名) 和 `int` (Build Index) 两种加载方式。
*   **请求管线统一**: 同步、异步、手动激活加载共用同一套校验、开始、完成、失败、取消事件流程。
*   **Additive 管理**: 记录由 SceneSystem 以 Additive 模式加载成功的场景，并提供查询、卸载和活动场景切换辅助。

## 2. 依赖 (Dependencies)

*   [UniTask](https://github.com/Cysharp/UniTask)
*   UnityEngine.SceneManagement

## 3. 全局事件 (Global Events)

系统提供显式注册入口，方便其他模块（如 Loading UI）监听全局加载状态。注册方法会返回 `IUnRegister`，调用方应在自己的生命周期结束时注销：

```csharp
private IUnRegister progressUnRegister;
private IUnRegister succeededUnRegister;

private void OnEnable()
{
    // 监听加载进度（0.0 ~ 1.0）
    progressUnRegister = SceneSystem.RegisterLoadProgressChanged(args => {
        Debug.Log($"Global Loading Progress: {args.Progress}");
        // Update Loading Bar UI...
    });

    // 监听加载完成
    succeededUnRegister = SceneSystem.RegisterLoadSucceeded(args => {
        Debug.Log($"Scene Loaded Successfully: {args.LoadInfo.Target}");
    });
}

private void OnDisable()
{
    progressUnRegister?.UnRegister();
    progressUnRegister = null;

    succeededUnRegister?.UnRegister();
    succeededUnRegister = null;
}
```

## 4. API 与使用示例 (Usage Examples)

### 4.1 同步加载 (Sync Load)
封装了原本的 `SceneManager.LoadScene`。

```csharp
// 通过场景名加载
SceneSystem.LoadScene("GameScene");

// 通过 Build Index 加载
SceneSystem.LoadScene(1);

// 使用 LoadSceneMode (Single / Additive)
SceneSystem.LoadScene("GameScene", LoadSceneMode.Additive);
```

### 4.2 异步加载 (Async Load)
加载场景并自动激活。适合通用的转场需求。

```csharp
// 这里的 callBack 是每帧更新进度的回调
await SceneSystem.LoadSceneAsync("GameScene", (progress) => {
    Debug.Log($"Loading... {progress * 100}%");
});

Debug.Log("加载完成，虽然上面的 await 已经等到完成了，但这里依然是在场景激活之后执行");
```

### 4.3 异步加载并手动激活 (Async Load Without Active)
加载场景到 90% 后停止，等待调用者显式激活。适合需要精确控制转场时机（如等待过场动画播放完毕）的场景。

> 注意：Unity 的 `AsyncOperation` 不能真正取消。场景已经加载到 90% 并等待手动激活时，如果 `CancellationToken` 被取消，SceneSystem 会先放行场景激活，等待 Unity 操作结束，然后触发取消事件并抛出 `OperationCanceledException`，避免加载操作永久卡住。

```csharp
// 参数说明：
// 1. sceneName: 场景名
// 2. activeCallBack: 当场景准备就绪(90%)时调用的回调。
//    - 这个回调会给你一个 Action 参数 (activateScene)。
//    - 调用这个 action 才会真正进入新场景。
// 3. loadingCallBack: 进度回调 (0.0 ~ 0.9 -> 1.0)

await SceneSystem.LoadSceneAsyncWithoutActive("GameScene", 
    // 当场景加载到 90% 准备好时，会执行这个回调
    activeCallBack: (activateHandle) => {
        Debug.Log("场景已准备就绪，按 A 键进入...");
        
        // 模拟等待用户输入
        WaitForInput(activateHandle); 
    },
    // 加载进度回调
    loadingCallBack: (progress) => {
        Debug.Log($"Loading Progress: {progress}");
    }
);

void WaitForInput(Action activateHandle)
{
    // 假设这是在一个 MonoBehaviour 或其它逻辑中
    // 当满足条件时，调用 handle 激活场景
    // activateHandle.Invoke(); 
}
```

#### 完整示例：配合过场动画

```csharp
public async UniTask EnterLevel()
{
    UIManager.Show("LoadingPanel");

    await SceneSystem.LoadSceneAsyncWithoutActive(
        "Level1",
        activeCallBack: activate =>
        {
            PlayTransitionAndActivate(activate).Forget();
        },
        loadingCallBack: progress =>
        {
            LoadingPanel.SetProgress(progress);
        });
}

private async UniTaskVoid PlayTransitionAndActivate(Action activate)
{
    Debug.Log("场景准备好了，播放 3 秒过场动画...");
    await UniTask.Delay(3000);
    Debug.Log("动画结束，进入场景！");
    activate.Invoke();
}
```

### 4.4 Additive 场景管理

SceneSystem 只追踪自己通过 `LoadSceneMode.Additive` 成功加载的场景，不会自动同步外部直接调用 `SceneManager` 加载的场景。

```csharp
// Additive 加载场景，成功后会进入 SceneSystem 的 Additive 记录集合
await SceneSystem.LoadSceneAsync("HUDScene", mode: LoadSceneMode.Additive);

if (SceneSystem.IsSceneLoaded("HUDScene"))
{
    Debug.Log("HUDScene is tracked by SceneSystem.");
}

// 切换当前活动场景
SceneSystem.SetActiveScene("HUDScene");

// 获取当前记录的 Additive 场景名称快照
string[] additiveScenes = SceneSystem.GetLoadedAdditiveSceneNames();

// 卸载由 SceneSystem 记录的 Additive 场景
await SceneSystem.UnloadSceneAsync("HUDScene");
```

卸载 API 不支持取消。Unity 已经开始的卸载操作不能被真正中断，SceneSystem 会等待 Unity 操作结束后触发 `UnloadSucceeded`；校验失败或 Unity 启动卸载失败时触发 `UnloadFailed`。

```csharp
private IUnRegister unloadStartedUnRegister;
private IUnRegister unloadSucceededUnRegister;
private IUnRegister unloadFailedUnRegister;

private void OnEnable()
{
    unloadStartedUnRegister = SceneSystem.RegisterUnloadStarted(args =>
    {
        Debug.Log($"Unload started: {args.UnloadInfo.Target}");
    });

    unloadSucceededUnRegister = SceneSystem.RegisterUnloadSucceeded(args =>
    {
        Debug.Log($"Unload succeeded: {args.UnloadInfo.Target}");
    });

    unloadFailedUnRegister = SceneSystem.RegisterUnloadFailed(args =>
    {
        Debug.LogError($"Unload failed: {args.UnloadInfo.Target}, {args.Exception}");
    });
}

private void OnDisable()
{
    unloadStartedUnRegister?.UnRegister();
    unloadStartedUnRegister = null;

    unloadSucceededUnRegister?.UnRegister();
    unloadSucceededUnRegister = null;

    unloadFailedUnRegister?.UnRegister();
    unloadFailedUnRegister = null;
}
```

### 4.5 Route 配置式场景转换

Route 转场用于 2D 角色进入触发器后切换到目标场景，并落到目标场景配置的地点。转场不新增事件；Loading UI、Fader、日志等仍然监听 `SceneSystem.RegisterLoad...`。

#### 目标场景配置 SceneSpawnRoot

在目标场景中创建一个管理落点的父物体并挂载 `SceneSpawnRoot`。`SceneSpawnRoot` 维护 `TargetSpawnId -> Transform` 的序列化表，子物体名称只用于整理层级，不参与运行时查找。

```text
TownScene
└── SceneSpawnRoot
    ├── NorthGateTransform
    └── HouseDoorTransform
```

`SceneSpawnRoot` 表示例：

```text
NorthGate -> NorthGateTransform
HouseDoor -> HouseDoorTransform
```

#### Route 配置

创建 `SceneTransitionConfig`，每条 Route 表示“去哪个场景、去哪个地点”。

```text
RouteId: ToTownNorthGate
DisplayName: NorthGate
TargetSceneName: TownScene
TargetSpawnId: NorthGate
ResetRigidbodyVelocity: true
ApplySpawnRotation: false
```

#### 触发器配置

在源场景的门或区域物体上挂 `SceneTransitionTrigger2D`：

```text
TravelerLayerMask: Player
TransitionConfig: SceneTransitionConfig
Route: TownScene/NorthGate
```

Route 字段使用 Editor-only 分层菜单，菜单内容只来自 `SceneTransitionConfig.Routes`，不会扫描目标场景。选择后触发器只保存 `routeId`。

运行时流程：

```text
玩家进入触发器
-> SceneTransitionTrigger2D 根据 routeId 读取 Route
-> SceneTransitionSystem 调用 SceneSystem.LoadSceneAsync(TargetSceneName)
-> 在目标场景查找 SceneSpawnRoot
-> 用 TargetSpawnId 获取 Transform
-> 移动玩家或触发对象到目标位置
```

如果缺少 `SceneTransitionConfig`、Route、`SceneSpawnRoot` 或 `TargetSpawnId`，系统会抛出明确异常；已经完成的场景加载不会回滚。

