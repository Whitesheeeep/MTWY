# Agent Instructions

## 代码生成规则

- 为本项目生成或修改 C# 代码时，优先使用中文注释。
- 新增 `public` 方法、`public` 类、`public` 接口、`public` 结构体、`public` 枚举时，应添加 XML 文档注释，便于外部调用方理解用途。
- XML 注释使用中文说明，至少包含 `<summary>`。
- 有参数的 `public` 方法应补充 `<param>`；有返回值且含义不明显时应补充 `<returns>`。
- 新增或修改 `private` 方法、`private` 类、`private` 接口、`private` 结构体、`private` 枚举时，不要机械添加注释；只有当用途、状态约束、调用顺序、边界条件或副作用不够直观时，才必须用简短中文注释说明。简单转发、字段赋值、空值检查、单行表达式等自解释代码不需要注释。
- 注释应解释用途、行为和调用注意点，避免重复方法名或代码本身。
- 仅在新增或修改代码附近补充注释，不为无关历史代码做大范围补注释。
- 自动生成代码也应尽量遵守上述规则；如生成器已有固定模板，优先修改模板而不是手工改生成结果。
- 修改 Unity C# 脚本后，应尽量保持现有命名空间、目录结构和模块风格一致。
- 对于只有一行实现内容的代码块，默认省略大括号并写成单行，例如 `if (condition) return;`；只有当单行会明显降低可读性、包含复杂条件、或需要避免歧义时才保留大括号。若方法适合表达式主体，优先使用 `=>` 表达式主体。
- C# 脚本应根据功能职责使用 `#region` 进行分区，例如字段、属性、生命周期、初始化、绑定、刷新、事件处理、工具方法等；分区名称使用中文或项目既有命名风格，避免把无关逻辑混在同一区域。
- 在 Unity 项目中新增普通 C# 脚本、接口、UXML、USS、文档或其他源文件时，只生成实际源文件，不手动创建或修改对应 `.meta` 文件；`.meta` 由 Unity 导入和编译流程自动生成与维护，以减少无效文件和 token 消耗。
- 只有在确实需要保留或维护 Unity GUID 引用时才处理 `.meta`，例如移动或重命名已有 Unity 资产/脚本时必须连同原 `.meta` 一起移动；手写 prefab、scene、asset YAML 且需要立刻引用某个 GUID；新建并立即被其他 Unity 资产引用的 ScriptableObject、Prefab、材质、图片、Addressables 资源等。若只是新增不被资产 GUID 引用的普通源文件，不要手动生成 `.meta`。

## 性能与防御式检查规则

- 依赖检查、布局配置、事件绑定、缓存重建等初始化类逻辑，应优先放在生命周期入口、`Bind`、`Initialize`、`SetContext`、容量变化或结构变化方法中处理。
- 不要在 `RefreshSlot`、`RefreshSelection`、`RefreshDropPreview`、`Update` 高频路径或单槽刷新路径里反复执行防御式初始化、Hierarchy 扫描、布局重配、事件重绑等操作；这类写法只会徒增性能消耗。
- 如果担心外部状态变化，使用明确的 dirty 标记或结构变化入口触发重建，而不是每个 public/private 方法都兜底调用 `Ensure...` / `Configure...`。
- 单槽数据变化只刷新单槽显示；容量、布局、可见范围变化应走整体刷新或结构刷新路径。

## 编码与乱码防护规则

- 项目内源文件和文档统一按 UTF-8 处理；涉及中文注释、中文 Markdown、XML 注释时，必须避免使用会按系统默认编码读写的方式。
- 修改已有中文文件前，先确认当前文件编码和内容状态。推荐检查：
  - 严格 UTF-8 解码是否通过。
  - 是否存在 `锟斤拷`、`銆`、`锛`、`浠ｇ爜` 等典型乱码特征；检查时应区分正文乱码与用于说明乱码的字面量示例。
- 发现文件已经是合法 UTF-8 但中文内容乱码时，不要继续追加乱码；应恢复为正常中文后再修改。
- 发现文件不是合法 UTF-8 时，不要直接用 `apply_patch` 强行修改；先读取结构，确认内容，再用明确 UTF-8 写回，避免半文件乱码。
- 不要用不指定编码的批量脚本读写中文文件；PowerShell 写文件时必须显式指定 UTF-8，例如 `Set-Content -Encoding UTF8`。
- 不要对整个目录做盲目的编码转换。先扫描命中文件，再逐个处理，避免破坏 Unity 生成文件、二进制资源或用户未要求修改的文件。
- 修改编码或修复乱码后必须复查：
  - 严格 UTF-8 解码通过。
  - 乱码特征搜索无结果；用于说明乱码的字面量示例应单独核对。
  - `git diff --check` 无空白错误。
- 如果本次修改只涉及注释或文档，不要额外运行会改动工程状态的 Unity 操作。
- 如果出现编码事故，优先收敛范围、修复已影响文件，并在最终回复中明确说明检查结果。

## 日志使用规则

- 常规运行时业务日志优先使用 `WS_Modules.LogModule.WSLog`，不要随意混用 `Debug.Log`。
- 普通信息使用 `WSLog.Log`，成功流程使用 `WSLog.LogSuccess`，可恢复问题使用 `WSLog.LogWarning`，错误流程使用 `WSLog.LogError`。
- 日志内容应包含模块前缀，例如 `[Inventory]`、`[ItemPickupCollector2D]`，并写清关键参数和失败原因。
- `WSLog` 方法带有 `[Conditional("WS_LOG_ENABLED")]`，只有启用 `WS_LOG_ENABLED` 编译宏时日志调用才会生效；不要把业务逻辑写进日志参数构建或日志调用副作用中。

## WSLog 初始化时机

- `WSLog` 的正式配置初始化发生在 `WSFrameRoot.InitWSFrameRoot()` 中：`WSLog.Init(frameSetting.logSetting)`。
- `WSLog` 静态构造会尝试访问 `WSFrameRoot.Instance.FrameSetting`；如果在 `WSFrameRoot` 初始化完成前调用，可能触发单例创建或拿不到完整配置。
- 在 `Awake`、静态构造函数、字段初始化器、`RuntimeInitializeOnLoadMethod`、编辑器导入回调等早期时机，不要直接调用 `WSLog`，除非已确认 `WSFrameRoot` 和 `FrameSetting` 已初始化。
- 早于框架初始化的诊断日志，使用 `Debug.Log` / `Debug.LogWarning` / `Debug.LogError` 作为临时兜底；进入正常运行流程后再切换回 `WSLog`。
- 不要在 `WSLog`、`LogManager`、`UnityLogger`、`WSFrameRoot` 的初始化链路中新增会再次调用 `WSLog` 的代码，避免递归初始化。

## 未来计划记录规则

- 用户明确表示“暂不实现”、“未来计划”、“后续再做”、“作为讨论”或类似含义时，不要把这些内容直接实现到运行时代码中。
- 这类未来计划统一记录到 `Assets/Scripts/Temp/plan.md`，作为项目内长期维护的计划记录文件。
- 计划内容使用中文记录，建议包含：背景、结论、暂不实现项、未来触发条件。
- 如果 `Assets/Scripts/Temp/plan.md` 已经存在，优先追加或更新对应主题，避免重复创建多个零散计划文件。
- 只有当用户明确要求“实现”、“执行”、“开始写代码”或给出同等明确指令时，才将计划内容落到实际代码中。
