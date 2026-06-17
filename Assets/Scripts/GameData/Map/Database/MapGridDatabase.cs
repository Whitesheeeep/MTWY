using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using WS_Modules.ResLoadModule;

namespace GameData
{
    /// <summary>
    /// Multi-map data aggregate. Pinned maps are kept outside the LRU cache; unpinned maps are cached by LRU.
    /// </summary>
    public sealed class MapGridDatabase : IMapGridDatabase
    {
        private static readonly Vector3Int[] FourDirections =
        {
            Vector3Int.up,
            Vector3Int.right,
            Vector3Int.down,
            Vector3Int.left
        };

        private static readonly Vector3Int[] EightDirections =
        {
            Vector3Int.up,
            Vector3Int.right,
            Vector3Int.down,
            Vector3Int.left,
            new Vector3Int(1, 1, 0),
            new Vector3Int(1, -1, 0),
            new Vector3Int(-1, -1, 0),
            new Vector3Int(-1, 1, 0)
        };

        private readonly MapGridMapCache mapCache;
        private readonly MapGridCatalog_SO catalog;

        private string currentMapId = string.Empty;
        private Grid currentGrid;

        public MapGridDatabase(MapGridCatalog_SO catalog = null)
        {
            this.catalog = catalog;
            mapCache = new MapGridMapCache(GetMaxCachedMaps());
            mapCache.CapacityExceeded += TrimLruCacheIfNeeded;
        }

        public string CurrentMapId => currentMapId;
        public MapGridData_SO CurrentMapData => TryGetCurrentState(out MapGridMapState state)
            ? state.staticModule.LoadedMapData
            : null;
        public Grid CurrentGrid => currentGrid;
        public bool HasCurrentGrid => currentGrid != null;

