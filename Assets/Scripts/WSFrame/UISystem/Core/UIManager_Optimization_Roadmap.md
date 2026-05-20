# UIManager Optimization Roadmap

本文档记录 UIManager 模块后续两个阶段的优化方向，以及当前 UI 自动化生成脚本的检查结论。当前内容只作为后续参考，不表示这些结构已经实现。

## 第二阶段：结构拆分与职责稳定

目标是把 `UIManager` 从全能管理类拆成职责更稳定的内部模块，同时尽量保持现有外部 API 不变。

### 生命周期职责拆分

当前 `UIManager` 同时处理窗口加载、初始化、显示、隐藏、销毁、资源释放、层级刷新和栈弹出。第二阶段建议先把生命周期流程收敛成一条统一路径：

- `Load`：负责根据配置加载窗口 Prefab。
- `Initialize`：负责创建 `WindowBase` 实例、绑定 GameObject、调用 `OnAwake`。
- `Show`：负责显示窗口、加入可见集合、触发层级和遮罩刷新。
- `Hide`：负责隐藏窗口、移出可见集合、恢复下层窗口状态。
- `Destroy`：负责销毁实例、移除缓存、释放资源引用。

这样可以减少 `PreLoadWindow`、`PopUpWindowAsync`、`HideWindow`、`DestroyWindow` 之间的重复状态处理。

### 层级与遮罩管理拆分

层级、全屏遮挡、伪隐藏和单遮罩属于同一类显示策略，建议从 `UIManager` 中抽成独立服务，例如 `UIWindowLayerService`。

该服务负责：

- 计算当前最高层窗口。
- 管理 `Canvas.sortingOrder`。
- 管理单遮罩归属和显示状态。
- 根据 `FullScreenWindow` 处理下层窗口伪隐藏。
- 恢复全屏窗口关闭后的下层显示状态。

`UIManager` 只负责在窗口显示/隐藏后通知它重新计算。

### 窗口栈拆分

`_windowStack`、`PushWindowToStack`、`PushAndPopStackWindow`、`PopNextStackWindow` 和普通窗口打开逻辑混在一起后，容易让普通显示流程和连续弹窗流程互相影响。

建议抽出 `UIWindowStackService`：

- 管理等待弹出的窗口队列。
- 处理栈内去重。
- 处理插队到栈顶。
- 监听当前栈窗口关闭后弹出下一个窗口。

`UIManager` 只提供实际打开窗口能力，栈服务只负责编排顺序。

### 窗口状态显式化

建议给窗口运行时增加明确状态，避免异步打开、隐藏、销毁交叉时状态不清。

推荐状态：

```csharp
public enum UIWindowState
{
    Unloaded,
    Loading,
    Hidden,
    Showing,
    Visible,
    Hiding,
    Destroyed
}
```

状态变化应集中在生命周期流程中处理，不建议由多个方法分散修改。

### 容器状态收敛

当前存在 `_allWindowDic`、`_allWindowList`、`_visibleWindowList` 等多个集合。它们表达的信息有重叠，长期维护容易出现不同步。

第二阶段建议：

- 保留一个窗口运行时字典作为事实来源。
- 可见窗口列表由运行时信息过滤得到，或者由层级服务集中维护。
- 避免同一个窗口同时在多个集合中手动增删。

### 异步加载保护统一

当前 `_loadingWindowDic` 已经是正确方向。后续所有打开和预加载路径都应该走统一加载锁，避免同一窗口在异步加载期间被重复实例化。

需要重点覆盖：

- 连续调用 `PopUpWindowAsync<T>()`。
- `PreLoadWindow<T>()` 和 `PopUpWindowAsync<T>()` 同时触发。
- 加载失败后加载锁是否正确清理。
- Destroy 后立即再次打开是否进入正确状态。

## 第三阶段：架构升级与可扩展性

目标是把 UI 系统从项目内工具升级成更稳定的框架层，支持参数化、事件监听、MVVM 接入、调试和配置化资源策略。

### 窗口描述对象

建议引入 `WindowDescriptor` 或 `WindowRuntimeInfo`。

`WindowDescriptor` 偏静态配置，可包含：

- 窗口类型。
- Prefab 路径。
- 默认层级。
- 是否全屏。
- 是否使用遮罩。
- Hide 后缓存还是销毁。

`WindowRuntimeInfo` 偏运行时状态，可包含：

- 当前 `WindowBase`。
- 当前 GameObject。
- 当前状态。
- 当前 sorting order。
- 是否正在加载。
- 是否可见。

这可以减少大量散落在 `UIManager` 内部的临时判断。

### 参数化打开窗口

当前窗口传参主要依赖回调或打开后再取窗口实例设置数据。第三阶段建议支持参数化打开：

```csharp
await UIManager.Instance.PopUpWindowAsync<RewardWindow, RewardWindowArgs>(args);
```

窗口侧可提供明确入口，例如：

```csharp
public interface IWindowWithArgs<in TArgs>
{
    void SetArgs(TArgs args);
}
```

这样可以减少 `popCallBack` 滥用，让打开窗口的数据流更清晰。

### 窗口事件总线

建议提供窗口生命周期事件，方便其他系统监听 UI 状态，而不是直接访问 `UIManager` 内部集合。

推荐事件：

- `WindowOpened`
- `WindowHidden`
- `WindowDestroyed`
- `TopWindowChanged`
- `WindowStateChanged`

事件参数建议使用窗口运行时信息，而不是直接暴露内部集合。

### MVVM 接入点

后续窗口可以按以下结构组织：

```text
WindowCode -> 组装层
DataComponent -> 自动绑定组件引用
View -> UI 刷新和输入转发
ViewModel -> UI 状态和用户意图
Model/GameData -> 真实业务数据
```

