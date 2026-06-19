# Character Schedule System

角色日程系统用于驱动 NPC 在不同时间、条件下切换目标位置，并在当前场景内生成可见 Agent 执行移动。系统当前第一版以运行时内存状态为主，支持同地图寻路和一跳直接跨场景移动。

## 核心职责

- `CharacterScheduleManager`：角色逻辑状态中心。维护 `CharacterRuntimeState`，评估 Schedule Entry，写入移动目标和路径状态。
- `CharacterAgentManager`：当前场景 Agent 生命周期管理。根据当前 `MapGridManager.CurrentMapId` 生成、回收、同步可见角色实体。
- `CharacterAgent`：场景内实体表现。沿 cell path 移动，并把到达 cell / 到达终点回报给 `CharacterScheduleManager`。
- `CharacterSchedulePlanner`：路径规划入口。同地图调用 `MapPathfindingService`，跨地图读取 `SceneTransitionSystem` 的直接边。
- `CharacterScheduleCondition`：日程条件基类。Condition 不保存运行时状态，只通过 `ICharacterScheduleServices` 读取时间、flag、当前评估上下文等服务。

## 数据配置

### CharacterDefinition_SO

创建菜单：

```text
GameData/Character/Definition
```

字段语义：

- `characterId`：角色唯一 ID，需要与 `CharacterSchedule_SO.characterId` 一致。
- `displayName`：编辑器和日志显示名。
- `prefabKey`：角色 Agent prefab 的 Addressables key，运行时由 `PoolManager` 使用。
- `defaultMapId`：无存档时的默认地图，当前约定与 SceneName / MapGrid mapId 对齐。
- `defaultCell`：无存档时的默认逻辑格子。
- `defaultMoveSpeed`：Entry 没有设置速度覆盖时使用。

### CharacterSchedule_SO

创建菜单：

```text
GameData/Character/Schedule
```

每个 `CharacterSchedule_SO` 对应一个 `characterId`，内部保存 `List<CharacterScheduleEntry>`。

### CharacterScheduleEntry

Entry 是一个候选日程目标：

- `entryId`：日程项唯一 ID，运行时写入 `activeEntryId`。
- `priority`：优先级。多个 Entry 同时满足时，数值越大越优先。
- `targetMapId`：目标地图 ID，当前约定与 SceneName / MapGrid mapId 对齐。
- `targetCell`：目标逻辑格子。移动和离线结算都以 cell 为权威位置。
- `moveSpeedOverride`：大于 0 时覆盖角色默认移动速度。
- `conditions`：条件列表。为空表示永远满足；不为空时所有条件都满足才算该 Entry 满足。

选择规则：

- 全量遍历角色的所有 Entry。
- 没有 Condition 的 Entry 视为满足。
- 多个 Entry 满足时选 `priority` 最大者。
- `priority` 相同时，如果当前 `activeEntryId` 仍满足，则保持当前 Entry；否则按列表遍历结果选择。
- 没有 Entry 满足时角色进入 `Idle`。

## 条件与服务

当前内置条件：

- `ScheduleTimeRangeCondition`
  - 创建菜单：`GameData/Character/Schedule Condition/Time Range`
  - 读取 `GameTimeManager.Instance.CurrentTime`
  - 支持跨天时间段，例如 `22:00-02:00`
- `ScheduleFlagCondition`
  - 创建菜单：`GameData/Character/Schedule Condition/Flag`
  - 读取 `CharacterScheduleManager` 内部 flag 字典
  - 外部通过 `CharacterScheduleManager.Instance.SetFlag(flagId, value)` 写入

Condition 通过 `ICharacterScheduleServices` 读取运行时服务，不直接依赖 Manager。当前服务包括：

- `ICharacterScheduleEvaluationService`：当前正在评估的 `CharacterId / State / Entry`
- `ICharacterScheduleTimeService`：游戏时间
- `ICharacterScheduleFlagService`：Schedule flag
- `ICharacterScheduleMapService`：地图查询入口

## 初始化链路

通过 `CharacterScheduleRegisterNode` 接入 `ConfigInstaller`。

创建菜单：

```text
GameData/Character/Schedule Register Node
```

注册流程：

```text
CharacterScheduleRegisterNode.Register()
-> new CharacterDefinitionDatabase(definitions)
-> GameDatabase.Register<ICharacterDefinitionDatabase>(definitionDatabase)
-> CharacterScheduleManager.Instance.Initialize(definitionDatabase, schedules)
```

初始化后：

- 根据所有 `CharacterDefinition_SO` 创建默认 `CharacterRuntimeState`
- 加载所有 `CharacterSchedule_SO`
- 注册 Schedule Services
- 订阅 `GameTimeManager` 的分钟、小时、天变化事件
- 触发一次 `EvaluateAllAsync`

## 运行时状态

`CharacterRuntimeState` 是角色逻辑状态，离线角色也只存在这里。

关键字段：

- `characterId`
- `currentMapId`
- `currentCell`
- `activeEntryId`
- `moveState`
- `targetMapId`
- `targetCell`
- `remainingPath`
- `moveSpeed`
- `blockedReason`

`CharacterMoveState`：

- `Idle`：没有可执行 Entry
- `Moving`：有路径，当前场景 Agent 可执行移动
- `Arrived`：已经到达目标
- `Blocked`：无法规划路径或缺少必要数据

## 移动流程

### 同地图移动

当 `currentMapId == targetMapId`：

