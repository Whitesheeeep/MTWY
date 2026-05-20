# Agent Instructions

## 代码生成规则

- 为本项目生成或修改 C# 代码时，优先使用中文注释。
- 新增 `public` 方法、`public` 类、`public` 接口、`public` 结构体、`public` 枚举时，应添加 XML 文档注释，便于外部调用方理解用途。
- XML 注释使用中文说明，至少包含 `<summary>`。
- 有参数的 `public` 方法应补充 `<param>`；有返回值且含义不明显时应补充 `<returns>`。
- 新增 `private` 方法、`private` 类、`private` 接口、`private` 结构体、`private` 枚举时，简单用注释说明，便于理解用途。
- 注释应解释用途、行为和调用注意点，避免重复方法名或代码本身。
- 仅在新增或修改代码附近补充注释，不为无关历史代码做大范围补注释。
- 自动生成代码也应尽量遵守上述规则；如生成器已有固定模板，优先修改模板而不是手工改生成结果。
- 修改 Unity C# 脚本后，应尽量保持现有命名空间、目录结构和模块风格一致。

## 日志使用规则

- 常规运行时业务日志优先使用 `WS_Modules.LogModule.WSLog`，不要随意混用 `Debug.Log`。
- 普通信息使用 `WSLog.Log`，成功流程使用 `WSLog.LogSuccess`，可恢复问题使用 `WSLog.LogWarning`，错误流程使用 `WSLog.LogError`。
- 日志内容应包含模块前缀，例如 `[Inventory]`、`[ItemPickupCollector2D]`，并写清关键参数和失败原因。
- `WSLog` 方法带有 `[Conditional("WS_LOG_ENABLED")]`，只有启用 `WS_LOG_ENABLED` 编译宏时日志调用才会生效；不要把业务逻辑写进日志参数构建或日志调用副作用中。

## WSLog 初始化时机

- `WSLog` 的正式配置初始化发生在 `WSFrameRoot.InitWSFrameRoot()` 中：`WSLog.Init(frameSetting.logSetting)`。
- `WSLog` 静态构造会尝试访问 `WSFrameRoot.Instance.FrameSetting`；如果在 `WSFrameRoot` 初始化完成前调用，可能触发单例创建或拿不到完整配置。
- 在 `Awake`、静态构造函数、字段初始化器、`RuntimeInitializeOnLoadMethod`、编辑器导入回调等早期时机，不要直接调用 `WSLog`，除非已确认 `WSFrameRoot` 和 `FrameSetting` 已初始化。
- 早于框架初始化的诊断日志，使用 `Debug.Log/Debug.LogWarning/Debug.LogError` 作为临时兜底；进入正常运行流程后再切换回 `WSLog`。
- 不要在 `WSLog`、`LogManager`、`UnityLogger`、`WSFrameRoot` 的初始化链路中新增会再次调用 `WSLog` 的代码，避免递归初始化。