`WindowCode` 不直接写复杂业务逻辑，只负责创建/绑定 `ViewModel` 和 `View`。ViewModel 通过 `XXXChanged` 事件通知 View，View 通过 `RefreshXXX` 方法刷新显示。

### 调试面板

建议增加一个 Editor 调试窗口，用于查看 UIManager 当前状态。

可显示：

- 已加载窗口。
- 当前可见窗口。
- 当前窗口栈。
- 当前最高层窗口。
- 每个窗口的 sorting order。
- 遮罩归属。
- 正在加载的窗口。
- 每个窗口的状态。

这个工具对排查遮罩层级、异步打开和窗口栈问题很有价值。

### 资源生命周期策略

建议每个窗口可配置 Hide 后策略：

- `CacheOnHide`：隐藏后保留实例。
- `DestroyOnHide`：隐藏后销毁实例。
- `PreloadAndKeep`：预加载并常驻。
- `Manual`：由业务手动控制销毁。

这类策略应进入窗口描述对象，而不是散落在业务窗口里。

### 自动化测试

第三阶段需要补充集中测试场景：

- 重复打开同一个窗口。
- 异步并发打开同一个窗口。
- 预加载期间打开窗口。
- 全屏窗口遮挡下层窗口。
- 弹窗遮罩层级。
- 窗口栈连续弹出。
- Destroy 后重新打开。
- Hide 后缓存和 DestroyOnHide 策略差异。

## 自动化生成脚本检查结论

当前 UI 自动化工具主要包含三条链路：

- `WindowCodeGeneratorTool`：生成窗口逻辑脚本。
- `WindowBindDataCompGeneratorTool`：生成 `DataComponent` 并自动绑定组件。
- `GeneratorBindItemsComponentTool`：生成可复用 Item 脚本并自动绑定组件。
- `ScriptDisplayWindow`：显示生成代码，并负责写入或追加代码。

### WindowCodeGeneratorTool

当前问题：

- 命名空间硬编码为 `WS_Modules.UIModule`，没有读取配置。
- 事件方法命名类似 `OnBagButtonButtonClick`，语义重复。
- 生成代码缺少中文 XML 注释。
- 依赖 `PlayerPrefs` 获取上一步分析结果，跨窗口/跨生成任务时不够明确。

优化建议：

- 命名空间统一读取 `WSFrameSetting.UIManagerSetting`。
- 事件命名调整为自然 UI 事件风格，例如 `OnBagButtonClicked`、`OnNameInputChanged`、`OnNameInputEnded`。
- 生成 public 方法时补中文 XML 注释。
- 用明确的生成上下文对象替代散落的 `PlayerPrefs` key。

### WindowBindDataCompGeneratorTool

当前优点：

- 已记录生成时目标对象 InstanceID，能避免编译后 Selection 改变导致误绑定。
- 已有类型校验和命名空间解析。
- 已有 InstanceID 失效后的名称回退查找。
- 手动绑定和自动回调都支持不覆盖已有引用。

当前问题：

- 自动绑定和手动绑定存在大量重复代码。
- 部分 Editor 回调中使用 `WSLog`，如果 WSLog 尚未初始化，可能不适合编辑器生成链路。
- 字段命名为 `{fieldName}{fieldType}`，可读性一般，后续改名成本较高。

优化建议：

- 抽出公共绑定服务，例如 `UIBindingFieldAssigner`。
- 编辑器生成链路优先使用 `Debug.Log*`，运行时框架初始化完成后再使用 `WSLog`。
- 保持现有字段命名兼容，后续如果要改字段命名，需要单独做迁移计划。

### GeneratorBindItemsComponentTool

当前问题：

- 编译后自动挂载仍依赖 `Selection.activeGameObject`，比 DataComponent 生成器更容易误绑定。
- 缺少和 DataComponent 生成器一致的类型校验与命名空间解析。
- 事件绑定直接 `AddListener`，如果 `OnInitialize` 被重复调用，可能重复触发。
- `OnDispose` 没有生成 RemoveListener 逻辑。
- 字段赋值依赖 InstanceID，缺少名称回退。

优化建议：

- 补齐目标对象 InstanceID 记录机制。
- 复用 DataComponent 生成器的类型解析和字段赋值逻辑。
- 生成 `OnInitialize` 时先 `RemoveListener` 再 `AddListener`。
- 在 `OnDispose` 中生成对应 RemoveListener。
- 增加 InstanceID 失效后的层级名称回退。

### ScriptDisplayWindow

当前问题：

- 写文件时先删除旧文件再创建新文件，如果写入失败，可能丢失原文件。
- 插入点依赖中文 region 字符串，例如 `UI组件事件`、`自定义字段`，结构变化后容易失效。
- 使用简单 `Contains` 判断字段和方法是否已存在，可能误判。
- “生成脚本”按钮会覆盖目标文件，但 UI 提示不够明确。

优化建议：

- 改为先写临时文件，再替换目标文件；或者直接使用 `File.WriteAllText` 覆盖。
- 使用稳定生成标记，例如 `// <auto-generated-fields>`。
- 字段和方法去重改为正则或语法级判断。
- 文件已存在时增加明确确认提示，并显示是追加还是覆盖。

## 建议实施顺序

1. 先修 `GeneratorBindItemsComponentTool` 的 Selection 误绑定风险。
2. 抽出公共字段绑定逻辑，减少 DataComponent 和 Item 生成器重复代码。
3. 调整事件生成命名和 listener 生命周期。
4. 再优化 `ScriptDisplayWindow` 的写入安全和插入点稳定性。
5. 最后再考虑字段命名规范迁移。

