#if UNITY_EDITOR
#region MapGrid Cache Odin Tester

using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 基于 Odin Inspector 的 MapGrid 多地图缓存手动测试组件。
    /// 测试加载路径与运行时保持一致：mapId -> Catalog -> AddressablesKey -> MapGridData_SO。
    /// </summary>
    public sealed class MapGridCacheOdinTester : MonoBehaviour
    {
        [Serializable]
        private sealed class TestMapEntry
        {
            [LabelText("地图 MapId")] public string mapId = "01_MainScene";
            [LabelText("场景 Grid，可选")] public Grid grid;
            [LabelText("示例 Cell")] public Vector3Int sampleCell;
        }

        [Title("单图测试")]
        [SerializeField] private MapGridCatalog_SO catalog;
        [SerializeField] private string testMapId = "01_MainScene";
        [SerializeField] private Grid grid;

        [Title("多图测试")]
        [SerializeField] private List<TestMapEntry> testMaps = new List<TestMapEntry>();
        [SerializeField] private bool keepLastLoadedMapAsCurrent;

        [Title("查询参数")]
        [SerializeField] private string queryMapId = "01_MainScene";
        [SerializeField] private Vector3Int queryCell;
        [SerializeField] private bool includeDiagonalNeighbors;

        [Title("Override 参数")]
        [SerializeField] private string overrideMapId = "01_MainScene";
        [SerializeField] private string overrideSourceId = "ManualTest:001";
        [SerializeField] private List<Vector3Int> overrideCells = new List<Vector3Int>();
        [SerializeField] private MapGridCellFlags addFlags = MapGridCellFlags.Blocked;
        [SerializeField] private MapGridCellFlags removeFlags = MapGridCellFlags.None;

        [Title("最近结果")]
        [ShowInInspector, ReadOnly, MultiLineProperty(12)] private string lastResult = "Ready.";

        [Button("加载单图为当前地图", ButtonSizes.Large)]
        public void LoadSingleAsCurrentMap()
        {
            LoadSingleAsCurrentMapAsync().Forget();
        }

        [Button("批量加载多图到缓存", ButtonSizes.Large)]
        public void LoadAllMapsIntoCache()
        {
            LoadAllMapsIntoCacheAsync().Forget();
        }

        [Button("查询指定 MapId Cell", ButtonSizes.Large)]
        public void QueryCellByMapId()
        {
            if (!EnsureDatabaseReady(out string failureReason))
            {
                ReportFailure($"查询指定 MapId Cell 失败：{failureReason}");
                return;
            }

            bool found = MapGridManager.Instance.TryGetCell(queryMapId, queryCell, out MapGridCellInfo info);
            if (!found)
            {
                ReportFailure($"查询失败：mapId={queryMapId}, cell={queryCell} 不存在或地图未加载。");
                return;
            }

            int neighborCount = CountNeighbors(queryMapId, queryCell, includeDiagonalNeighbors);
            ReportSuccess(
                $"查询成功。mapId={queryMapId}, cell={queryCell}, grid=({info.GridX},{info.GridY}), static={info.StaticFlags}, final={info.FinalFlags}, walkable={MapGridManager.Instance.IsWalkable(queryMapId, queryCell)}, neighbors={neighborCount}");
        }

        [Button("查询多图 Sample Cell")]
        public void QueryAllSampleCells()
        {
            if (!EnsureDatabaseReady(out string failureReason))
            {
                ReportFailure($"查询多图 Sample Cell 失败：{failureReason}");
                return;
            }

            if (testMaps == null || testMaps.Count == 0)
            {
                ReportFailure("查询多图 Sample Cell 失败：testMaps 为空。");
                return;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < testMaps.Count; i++)
            {
                TestMapEntry entry = testMaps[i];
                string mapId = entry != null ? entry.mapId : string.Empty;
                if (string.IsNullOrWhiteSpace(mapId))
                {
                    builder.AppendLine($"[{i}] 失败：mapId 为空。");
                    continue;
                }

                bool loaded = MapGridManager.Instance.IsLoaded(mapId);
                bool found = MapGridManager.Instance.TryGetCell(mapId, entry.sampleCell, out MapGridCellInfo info);
                builder.AppendLine(found
                    ? $"[{i}] OK mapId={mapId}, loaded={loaded}, cell={entry.sampleCell}, static={info.StaticFlags}, final={info.FinalFlags}, walkable={MapGridManager.Instance.IsWalkable(mapId, entry.sampleCell)}"
                    : $"[{i}] MISS mapId={mapId}, loaded={loaded}, cell={entry.sampleCell}");
            }

            ReportSuccess("多图 Sample Cell 查询完成。\n" + builder);
        }

        [Button("应用 Override 到指定 MapId", ButtonSizes.Large)]
        public void ApplyOverrideByMapId()
        {
            if (!EnsureDatabaseReady(out string failureReason))
            {
                ReportFailure($"应用 Override 失败：{failureReason}");
                return;
            }

            var record = new MapGridRuntimeOverrideRecord
            {
                mapId = overrideMapId,
                sourceId = overrideSourceId,
                occupiedCells = overrideCells,
                addFlags = addFlags,
                removeFlags = removeFlags
            };

            bool success = MapGridManager.Instance.TryApplyOverride(record);
            if (!success)
            {
                ReportFailure($"应用 Override 失败。mapId={overrideMapId}, sourceId={overrideSourceId}, cells={FormatCells(overrideCells)}");
                return;
            }

            Vector3Int firstCell = overrideCells[0];
            MapGridManager.Instance.TryGetCell(overrideMapId, firstCell, out MapGridCellInfo info);
            ReportSuccess($"应用 Override 成功。mapId={overrideMapId}, sourceId={overrideSourceId}, firstCell={firstCell}, final={info.FinalFlags}");
        }

        [Button("清除指定 MapId Override")]
        public void ClearOverrideByMapId()
        {
            if (!EnsureDatabaseReady(out string failureReason))
            {
                ReportFailure($"清除 Override 失败：{failureReason}");
                return;
            }

            MapGridManager.Instance.ClearOverrides(overrideMapId, overrideSourceId);
            ReportSuccess($"已清除 Override。mapId={overrideMapId}, sourceId={overrideSourceId}");
        }

        [Button("卸载当前地图并检查缓存")]
        public void UnloadCurrentAndCheckCache()
        {
            if (!EnsureDatabaseReady(out string failureReason))
            {
                ReportFailure($"卸载当前地图失败：{failureReason}");
                return;
            }

            string oldMapId = MapGridManager.Instance.CurrentMapId;
            MapGridManager.Instance.UnloadCurrentMap();

            bool stillLoaded = !string.IsNullOrWhiteSpace(oldMapId) && MapGridManager.Instance.IsLoaded(oldMapId);
            ReportSuccess($"已卸载当前地图绑定。oldMapId={oldMapId}, currentMapId={MapGridManager.Instance.CurrentMapId}, cacheStillLoaded={stillLoaded}\n{BuildLoadedMapSnapshotText()}");
        }

        [Button("打印已存储地图快照", ButtonSizes.Large)]
        public void PrintLoadedMapSnapshot()
        {
            if (!EnsureDatabaseReady(out string failureReason))
            {
                ReportFailure($"打印已存储地图快照失败：{failureReason}");
                return;
            }

            ReportSuccess(BuildLoadedMapSnapshotText());
        }

        [Button("一键执行多图流程", ButtonSizes.Large)]
        public void RunMultiMapFlow()
        {
            RunMultiMapFlowAsync().Forget();
        }

        private async UniTaskVoid LoadSingleAsCurrentMapAsync()
        {
            if (!TryValidateCurrentMapInput(testMapId, grid, out string failureReason))
            {
                ReportFailure($"加载单图失败：{failureReason}");
                return;
            }

            EnsureDatabaseRegistered(catalog);
            bool loaded = await MapGridManager.Instance.LoadCurrentMapAsync(testMapId, grid);
            if (!loaded)
            {
                ReportFailure($"加载单图失败：请检查 Catalog 和 Addressables。mapId={testMapId}");
                return;
            }

            queryMapId = testMapId;
            overrideMapId = testMapId;
            ReportSuccess($"加载单图成功。mapId={testMapId}, grid={grid.name}, loaded={MapGridManager.Instance.IsLoaded(testMapId)}");
        }

        private async UniTask LoadAllMapsIntoCacheAsync()
        {
            if (testMaps == null || testMaps.Count == 0)
            {
                ReportFailure("批量加载失败：testMaps 为空。");
                return;
            }

            EnsureDatabaseRegistered(catalog);
            var builder = new StringBuilder();
            int successCount = 0;

            for (int i = 0; i < testMaps.Count; i++)
            {
                TestMapEntry entry = testMaps[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.mapId))
                {
                    builder.AppendLine($"[{i}] 失败：entry 或 mapId 为空。");
                    continue;
                }

                bool loaded;
                if (entry.grid != null)
                {
                    loaded = await MapGridManager.Instance.LoadCurrentMapAsync(entry.mapId, entry.grid);
                }
                else
                {
                    loaded = await MapGridManager.Instance.EnsureLoadedAsync(entry.mapId);
                }

                if (!loaded)
                {
                    builder.AppendLine($"[{i}] 失败：加载失败，请检查 Catalog 和 Addressables。mapId={entry.mapId}");
                    continue;
                }

                successCount++;
                builder.AppendLine($"[{i}] 加载：mapId={entry.mapId}, grid={(entry.grid != null ? entry.grid.name : "none/static-only")}");

                bool shouldUnloadCurrent = entry.grid != null && (!keepLastLoadedMapAsCurrent || i < testMaps.Count - 1);
                if (shouldUnloadCurrent)
                {
                    MapGridManager.Instance.UnloadCurrentMap();
                    builder.AppendLine($"[{i}] 已解除当前地图绑定，保留在缓存中。loaded={MapGridManager.Instance.IsLoaded(entry.mapId)}");
                }
            }

            builder.AppendLine();
            builder.Append(BuildLoadedMapSnapshotText());
            ReportSuccess($"批量加载多图完成。success={successCount}/{testMaps.Count}\n{builder}");
        }

        private async UniTaskVoid RunMultiMapFlowAsync()
        {
            await LoadAllMapsIntoCacheAsync();
            if (!IsLastResultSuccess())
            {
                return;
            }

            QueryAllSampleCells();
        }

        private void OnValidate()
        {
            overrideCells ??= new List<Vector3Int>();
            testMaps ??= new List<TestMapEntry>();
        }

        private static bool TryValidateCurrentMapInput(string mapId, Grid targetGrid, out string failureReason)
        {
            if (string.IsNullOrWhiteSpace(mapId))
            {
                failureReason = "mapId 为空";
                return false;
            }

            if (targetGrid == null)
            {
                failureReason = "grid 未赋值";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        private static bool EnsureDatabaseReady(out string failureReason)
        {
            if (!GameDatabase.TryGet(out IMapGridDatabase _))
            {
                failureReason = "IMapGridDatabase 未注册。请先点击加载按钮，或通过 ConfigInstaller 初始化。";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        private static void EnsureDatabaseRegistered(MapGridCatalog_SO catalog)
        {
            if (GameDatabase.TryGet(out IMapGridDatabase _))
            {
                return;
            }

            GameDatabase.Register<IMapGridDatabase>(new MapGridDatabase(catalog));
        }

        private static int CountNeighbors(string mapId, Vector3Int cell, bool includeDiagonal)
        {
            int count = 0;
            foreach (Vector3Int _ in MapGridManager.Instance.GetNeighbors(mapId, cell, includeDiagonal))
            {
                count++;
            }

            return count;
        }

        private static string BuildLoadedMapSnapshotText()
        {
            if (!GameDatabase.TryGet(out IMapGridDatabase database))
            {
                return "IMapGridDatabase 未注册。";
            }

            var builder = new StringBuilder();
            builder.AppendLine("已存储地图快照：");
            builder.AppendLine($"CurrentMapId={MapGridManager.Instance.CurrentMapId}, HasCurrentGrid={MapGridManager.Instance.HasCurrentGrid}");

            if (database is not MapGridDatabase mapGridDatabase)
            {
                builder.AppendLine($"当前数据库类型不是 MapGridDatabase，无法读取 debug 快照。Type={database.GetType().Name}");
                return builder.ToString();
            }

            IReadOnlyList<MapGridLoadedMapDebugInfo> infos = mapGridDatabase.GetLoadedMapDebugInfos();
            builder.AppendLine($"LoadedMapCount={infos.Count}");
            for (int i = 0; i < infos.Count; i++)
            {
                MapGridLoadedMapDebugInfo info = infos[i];
                builder.AppendLine(
                    $"[{i}] {info.CacheKind} mapId={info.MapId}, asset={info.AssetName}, origin={info.OriginCell}, size={info.Width}x{info.Height}, cells={info.CellCount}, overrides={info.OverrideRecordCount}/{info.OverrideCellCount}, pinScene={info.PinFromCurrentScene}, pinCatalog={info.PinFromCatalog}, fromCatalog={info.LoadedFromCatalog}, key={info.ResourceKey}");
            }

            return builder.ToString();
        }

        private void ReportSuccess(string message)
        {
            lastResult = $"通过 | {message}";
            Debug.Log($"[MapGridCacheOdinTester] {lastResult}");
        }

        private void ReportFailure(string message)
        {
            lastResult = $"失败 | {message}";
            Debug.LogError($"[MapGridCacheOdinTester] {lastResult}");
        }

        private bool IsLastResultSuccess()
        {
            return lastResult.StartsWith("通过", StringComparison.Ordinal);
        }

        private static string FormatCells(IReadOnlyList<Vector3Int> cells)
        {
            if (cells == null || cells.Count == 0)
            {
                return "[]";
            }

            var builder = new StringBuilder();
            builder.Append('[');
            for (int i = 0; i < cells.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(cells[i]);
            }

            builder.Append(']');
            return builder.ToString();
        }
    }
}

#endregion
#endif
