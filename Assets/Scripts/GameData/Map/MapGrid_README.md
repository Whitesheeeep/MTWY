# Map Grid System

Map Grid System 用于把地图场景中的 Tilemap 逻辑层 Bake 成静态 Grid 数据，并在运行时提供格子查询、通行判断、邻居查询和动态覆盖能力。

## 模块职责

- `MapGridBakeSource`：挂在地图场景根节点，声明哪些 Tilemap 图层参与 Bake。
- `MapGridData_SO`：保存 Bake 后的静态地图数据。
- `MapGridDatabase`：运行时数据库，把 SO 转成一维数组索引并提供查询接口。
- `MapGridRuntimeLoader`：挂在地图场景中，场景启用时加载本场景的 `MapGridData_SO`。
- `MapGridDatabaseRegisterNode`：接入 WSFrame `ConfigInstaller`，注册 `IMapGridDatabase`。

`MapGridDatabase` 不依赖 `SceneSystem`。场景切换后，Unity 会启用新场景中的 `MapGridRuntimeLoader`，由它加载对应地图数据。

## Editor Bake 流程

1. 在地图场景根节点挂 `MapGridBakeSource`。
2. 在 `MapId` 字段选择当前场景名。该字段使用 `[WSScene]`，来源是 Build Settings 中启用的场景。
3. 点击 `Auto Fill Layers From Children`，工具会按子物体 Tilemap 名称自动识别常见逻辑层。
4. 检查 `layers` 中每个 Tilemap 对应的 `MapGridCellFlags`。
5. 点击 `Bake Map Grid Data`。

如果 `outputData` 为空，Bake 工具会自动在 `Assets/Scripts/GameData/Map/SO` 下创建 `MapGridData_SO`。

当前自动识别的图层名：

- `Collision` -> `Blocked`
- `Water` -> `Water`
- `CanDig` 或 `Dig` -> `CanDig`
- `CanDropItem` -> `CanDropItem`
- `CanPlaceFurniture` -> `CanPlaceFurniture`
- `NPC Obstacle` -> `NpcObstacle`

## Bounds 与坐标

不同 Tilemap 的 bounds 可以不一样。Bake 时会合并所有 `affectsBounds == true` 的图层 bounds，得到统一 Grid：

```text
originCell = combinedBounds 左下角 cell
width = combinedBounds.size.x
height = combinedBounds.size.y
gridX = cell.x - originCell.x
gridY = cell.y - originCell.y
index = gridY * width + gridX
```

运行时查询以 Unity Tilemap 的 `Vector3Int cellPosition` 为权威坐标，`gridX/gridY` 只是数组索引。

## 运行时接入

1. 创建 `MapGridDatabaseRegisterNode`。
2. 将该节点挂到现有 `GameDatabaseRegisterModule` 的子节点中。
3. 在地图场景中挂 `MapGridRuntimeLoader`。
4. 把 Bake 得到的 `MapGridData_SO` 引用到 `MapGridRuntimeLoader.mapGridData`。
5. 把当前地图场景的 `Grid` 手动拖拽到 `MapGridRuntimeLoader.grid`。

场景启用时：

```text
MapGridRuntimeLoader.OnEnable
-> GameDatabase.Get<IMapGridDatabase>()
-> LoadMap(mapGridData, grid)
```

场景卸载或对象禁用时：

```text
MapGridRuntimeLoader.OnDisable
-> 如果数据库当前持有自己的 mapGridData
-> UnloadCurrentMap()
```

## 跨场景地图数据缓存（LRU Catalog）

`MapGridDatabase` 只负责当前已加载地图场景的运行时查询。它持有当前场景的 `MapGridData_SO` 和 `Grid`，因此可以处理 `WorldToCell`、`GetCellCenterWorld` 这类世界坐标转换。

NPC 跨场景移动、离线寻路和日程模拟不应该加载所有 Unity 场景，也不应该依赖当前场景的 `Grid`。后续新增独立的 `IMapGridCatalog`，按 `mapId` 异步加载轻量 `MapGridData_SO`，并用 LRU 缓存控制内存。

职责划分：

