# Map Grid System

Map Grid System 用于把 Tilemap 逻辑层 Bake 成静态格子数据，并在运行时提供当前地图查询、多地图缓存、A* 判断和动态 runtime override。

## 模块职责

- `MapGridBakeSource`：Editor-only Bake 入口，只用于声明参与扫描的 Tilemap 图层。
- `MapGridData_SO`：Bake 后生成的地图静态数据资产，只保存逻辑 cell、flags、bounds 和索引。
- `MapGridCatalog_SO`：运行时地图索引表，按 `mapId` 配置 Addressables `resourceKey`、缓存上限和是否常驻。
- `MapGridRuntimeLoader`：场景内 Loader，只配置当前场景的 `mapId` 和 `Grid`。
- `MapGridManager`：对外唯一入口，封装当前地图、多地图查询、坐标转换和 runtime override。
- `MapGridDatabase`：多地图静态/动态数据聚合器，负责 pinned 地图、LRU 地图缓存和业务日志。
- `MapGridMapCache`：MapGrid 专用运行时地图缓存，统一封装 pinned 地图与可淘汰 LRU 地图。
- `MapGridStaticModule`：单张地图的静态 SO 索引。
- `MapGridRuntimeOverrideModule`：单张地图的动态覆盖数据。
- `LruCache<TKey, TValue>`：通用 LRU 缓存容器。

## Editor Bake

1. 在地图场景根节点挂 `MapGridBakeSource`。
2. `MapId` 使用 `[WSScene]`，当前约定等于场景名。
3. 配置参与 Bake 的 Tilemap layers 和每层对应的 `MapGridCellFlags`。
4. 点击 `Bake Map Grid Data` 写入 `MapGridData_SO`。

不同 Tilemap 的 bounds 可以不一致。Bake 时会合并所有 `affectsBounds == true` 的图层 bounds：

```text
originCell = combinedBounds.min
width = combinedBounds.size.x
height = combinedBounds.size.y
gridX = cell.x - originCell.x
gridY = cell.y - originCell.y
index = gridY * width + gridX
```

运行时查询以 `Vector3Int cellPosition` 为权威坐标，`gridX/gridY` 只是数组索引。

## 运行时加载

地图场景中放一个 `MapGridRuntimeLoader`：

- `mapId`：当前场景地图 ID，使用 `[WSScene]` 选择。
- `grid`：当前场景的 `Grid`，Inspector 手动拖拽。

启用时：

```text
MapGridRuntimeLoader.OnEnable
-> MapGridManager.Instance.LoadCurrentMapAsync(mapId, grid)
-> MapGridDatabase 通过 MapGridCatalog_SO 查找 resourceKey
-> ResSystem/Addressables 加载 MapGridData_SO
-> 绑定为当前地图并 pin 住
```

运行时不再从 Loader 直接引用 `MapGridData_SO`，也不再有 SO fallback。所有运行时地图数据都必须经过：

```text
mapId -> MapGridCatalog_SO -> Addressables resourceKey -> MapGridData_SO
```

如果该 `mapId` 已经在缓存中，Database 不会重建静态索引，也不会清空 runtime overrides，只会绑定为当前地图并 pin 住。

禁用时：

```text
MapGridRuntimeLoader.OnDisable
-> 如果当前地图来自自己
-> MapGridManager.Instance.UnloadCurrentMap()
```

`UnloadCurrentMap()` 只解除当前场景绑定和 current scene pin，不会立刻删除缓存和动态数据。真正删除由 LRU 后续决定。

## Catalog 与 Addressables

`MapGridCatalog_SO` 配置：

```text
entries:
  mapId       = 场景名 / 地图 ID
  resourceKey = Addressables key
  pinOnLoad   = 加载后是否常驻
maxCachedMaps = 最大可淘汰地图缓存数
```

当前地图资源约定：

```text
01_MainScene -> 01_MainSceneMapGridData
02_Home_01   -> 02_Home_01MapGridData
```

`resourceKey` 必须匹配 Addressables 中 `MapGrid` group 的 address。Bake 生成或更新 SO 后，需要保证对应 SO 已加入 Addressables。

## 多地图缓存

`MapGridDatabase` 通过 `MapGridMapCache` 同时持有多张地图：

```text
pinnedMaps
-> 当前场景地图或 Catalog 常驻地图，不参与 LRU 淘汰

lruMaps
-> 可淘汰地图，由 LruCache<string, MapGridMapState> 管理
```

规则：

- 当前场景地图通过 `pinFromCurrentScene` 进入 pinned。
- Catalog 中 `pinOnLoad` 的地图通过 `pinFromCatalog` 进入 pinned。
- Pinned 地图不占用 `maxCachedMaps`，也不会进入 LRU。
- 未 pinned 的地图进入 LRU，最多保留 `maxCachedMaps` 张。
- LRU 超容量时淘汰最近最少使用的地图。

## 查询接口

当前地图查询：

```csharp
MapGridManager mapGrid = MapGridManager.Instance;
Vector3Int cell = mapGrid.WorldToCell(worldPosition);

if (mapGrid.TryGetCell(cell, out MapGridCellInfo info))
{
    bool canDig = (info.FinalFlags & MapGridCellFlags.CanDig) != 0;
    bool walkable = mapGrid.IsWalkable(cell);
    Vector3 center = mapGrid.GetCellCenterWorld(cell);
}
```

指定地图查询：

```csharp
string mapId = "02_Home_01";

if (await MapGridManager.Instance.EnsureLoadedAsync(mapId))
{
    bool walkable = MapGridManager.Instance.IsWalkable(mapId, targetCell);
}
```

指定地图查询不依赖 `Grid`，适合 NPC 离线移动、跨场景路径规划和日程模拟。世界坐标转换仍然只适用于当前场景，因为它需要当前场景真实的 `Grid`。

## Runtime Overrides

静态 SO 不保存家具、建筑、作物、NPC 临时阻挡等运行时状态。业务系统保存自己的持久化数据，并在地图加载后把影响写入 `MapGridManager`。

当前地图写入：

```csharp
MapGridManager.Instance.TryApplyOverride(
    sourceId: "Furniture:chair_001",
    cells: new[] { furnitureCell },
    addFlags: MapGridCellFlags.Blocked);
```

指定地图写入：

```csharp
var record = new MapGridRuntimeOverrideRecord
{
    mapId = "02_Home_01",
    sourceId = "Furniture:chair_001",
    occupiedCells = new List<Vector3Int> { furnitureCell },
    addFlags = MapGridCellFlags.Blocked,
    removeFlags = MapGridCellFlags.None
};

MapGridManager.Instance.TryApplyOverride(record);
```

规则：

- `record.mapId` 必须非空。
- 第一版只允许对已加载地图写 override。
- `sourceId` 建议使用 `SystemName:instanceId`，例如 `Furniture:chair_001`。
- 清理一个 `sourceId` 不会影响其他系统写入的覆盖。

## 注意事项

- `MapGridData_SO` 只保存逻辑数据，不保存世界坐标或 `Grid`。
- 世界坐标和 cell 坐标转换统一走当前场景 `Grid` 或 `MapGridManager`，不要手算。
- `Walkable` 不作为静态 flag 存储，而是由 `Blocked | Water | NpcObstacle` 等阻挡 flags 推导。
- Bake 相关类型被 `#if UNITY_EDITOR` 包裹，只服务 Editor authoring，不进入 Player Build。