        public async UniTask<bool> EnsureLoadedAsync(string mapId)
        {
            if (string.IsNullOrWhiteSpace(mapId))
            {
                Debug.LogWarning("[MapGridDatabase] Cannot load map because mapId is empty.");
                return false;
            }

            if (mapCache.ContainsPinned(mapId))
            {
                return true;
            }

            if (mapCache.TryGet(mapId, out _))
            {
                return true;
            }

            if (!TryGetCatalogEntry(mapId, out MapGridCatalogEntry entry))
            {
                Debug.LogWarning($"[MapGridDatabase] MapGridCatalog has no entry for mapId '{mapId}'.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(entry.resourceKey))
            {
                Debug.LogWarning($"[MapGridDatabase] MapGridCatalog entry '{mapId}' has empty resourceKey.");
                return false;
            }

            MapGridData_SO mapData;
            try
            {
                mapData = await ResSystem.Instance.LoadAsync<MapGridData_SO>(entry.resourceKey);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }

            if (mapData == null)
            {
                Debug.LogWarning($"[MapGridDatabase] Failed to load MapGridData_SO. Map:{mapId}, Key:{entry.resourceKey}");
                return false;
            }

            MapGridMapState state = CreateState(mapData, entry.resourceKey, true, entry.pinOnLoad);
            StoreState(state);
            return true;
        }

        public bool IsLoaded(string mapId)
        {
            return !string.IsNullOrWhiteSpace(mapId) &&
                   mapCache.Contains(mapId);
        }

        public void LoadMap(MapGridData_SO mapData, Grid grid)
        {
            if (mapData == null)
            {
                throw new ArgumentNullException(nameof(mapData));
            }

            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            string mapId = mapData.mapId;
            if (string.IsNullOrWhiteSpace(mapId))
            {
                throw new ArgumentException($"[MapGridDatabase] MapGridData has empty mapId: {mapData.name}", nameof(mapData));
            }

            MapGridMapState state;
            if (mapCache.TryGetPinned(mapId, out state))
            {
                state.pinFromCurrentScene = true;
            }
            else if (mapCache.TryRemoveLru(mapId, out state))
            {
                state.pinFromCurrentScene = true;
                mapCache.SetPinned(state);
            }
            else
            {
                TryGetCatalogEntry(mapId, out MapGridCatalogEntry entry);
                state = CreateState(mapData, entry.resourceKey, false, entry.pinOnLoad);
                state.pinFromCurrentScene = true;
                mapCache.SetPinned(state);
            }

            currentMapId = mapId;
            currentGrid = grid;
        }

        public void UnloadCurrentMap()
        {
            string mapId = currentMapId;
            if (!string.IsNullOrWhiteSpace(mapId))
            {
                if (mapCache.TryGetPinned(mapId, out MapGridMapState state))
                {
                    state.pinFromCurrentScene = false;
                    if (!state.IsPinned)
                    {
                        mapCache.LruCapacity = GetMaxCachedMaps();
                        mapCache.SetLru(state);
                    }
                }
                else if (mapCache.ContainsLru(mapId))
                {
                    Debug.LogError($"[MapGridDatabase] Current map '{mapId}' was found in LRU cache. Current maps must be pinned.");
                }
            }

            currentMapId = string.Empty;
            currentGrid = null;
        }

        public void ReloadCurrentMap()
        {
            if (!TryGetCurrentState(out MapGridMapState state) || currentGrid == null)
            {
                return;
            }

            state.staticModule.Load(state.staticModule.LoadedMapData);
        }

        public Vector3 GetCellCenterWorld(Vector3Int cell)
        {
            EnsureCurrentGrid();
            return currentGrid.GetCellCenterWorld(cell);
        }

        public Vector3Int WorldToCell(Vector3 worldPosition)
        {
            EnsureCurrentGrid();
            return currentGrid.WorldToCell(worldPosition);
        }

        public bool TryGetCell(Vector3Int cell, out MapGridCellInfo info)
        {
            return TryGetCell(CurrentMapId, cell, out info);
        }

        public bool TryGetCell(string mapId, Vector3Int cell, out MapGridCellInfo info)
        {
            info = default;
            if (!TryGetState(mapId, out MapGridMapState state) ||
                !state.staticModule.TryGetCellData(cell, out MapGridCellData cellData))
            {
                return false;
            }

            MapGridCellFlags finalFlags = state.overrideModule.Apply(cell, cellData.staticFlags);
            info = new MapGridCellInfo(cellData.cellPosition, cellData.gridX, cellData.gridY, cellData.staticFlags, finalFlags);
            return true;
        }

        public bool HasFlag(Vector3Int cell, MapGridCellFlags flag)
        {
            return HasFlag(CurrentMapId, cell, flag);
        }

        public bool HasFlag(string mapId, Vector3Int cell, MapGridCellFlags flag)
        {
            return TryGetCell(mapId, cell, out MapGridCellInfo info) && (info.FinalFlags & flag) == flag;
        }

        public bool IsWalkable(Vector3Int cell)
        {
            return IsWalkable(CurrentMapId, cell);
        }

        public bool IsWalkable(string mapId, Vector3Int cell)
        {
            if (!TryGetCell(mapId, cell, out MapGridCellInfo info))
            {
                return false;
            }

            const MapGridCellFlags BlockingFlags =
                MapGridCellFlags.Blocked | MapGridCellFlags.Water | MapGridCellFlags.NpcObstacle;
            return (info.FinalFlags & BlockingFlags) == MapGridCellFlags.None;
        }

        public IEnumerable<Vector3Int> GetNeighbors(Vector3Int cell, bool includeDiagonal = false)
        {
            return GetNeighbors(CurrentMapId, cell, includeDiagonal);
        }

        public IEnumerable<Vector3Int> GetNeighbors(string mapId, Vector3Int cell, bool includeDiagonal = false)
        {
            Vector3Int[] directions = includeDiagonal ? EightDirections : FourDirections;
            foreach (Vector3Int direction in directions)
            {
                Vector3Int neighbor = cell + direction;
                if (IsWalkable(mapId, neighbor))
                {
                    yield return neighbor;
                }
            }
        }

        public void SetRuntimeOverride(
            string sourceId,
            Vector3Int cell,
            MapGridCellFlags addFlags,
            MapGridCellFlags removeFlags = MapGridCellFlags.None)
        {
            SetRuntimeOverride(CurrentMapId, sourceId, cell, addFlags, removeFlags);
        }

        public void SetRuntimeOverride(
            string mapId,
            string sourceId,
            Vector3Int cell,
            MapGridCellFlags addFlags,
            MapGridCellFlags removeFlags = MapGridCellFlags.None)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException("[MapGridDatabase] Runtime override sourceId is empty.", nameof(sourceId));
            }

            if (!TryGetState(mapId, out MapGridMapState state))
            {
                Debug.LogWarning($"[MapGridDatabase] Runtime override ignored because map is not loaded. Map:{mapId}, Source:{sourceId}");
                return;
            }

            if (!state.staticModule.ContainsCell(cell))
            {
                Debug.LogWarning($"[MapGridDatabase] Runtime override ignored because cell is outside map. Map:{mapId}, Source:{sourceId}, Cell:{cell}");
                return;
            }

            state.overrideModule.SetOverride(sourceId, cell, addFlags, removeFlags);
        }

        public void ClearRuntimeOverride(string sourceId, Vector3Int cell)
        {
            ClearRuntimeOverride(CurrentMapId, sourceId, cell);
        }

        public void ClearRuntimeOverride(string mapId, string sourceId, Vector3Int cell)
        {
            if (TryGetState(mapId, out MapGridMapState state))
            {
                state.overrideModule.ClearOverride(sourceId, cell);
            }
        }

        public void ClearRuntimeOverrides(string sourceId)
        {
            ClearRuntimeOverrides(CurrentMapId, sourceId);
        }

        public void ClearRuntimeOverrides(string mapId, string sourceId)
        {
            if (TryGetState(mapId, out MapGridMapState state))
            {
                state.overrideModule.ClearOverrides(sourceId);
            }
        }

        public void ClearAllRuntimeOverrides()
        {
            ClearAllRuntimeOverrides(CurrentMapId);
        }

        public void ClearAllRuntimeOverrides(string mapId)
        {
            if (TryGetState(mapId, out MapGridMapState state))
            {
                state.overrideModule.ClearAll();
            }
        }

