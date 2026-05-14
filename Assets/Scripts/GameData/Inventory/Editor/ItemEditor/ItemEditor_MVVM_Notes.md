# ItemEditor MVVM Notes

## 结构概览

`ItemEditorWindow` 是组装层：负责加载主 UXML、列表行 UXML，创建 `ItemEditorViewModel` 和 `ItemEditorView`。它不处理具体字段逻辑，只决定窗口入口、默认数据源和资源缺失时的错误显示。

`ItemEditorView` 是表现层：负责查询 UI 控件、注册 UI 事件、把 ViewModel 的状态刷新到 UI。它不直接维护物品数据规则，所有编辑都通过 ViewModel 的方法进入。

`ItemEditorViewModel` 是编辑状态层：持有当前 `ItemDataList_SO`、搜索关键字、过滤结果和当前选中项。新增、删除、字段修改、Undo、Dirty 标记和保存逻辑都集中在这里。

`ItemRowViewData` 是列表行显示数据：把 `ItemData` 转成 ListView 需要的图标、名称和详情文本，避免 ListView 直接处理业务对象的显示格式。

## 数据流

用户操作从 View 进入，例如点击列表、修改字段、搜索、添加或删除。View 不直接改 `ItemDataList_SO`，而是调用 ViewModel。

ViewModel 修改数据后触发事件：

- `DataListChanged`：数据源或搜索框状态需要同步。
- `ItemsChanged`：列表内容、搜索结果或列表行显示需要刷新。
- `SelectionChanged`：右侧详情区需要切换或重绘。

View 收到事件后调用对应刷新方法，并使用 `SetValueWithoutNotify` 回填字段，避免 UI 回填再次触发编辑事件。

## 关键设计选择

ListView 行只做展示，不放可编辑字段。这样点击列表只改变选择，不会在鼠标事件中同时触发字段绑定、列表重建和详情刷新。

详情区字段手写 `RegisterValueChangedCallback`。虽然代码量更多，但可以统一走 `EditSelected`，确保 Undo、Dirty、搜索刷新和选择刷新一致。

图标预览使用 `Image.sprite`，不使用 `AssetPreview`。`sprite` 能正确处理图集裁剪区域，也不会依赖 Editor 预览图的异步生成。

ObjectField 的类型约束放在 C# 中设置，UXML 只描述布局和 class。这样可以减少 UI Builder 对项目类型解析失败导致的显示问题。

## 扩展点

新增字段时需要同步修改三处：`ItemData` 数据结构、UXML 中的字段控件、`ItemEditorView.ConfigureFields` 和 `RefreshDetails` 中的绑定逻辑。字段数据修改应继续走 `Edit(...)` 或专门方法，避免绕过 ViewModel。

新增列表显示内容时优先改 `ItemRowViewData` 和 `ItemListRow.uxml`，不要把显示格式散落在 `BindItemRow` 中。

新增复杂编辑行为时优先放进 ViewModel，例如批量修改、排序、复制、校验、自动生成 ID。View 只负责把用户意图转成 ViewModel 调用。

如果将来需要使用 `SerializedObject.Bind`，建议只在简单详情区局部使用，不建议把可编辑字段放进虚拟化 ListView 行内。ListView 行内编辑容易和选择、复用、重建产生事件链冲突。

## 注意事项

`isRefreshing` 是防止 UI 回填触发写入的保护开关。任何批量刷新字段的代码都应设置它。

`Undo.RecordObject(DataList, ...)` 必须记录宿主 `ScriptableObject`，因为 `ItemData` 是嵌套在 SO List 内的普通序列化类。

修改嵌套数据后必须 `EditorUtility.SetDirty(DataList)`，否则 Unity 可能不会保存资产变更。

搜索过滤结果由 ViewModel 维护。View 不应自己判断搜索规则，否则列表和选择状态容易分叉。