```text
CharacterScheduleManager.StartEntryAsync
-> CharacterSchedulePlanner.PlanAsync
-> MapPathfindingService.TryFindPathAsync(mapId, currentCell, targetCell)
-> state.remainingPath = path 去掉起点
-> state.moveState = Moving
-> CharacterAgentManager 同步当前场景 Agent
-> CharacterAgent.MoveAlongCells
-> Agent 每到达一个 cell 调 ReportAgentReachedCell
-> Agent 走完后调 ReportMoveArrived
```

如果路径为空且角色已在目标格，则直接进入 `Arrived`。

### 跨场景移动

当前第一版只支持一条直接 `SceneTransitionEdge`：

```text
currentMapId -> targetMapId
```

规划流程：

```text
CharacterSchedulePlanner.FindDirectEdge(currentMapId, targetMapId)
-> 读取 SceneTransitionSystem.GetEdgesFromScene(currentMapId)
-> 找到 edge.toPoint.sceneName == targetMapId 的边
-> 只规划当前地图 currentCell -> edge.fromPoint.cell
-> Agent 在当前地图走到 fromPoint
-> ReportMoveArrived 后逻辑结算到 targetMapId / targetCell
```

也就是说，跨场景第一版不会让 NPC 真实加载目标场景并继续走第二段；目标地图内的位置通过逻辑结算完成。玩家之后进入目标场景时，`CharacterAgentManager` 会根据 `currentMapId == MapGridManager.CurrentMapId` 生成该 NPC。

## Agent 管理

`CharacterAgentManager` 是当前场景实体管理器：

- 监听 `SceneSystem` 加载完成事件
- 等待 `MapGridManager` 当前 Grid 就绪
- 回收不属于当前地图的 Agent
- 为 `CharacterScheduleManager.GetCharactersInMap(CurrentMapId)` 返回的角色生成 Agent
- 角色状态变化时同步 Agent

生成 Agent 时：

```text
CharacterDefinition_SO.prefabKey
-> PoolManager.Instance.Get(prefabKey, agentRoot)
-> 确保 GameObject 上存在 CharacterAgent
-> CharacterAgent.Bind(characterId)
```

Agent 位置通过 `MapGridManager.Instance.GetCellCenterWorld(cell)` 转成世界坐标。

## 与其他系统的关系

- `GameDatabase`
  - 保存 `ICharacterDefinitionDatabase`
- `ConfigInstaller`
  - 负责调用 `CharacterScheduleRegisterNode`
- `GameTimeManager`
  - 时间变化会触发 Schedule 重新评估
- `MapGridManager`
  - 提供当前地图、Grid、cell/world 转换和路径查询基础数据
- `MapPathfindingService`
  - 执行同地图 A* 路径规划
- `SceneTransitionSystem`
  - 提供跨地图直接边
- `PoolManager`
  - 生成和回收当前场景 Agent prefab

## 测试方式

测试组件：

```text
Assets/Scripts/CharacterSchedule/Test/CharacterScheduleOdinTester.cs
```

它只使用真实运行时系统，不再注册测试数据库，也不主动加载地图。测试前需要：

- `ConfigInstaller` 已执行，`CharacterScheduleRegisterNode` 已初始化角色数据
- 当前场景 `MapGridRuntimeLoader` 已加载当前地图 Grid
- 当前场景有 `GameTimeManager`
- 若要生成 Agent，`prefabKey` 对应资源能被 `PoolManager` 加载

常用按钮：

- `打印运行时状态`
- `设置 Flag 并评估`
- `设置时间并评估`
- `执行全量评估`
- `一键示例：OldMan 单场景移动`
- `一键示例：OldMan 跨场景移动`
- `模拟 Agent 到达 Cell`
- `模拟 Agent 走完路径`
- `断言跨场景结算结果`
- `刷新当前场景 Agent`

当前示例数据：

- 08:00：`npc_old_man` 选择 `oldman_morning_field`
  - 目标：`01_MainScene / (40,-10,0)`
- 13:00：`npc_old_man` 选择 `oldman_afternoon_home`
  - 目标：`02_Home_01 / (8,-12,0)`
- `schedule.oldman.go_square == true` 时，高优先级 `oldman_emergency_square` 会覆盖时间条件

## 当前限制

- 运行时状态暂存在内存，还没有接入存档。
- 跨地图只支持一跳直接 `SceneTransitionEdge`，还没有多跳图搜索。
- 跨地图第一版只执行当前地图内的可见路径，目标地图内路径暂不执行。
- 离线角色如果不在当前 `MapGridManager.CurrentMapId`，第一版会直接结算到目标。
- Condition 第一版全量遍历，没有 TriggerType 或条件索引优化。
- Agent 生成依赖 `prefabKey` 和 PoolManager；如果池或资源缺失，只保留逻辑状态，不生成实体。

## 扩展建议

- 接入存档：保存 `CharacterRuntimeState` 的 `characterId / currentMapId / currentCell / activeEntryId / moveState / targetMapId / targetCell / remainingPath / moveSpeed`。
- 多跳跨地图：把 `SceneTransitionGraph_SO` 当作图，SchedulePlanner 用图搜索得到多段 Transition。
- 目标地图内离线路径：结合 MapGrid 多地图缓存，为离线 NPC 规划目标地图内第二段路径。
- 条件优化：把 Condition 按时间、flag、事件类型索引，避免每次全量遍历所有 Entry。