        public void Clear()
        {
            foreach (MapGridMapState state in mapCache.PinnedStates)
            {
                UnloadStateResource(state);
            }

            while (mapCache.TryRemoveLeastRecentlyUsed(out _, out MapGridMapState state))
            {
                UnloadStateResource(state);
            }

            mapCache.Clear();
            currentMapId = string.Empty;
            currentGrid = null;
        }

        private MapGridMapState CreateState(
            MapGridData_SO mapData,
            string resourceKey,
            bool loadedFromCatalog,
            bool pinFromCatalog)
        {
            var state = new MapGridMapState
            {
                mapId = mapData.mapId,
                resourceKey = resourceKey,
                loadedFromCatalog = loadedFromCatalog,
                staticModule = new MapGridStaticModule(),
                overrideModule = new MapGridRuntimeOverrideModule(),
                pinFromCatalog = pinFromCatalog
            };

            state.staticModule.Load(mapData);
            return state;
        }

        private void StoreState(MapGridMapState state)
        {
            mapCache.LruCapacity = GetMaxCachedMaps();
            mapCache.Store(state);
        }

        private void TrimLruCacheIfNeeded(MapGridMapCache cache)
        {
            if (cache.LruCapacity <= 0)
            {
                return;
            }

            while (cache.LruCount > cache.LruCapacity)
            {
                if (!cache.TryRemoveLeastRecentlyUsed(out string mapId, out MapGridMapState state))
                {
                    return;
                }

                if (string.Equals(mapId, currentMapId, StringComparison.Ordinal))
                {
                    Debug.LogError($"[MapGridDatabase] Current map '{mapId}' was evicted from LRU. Current maps must never be stored in LRU.");
                }

                UnloadStateResource(state);
            }
        }

        private void UnloadStateResource(MapGridMapState state)
        {
            if (state.loadedFromCatalog && !string.IsNullOrWhiteSpace(state.resourceKey))
            {
                ResSystem.Instance.UnLoad<MapGridData_SO>(state.resourceKey);
            }
        }

        private bool TryGetCurrentState(out MapGridMapState state)
        {
            return TryGetState(CurrentMapId, out state);
        }

        private bool TryGetState(string mapId, out MapGridMapState state)
        {
            if (string.IsNullOrWhiteSpace(mapId))
            {
                state = null;
                return false;
            }

            return mapCache.TryGet(mapId, out state);
        }

        private bool TryGetCatalogEntry(string mapId, out MapGridCatalogEntry entry)
        {
            if (catalog != null && catalog.entries != null)
            {
                for (int i = 0; i < catalog.entries.Count; i++)
                {
                    MapGridCatalogEntry candidate = catalog.entries[i];
                    if (string.Equals(candidate.mapId, mapId, StringComparison.Ordinal))
                    {
                        entry = candidate;
                        return true;
                    }
                }
            }

            entry = default;
            return false;
        }

        private int GetMaxCachedMaps()
        {
            return catalog != null && catalog.maxCachedMaps > 0 ? catalog.maxCachedMaps : 4;
        }

        private void EnsureCurrentGrid()
        {
            if (currentGrid == null)
            {
                throw new InvalidOperationException("[MapGridDatabase] CurrentGrid is null. Load a map with a Grid before converting coordinates.");
            }
        }

#if UNITY_EDITOR
        #region Editor Debug

        public IReadOnlyList<MapGridLoadedMapDebugInfo> GetLoadedMapDebugInfos()
        {
            var results = new List<MapGridLoadedMapDebugInfo>(mapCache.Count);
            foreach (MapGridMapState state in mapCache.PinnedStates)
            {
                results.Add(CreateDebugInfo(state, "Pinned"));
            }

            foreach (MapGridMapState state in mapCache.LruStates)
            {
                results.Add(CreateDebugInfo(state, "LRU"));
            }

            return results;
        }

        public bool EditorLoadMapDataForTest(MapGridData_SO mapData)
        {
            if (mapData == null || !mapData.IsValid)
            {
                return false;
            }

            string mapId = mapData.mapId;
            if (string.IsNullOrWhiteSpace(mapId))
            {
                return false;
            }

            if (mapCache.Contains(mapId))
            {
                return true;
            }

            MapGridMapState state = CreateState(mapData, string.Empty, false, false);
            StoreState(state);
            return true;
        }

        private static MapGridLoadedMapDebugInfo CreateDebugInfo(MapGridMapState state, string cacheKind)
        {
            MapGridStaticModule staticModule = state.staticModule;
            MapGridData_SO mapData = staticModule.LoadedMapData;
            return new MapGridLoadedMapDebugInfo(
                state.mapId,
                cacheKind,
                mapData != null ? mapData.name : string.Empty,
                state.resourceKey,
                state.loadedFromCatalog,
                state.pinFromCurrentScene,
                state.pinFromCatalog,
                staticModule.OriginCell,
                staticModule.Width,
                staticModule.Height,
                staticModule.Cells != null ? staticModule.Cells.Length : 0,
                state.overrideModule.OverrideCellCount,
                state.overrideModule.OverrideRecordCount);
        }

        #endregion
#endif
    }
}