- `IMapGridDatabase`：当前场景地图，负责当前地图查询、动态覆盖、世界坐标与 cell 坐标转换。
- `IMapGridCatalog`：跨场景静态地图查询，负责按 `mapId` 加载和缓存 `MapGridData_SO`，不提供世界坐标转换。

LRU Catalog 第一版使用 `ResSystem Key` 配置地图资源：

```text
mapId -> resourceKey
maxCachedMaps
pinCurrentMap
```

缓存 miss 时由调用方显式异步加载：

```csharp
IMapGridCatalog catalog = GameDatabase.Get<IMapGridCatalog>();

if (await catalog.EnsureLoadedAsync(targetMapId)
    && catalog.IsWalkable(targetMapId, nextCell))
{
    // 推进 NPC 离线移动状态
}
```

LRU 淘汰时，Catalog 通过 `ResSystem` 卸载对应 `MapGridData_SO`。当前场景地图默认可以被 pin，避免 NPC 离线查询导致当前地图被淘汰。

注意：

- Catalog 只加载 `MapGridData_SO`，不会加载 Tilemap、Renderer、Collider、NPC 实体或完整 Unity 场景。
- Catalog 查询只依赖 `originCell / width / height / cells / staticFlags`。
- Catalog 第一版只处理静态地图数据。
- 家具、建筑等持久动态障碍后续通过 `mapId + cell` 的动态覆盖数据源接入。
- 未缓存地图的 `TryGetCell / IsWalkable / GetNeighbors` 不触发同步加载，调用方需要先 `await EnsureLoadedAsync(mapId)`。

## 查询示例

```csharp
IMapGridDatabase mapGrid = GameDatabase.Get<IMapGridDatabase>();
Vector3Int cell = mapGrid.WorldToCell(worldPosition);

if (mapGrid.TryGetCell(cell, out MapGridCellInfo info))
{
    bool canDig = (info.FinalFlags & MapGridCellFlags.CanDig) != 0;
    bool walkable = mapGrid.IsWalkable(cell);
    Vector3 cellCenter = mapGrid.GetCellCenterWorld(cell);
}
```

邻居查询可供 A* 或区域逻辑使用：

```csharp
foreach (Vector3Int neighbor in mapGrid.GetNeighbors(cell))
{
    // neighbor 已经过 IsWalkable 过滤
}
```

## Runtime Overrides

静态 SO 不保存家具、作物、临时障碍等运行时状态。这些业务对象应该独立存档，然后在加载后把对 Grid 的影响同步给 `MapGridDatabase`。

例：家具占用某格，临时添加阻挡：

```csharp
mapGrid.SetRuntimeOverride(
    sourceId: "Furniture:chair_001",
    cell: furnitureCell,
    addFlags: MapGridCellFlags.Blocked);
```

家具移除时清除自己的覆盖：

```csharp
mapGrid.ClearRuntimeOverrides("Furniture:chair_001");
```

`sourceId` 用于避免不同系统互相误删覆盖。比如家具和作物同时影响同一格时，只清除家具的 `sourceId` 不会影响作物。

## 场景切换约定

方案 A 保留 `MapGridRuntimeLoader`：

- 地图场景放一个 `MapGridRuntimeLoader`。
- 非地图场景不放 `MapGridRuntimeLoader`。
- `SceneSystem` 不需要额外注册地图事件。
- Single 场景切换时，旧 Loader 卸载旧地图，新 Loader 加载新地图。

如果后续需要 Additive 多地图同时存在，再扩展 `IMapGridDatabase` 为多地图并行查询。

## 注意事项

- `MapId` 当前约定等于场景名，SO 不再额外保存 `SceneName`。
- `Walkable` 不作为静态 flag 存储，运行时通过 `Blocked | Water | NpcObstacle` 等阻挡属性计算。
- `MapGridData_SO` 只由 Editor Bake 写入，运行时不要修改。
- Bake 后需要确认 `MapGridRuntimeLoader` 引用的是最新的 `MapGridData_SO`。
- 世界坐标与 cell 坐标转换依赖场景中的 `Grid`，不要根据 `originCell` 和 `cellSize` 手算。
