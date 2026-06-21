# Farm / Furniture / MapGrid 框架方案

## Summary

第一阶段先做“耕种闭环”：锄地、浇水、播种、湿润期间生长、收获。`MapGrid` 保持底层地图查询职责，只保存静态格子能力与运行时占用投影；`Farm/Land` 保存地块、浇水、作物状态；`Furniture` 后续独立保存家具实例并用同一套投影方式影响格子。

## Key Changes

- 新增 `CellActionController` 作为点击动作入口：监听左键，读取 `CursorManager.CurrentState`、玩家当前选中物品/工具，统一路由到 `FarmLandManager`，后续再接 `FurnitureManager`。
- 新增 `FarmLandManager` 管理跨地图地块运行时状态：按 `mapId + cell` 存储耕地、湿润、作物记录；即使地图不在当前场景，只要游戏在运行，后台状态继续推进。
- 新增 `CropData_SO` 和作物数据库：用 `seedItemId` 映射作物配置，配置阶段时长、阶段表现 prefab/sprite、收获物 itemId/count。
- 表现层采用 `Tilemap + Prefab`：耕地/湿润由场景内 Farm Tilemap 显示，作物阶段由 prefab 或池化实例显示；地图未加载时只推进数据，不生成表现对象。
- `MapGrid` 只接收投影：耕地、作物、家具用 `MapGridManager.TryApplyOverride` 写入影响，例如移除 `CanPlaceFurniture`，必要时添加 `Blocked` 或 `NpcObstacle`；清理时用稳定 `sourceId` 移除。

## Behavior

- 锄地：要求目标 cell 的 `MapGridCellFlags.CanDig` 存在，且无作物/家具占用；成功后生成耕地记录，设置退化结束时间。
- 耕地退化：耕地有持续时间；未种植且到期后恢复普通土地，清除 Farm 对 MapGrid 的投影。
- 浇水：要求 cell 已是耕地；设置湿润结束时间。湿润状态有持续时间，到期后变干。
- 播种：要求 cell 已耕地且无作物；消耗背包中的种子，创建作物记录，并绑定对应 `CropData_SO`。
- 生长：作物只有在地块湿润时推进阶段计时；湿润到期后暂停，重新浇水后继续累计剩余阶段时间。
- 收获：成熟后用收获工具或交互动作收获；优先加入 `InventoryManager`，放不下时后续可接 `WorldItemManager` 掉落。

## API / Data Shape

- `FarmCellRecord`：`mapId`、`cell`、`soilState`、`tilledUntilTotalMinutes`、`wateredUntilTotalMinutes`、`cropInstanceId`。
- `CropRuntimeRecord`：`cropId`、`seedItemId`、`stageIndex`、`remainingStageMinutes`、`isMature`。
- `CropData_SO`：`cropId`、`seedItemId`、`harvestItemId`、`harvestCount`、`stages`，每个 stage 包含 `durationMinutes` 和表现资源引用。
- `FarmLandManager` 公开方法：`TryTill`、`TryWater`、`TryPlant`、`TryHarvest`、`TryGetCellState`、`RestoreRuntimeProgress`、`ApplyMapGridProjection`、`ClearMapGridProjection`。
- `sourceId` 规范：Farm 使用 `Farm:{mapId}:{x}:{y}`，Furniture 后续使用 `Furniture:{instanceId}`。

## Test Plan

- Odin 手动测试组件优先：测试锄地、浇水、播种、生长暂停/恢复、成熟收获、耕地退化。
- MapGrid 投影测试：锄地/种植后指定 cell 不能摆家具；清除/退化/收获后投影正确恢复。
- 跨地图后台测试：加载 A 地图创建作物，切到 B 地图推进时间，再回 A 地图检查阶段和表现同步。
- Cursor/Action 测试：不可操作 cell 不显示可交互；工具范围外不能执行；背包无种子时播种失败且不改变地块。
- 时间测试：湿润时阶段倒计时减少；干燥时生长暂停；重新浇水后继续推进。

## Assumptions

- 第一版不做读档后的离线补算；只处理本次运行期间的后台推进。
- 第一版先完成耕种闭环，家具只预留同样的 `MapGrid` 投影边界。
- `MapGridCellFlags` 不扩展为业务状态枚举；业务查询走 `FarmLandManager`，MapGrid 只负责可走、可放置、阻挡等底层判断。
