# Scene System (场景加载系统)

`WS_Modules.SceneModule.SceneSystem` 是一个基于 Unity `SceneManager` 和 `UniTask` 的场景加载封装模块。它提供了一套简洁的 API 来处理同步和异步场景加载，特别支持异步加载时的进度回调和“加载后手动激活”的功能。

## 1. 特性 (Features)

*   **UniTask 支持**: 所有异步方法均返回 `UniTask`，支持 `await` 等待，方便整合进异步工作流。
*   **进度监控**: 提供加载进度的实时回调 (0.0 ~ 1.0) 以及全局事件。
*   **手动激活**: 支持场景加载到 90% 后暂停，等待业务逻辑（如过场动画结束、用户点击）确认后再激活进入场景。
*   **接口统一**: 同时支持 `string` (场景名) 和 `int` (Build Index) 两种加载方式。

## 2. 依赖 (Dependencies)

*   [UniTask](https://github.com/Cysharp/UniTask)
*   UnityEngine.SceneManagement

## 3. 全局事件 (Global Events)

系统提供了两个静态事件，方便其他模块（如 Loading UI）监听全局加载状态：

```csharp
// 监听加载进度（0.0 ~ 1.0）
SceneSystem.OnLoadingSceneProgress += (progress) => {
    Debug.Log($"Global Loading Progress: {progress}");
    // Update Loading Bar UI...
};

// 监听加载完成
SceneSystem.OnLoadSceneSucceed += () => {
    Debug.Log("Scene Loaded Successfully!");
};
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
    // 1. 显示 Loading 界面
    UIManager.Show("LoadingPanel");
    
    // 2. 开始加载场景，但不立即进入
    Action activator = null;
    
    await SceneSystem.LoadSceneAsyncWithoutActive("Level1", 
        activeCallBack: (act) => {
            // 拿到激活器
            activator = act;
        },
        loadingCallBack: (p) => {
            // 更新 UI 进度条
            LoadingPanel.SetProgress(p);
        }
    );
    
    // 此时场景加载到了 90%，且 activeCallBack 已经被调用，activator 已经有值了
    
    // 3. 等待 Loading 进度条动画跑完，或者等待玩家按键
    await UniTask.Delay(1000); 
    
    // 4. 激活场景
    activator?.Invoke();
    
    // 5. 等待场景彻底切换完成（SceneSystem 内部会等待激活后的最后一步）
    // 注意：上面的 await LoadSceneAsyncWithoutActive 其实在 activator 没调用前是不会结束的吗？
    // 修正：LoadSceneAsyncWithoutActive 内部是 await 等待直到 isDone。
    // 所以逻辑上，activeCallBack 是在内部 while 循环中调用的。
    // 如果你在 activeCallBack 里直接 invoke，那就会直接往下走。
    // 如果你在 activeCallBack 里只是存了引用（如上例），那么 LoadSceneAsyncWithoutActive 的 Task 就会一直卡住等待。
    // 这是一个死锁！
    
    // --- 正确写法 ---
    await SceneSystem.LoadSceneAsyncWithoutActive("Level1", (activate) => {
        // 在这个回调里，你可以做任何异步等待，然后再激活
        AsyncActivate(activate).Forget();
    }, (p) => {
        Debug.Log(p);
    });
}

private async UniTaskVoid AsyncActivate(Action activate)
{
    Debug.Log("场景准备好了，播放 3秒 过场动画...");
    await UniTask.Delay(3000);
    Debug.Log("动画结束，进入场景！");
    activate.Invoke();
}
```

