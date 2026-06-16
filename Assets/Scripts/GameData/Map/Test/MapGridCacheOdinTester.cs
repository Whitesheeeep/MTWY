#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 基于 Odin Inspector 的 MapGrid 多地图缓存手动测试组件，用于验证多张地图加载、缓存状态、指定 mapId 查询和 runtime override。
    /// </summary>
    public sealed class MapGridCacheOdinTester : MonoBehaviour
    {
        [Serializable]
        private sealed class TestMapEntry
        {
            [LabelText("测试 MapId")] public string mapId = "Test_Map";
            [LabelText("地图 SO")] public MapGridData_SO mapGridData;
            [LabelText("场景 Grid")] public Grid grid;
            [LabelText("示例 Cell")] public Vector3Int sampleCell;
        }

        [Title("单图测试")]
        [InfoBox("testMapId 是普通 string。点击同步按钮后会写入当前 SO 的 mapId，方便用任意字符串测试。")]
        [SerializeField] private string testMapId = "Test_Map_01";
        [SerializeField] private MapGridData_SO mapGridData;
        [SerializeField] private Grid grid;

        [Title("多图测试")]
        [SerializeField] private List<TestMapEntry> testMaps = new List<TestMapEntry>();
        [SerializeField] private bool keepLastLoadedMapAsCurrent;

        [Title("查询参数")]
        [SerializeField] private string queryMapId = "Test_Map_01";
        [SerializeField] private Vector3Int queryCell;
        [SerializeField] private bool includeDiagonalNeighbors;

        [Title("Override 参数")]
        [SerializeField] private string overrideMapId = "Test_Map_01";
        [SerializeField] private string overrideSourceId = "ManualTest:001";
        [SerializeField] private List<Vector3Int> overrideCells = new List<Vector3Int>();
        [SerializeField] private MapGridCellFlags addFlags = MapGridCellFlags.Blocked;
        [SerializeField] private MapGridCellFlags removeFlags = MapGridCellFlags.None;

        [Title("最近结果")]
        [ShowInInspector, ReadOnly, MultiLineProperty(12)] private string lastResult = "Ready.";

        /// <summary>
        /// 将单图测试 mapId 写入当前 MapGridData_SO。
        /// </summary>
        [Button("同步单图 MapId 到 SO", ButtonSizes.Large)]
        public void SyncSingleMapIdToSo()
        {
            if (!TrySyncMapIdToSo(testMapId, mapGridData, out string failureReason))
            {
                ReportFailure($"同步单图 MapId 到 SO 失败：{failureReason}");
                return;
            }

            queryMapId = testMapId;
            overrideMapId = testMapId;
            ReportSuccess($"已同步单图 mapId 到 SO。mapId={testMapId}, asset={mapGridData.name}");
        }

        /// <summary>
        /// 将多图列表中每个 entry 的 mapId 写入对应 MapGridData_SO。
        /// </summary>
        [Button("同步多图 MapId 到 SO")]
        public void SyncAllMapIdsToSo()
        {
            if (testMaps == null || testMaps.Count == 0)
            {
                ReportFailure("同步多图 MapId 到 SO 失败：testMaps 为空。");
                return;
            }

            var builder = new StringBuilder();
            int successCount = 0;
            for (int i = 0; i < testMaps.Count; i++)
            {
                TestMapEntry entry = testMaps[i];
                if (entry == null)
                {
                    builder.AppendLine($"[{i}] 失败：entry 为空。");
                    continue;
                }

                if (!TrySyncMapIdToSo(entry.mapId, entry.mapGridData, out string failureReason))
                {
                    builder.AppendLine($"[{i}] 失败：{failureReason}");
                    continue;
                }

                successCount++;
                builder.AppendLine($"[{i}] 成功：mapId={entry.mapId}, asset={entry.mapGridData.name}");
            }

            ReportSuccess($"同步多图 MapId 完成。success={successCount}/{testMaps.Count}\n{builder}");
        }

        /// <summary>
        /// 用单图字段加载当前地图。
        /// </summary>
        [Button("加载单图为当前地图", ButtonSizes.Large)]
        public void LoadSingleAsCurrentMap()
        {
            if (!TryValidateMapData(mapGridData, grid, out string failureReason))
            {
                ReportFailure($"加载单图失败：{failureReason}");
                return;
            }

            EnsureDatabaseRegistered();
            MapGridManager.Instance.LoadCurrentMap(mapGridData, grid);

            queryMapId = mapGridData.mapId;
            overrideMapId = mapGridData.mapId;
            ReportSuccess($"加载单图成功。mapId={mapGridData.mapId}, grid={grid.name}, loaded={MapGridManager.Instance.IsLoaded(mapGridData.mapId)}");
        }

        /// <summary>
        /// 按多图列表依次加载地图。默认每张加载后解除当前绑定，让它们留在缓存里。
        /// </summary>
        [Button("批量加载多图到缓存", ButtonSizes.Large)]
        public void LoadAllMapsIntoCache()
        {
            if (testMaps == null || testMaps.Count == 0)
            {
                ReportFailure("批量加载失败：testMaps 为空。");
                return;
            }

            EnsureDatabaseRegistered();
            var builder = new StringBuilder();
            int successCount = 0;

            for (int i = 0; i < testMaps.Count; i++)
            {
                TestMapEntry entry = testMaps[i];
                if (entry == null)
                {
                    builder.AppendLine($"[{i}] 失败：entry 为空。");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(entry.mapId))
                {
                    TrySyncMapIdToSo(entry.mapId, entry.mapGridData, out _);
                }

                if (!TryValidateMapData(entry.mapGridData, entry.grid, out string failureReason))
                {
                    builder.AppendLine($"[{i}] 失败：{failureReason}");
                    continue;
                }

                MapGridManager.Instance.LoadCurrentMap(entry.mapGridData, entry.grid);
                successCount++;
                builder.AppendLine($"[{i}] 加载：mapId={entry.mapGridData.mapId}, asset={entry.mapGridData.name}, grid={entry.grid.name}");

                bool shouldUnloadCurrent = !keepLastLoadedMapAsCurrent || i < testMaps.Count - 1;
                if (shouldUnloadCurrent)
                {
                    MapGridManager.Instance.UnloadCurrentMap();
                    builder.AppendLine($"[{i}] 已解除当前地图绑定，保留在缓存中。loaded={MapGridManager.Instance.IsLoaded(entry.mapGridData.mapId)}");
                }
            }

            builder.AppendLine();
            builder.Append(BuildLoadedMapSnapshotText());
            ReportSuccess($"批量加载多图完成。success={successCount}/{testMaps.Count}\n{builder}");
        }

        /// <summary>
        /// 使用 queryMapId 与 queryCell 查询指定地图的格子信息。
        /// </summary>
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

        /// <summary>
        /// 逐个查询多图列表中的 sampleCell，用于检查多张地图是否都在缓存里且数据正确。
        /// </summary>
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
                string mapId = ResolveEntryMapId(entry);
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

        /// <summary>
        /// 对 overrideMapId 写入一组 runtime override，并立即打印第一个格子的最终 flags。
        /// </summary>
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

        /// <summary>
        /// 清除 overrideSourceId 在 overrideMapId 上写入的所有 runtime override。
        /// </summary>
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

        /// <summary>
        /// 卸载当前地图绑定，验证当前地图会从 pinned 状态转入可淘汰缓存。
        /// </summary>
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

        /// <summary>
        /// 打印当前实际存储在 MapGridDatabase 中的地图缓存快照。
        /// </summary>
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

        /// <summary>
        /// 一键执行：同步多图 mapId、批量加载、打印缓存快照、查询 sample cell。
        /// </summary>
        [Button("一键执行多图流程", ButtonSizes.Large)]
        public void RunMultiMapFlow()
        {
            SyncAllMapIdsToSo();
            if (!IsLastResultSuccess())
            {
                return;
            }

            LoadAllMapsIntoCache();
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

        private static bool TrySyncMapIdToSo(string mapId, MapGridData_SO targetMapData, out string failureReason)
        {
            if (targetMapData == null)
            {
                failureReason = "mapGridData 未赋值";
                return false;
            }

            if (string.IsNullOrWhiteSpace(mapId))
            {
                failureReason = "mapId 为空";
                return false;
            }

            Undo.RecordObject(targetMapData, "Sync MapGrid Test MapId");
            targetMapData.mapId = mapId;
            EditorUtility.SetDirty(targetMapData);
            failureReason = string.Empty;
            return true;
        }

        private static bool TryValidateMapData(MapGridData_SO targetMapData, Grid targetGrid, out string failureReason)
        {
            if (targetMapData == null)
            {
                failureReason = "mapGridData 未赋值";
                return false;
            }

            if (targetGrid == null)
            {
                failureReason = "grid 未赋值";
                return false;
            }

            if (!targetMapData.IsValid)
            {
                failureReason = $"mapGridData 无效。asset={targetMapData.name}, mapId={targetMapData.mapId}, size={targetMapData.width}x{targetMapData.height}";
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

        private static void EnsureDatabaseRegistered()
        {
            if (GameDatabase.TryGet(out IMapGridDatabase _))
            {
                return;
            }

            GameDatabase.Register<IMapGridDatabase>(new MapGridDatabase());
        }

        private static string ResolveEntryMapId(TestMapEntry entry)
        {
            if (entry == null)
            {
                return string.Empty;
            }

            return !string.IsNullOrWhiteSpace(entry.mapId)
                ? entry.mapId
                : entry.mapGridData != null
                    ? entry.mapGridData.mapId
                    : string.Empty;
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
                builder.AppendLine($"当前数据库类型不是 MapGridDatabase，无法读取 debug 快照。type={database.GetType().Name}");
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
#endif
