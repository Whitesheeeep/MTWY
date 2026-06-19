using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using WS_Modules.Singleton;

namespace GameData
{
    /// <summary>
    /// Public facade for current-map grid loading, queries, coordinate conversion, and runtime overrides.
    /// </summary>
    public sealed class MapGridManager : SingletonBase<MapGridManager>
    {
        private MapGridManager()
        {
        }

        private IMapGridDatabase Database => GameDatabase.Get<IMapGridDatabase>();

        public string CurrentMapId => TryGetDatabase(out IMapGridDatabase database) ? database.CurrentMapId : string.Empty;
        public MapGridData_SO CurrentMapData => TryGetDatabase(out IMapGridDatabase database) ? database.CurrentMapData : null;
        public Grid CurrentGrid => TryGetDatabase(out IMapGridDatabase database) ? database.CurrentGrid : null;
        public bool HasCurrentGrid => TryGetDatabase(out IMapGridDatabase database) && database.HasCurrentGrid;
        public bool HasCurrentMap => TryGetDatabase(out IMapGridDatabase database) && database.CurrentMapData != null;

        public UniTask<bool> EnsureLoadedAsync(string mapId)
        {
            return TryGetDatabase(out IMapGridDatabase database)
                ? database.EnsureLoadedAsync(mapId)
                : UniTask.FromResult(false);
        }

        public bool IsLoaded(string mapId)
        {
            return TryGetDatabase(out IMapGridDatabase database) && database.IsLoaded(mapId);
        }

        public bool TryGetLoadedMapData(string mapId, out MapGridData_SO mapData)
        {
            if (TryGetDatabase(out IMapGridDatabase database))
            {
                return database.TryGetLoadedMapData(mapId, out mapData);
            }

            mapData = null;
            return false;
        }

        public bool TryGetMapCellSize(string mapId, out Vector3 cellSize)
        {
            if (TryGetDatabase(out IMapGridDatabase database))
            {
                return database.TryGetMapCellSize(mapId, out cellSize);
            }

            cellSize = Vector3.one;
            return false;
        }

        public UniTask<bool> LoadCurrentMapAsync(string mapId, Grid grid)
        {
            return TryGetDatabase(out IMapGridDatabase database)
                ? database.LoadCurrentMapAsync(mapId, grid)
                : UniTask.FromResult(false);
        }

        public void UnloadCurrentMap()
        {
            if (TryGetDatabase(out IMapGridDatabase database))
            {
                database.UnloadCurrentMap();
            }
        }

        public void ReloadCurrentMap()
        {
            Database.ReloadCurrentMap();
        }

        public Vector3 GetCellCenterWorld(Vector3Int cell)
        {
            return Database.GetCellCenterWorld(cell);
        }

        public Vector3Int WorldToCell(Vector3 worldPosition)
        {
            return Database.WorldToCell(worldPosition);
        }

        public bool TryGetCell(Vector3Int cell, out MapGridCellInfo info)
        {
            if (TryGetDatabase(out IMapGridDatabase database))
            {
                return database.TryGetCell(cell, out info);
            }

            info = default;
            return false;
        }

        public bool TryGetCell(string mapId, Vector3Int cell, out MapGridCellInfo info)
        {
            if (TryGetDatabase(out IMapGridDatabase database))
            {
                return database.TryGetCell(mapId, cell, out info);
            }

            info = default;
            return false;
        }

        public bool HasFlag(Vector3Int cell, MapGridCellFlags flag)
        {
            return TryGetDatabase(out IMapGridDatabase database) && database.HasFlag(cell, flag);
        }

        public bool HasFlag(string mapId, Vector3Int cell, MapGridCellFlags flag)
        {
            return TryGetDatabase(out IMapGridDatabase database) && database.HasFlag(mapId, cell, flag);
        }

        public bool IsWalkable(Vector3Int cell)
        {
            return TryGetDatabase(out IMapGridDatabase database) && database.IsWalkable(cell);
        }

        public bool IsWalkable(string mapId, Vector3Int cell)
        {
            return TryGetDatabase(out IMapGridDatabase database) && database.IsWalkable(mapId, cell);
        }

        public IEnumerable<Vector3Int> GetNeighbors(Vector3Int cell, bool includeDiagonal = false)
        {
            return TryGetDatabase(out IMapGridDatabase database)
                ? database.GetNeighbors(cell, includeDiagonal)
                : Array.Empty<Vector3Int>();
        }

        public IEnumerable<Vector3Int> GetNeighbors(string mapId, Vector3Int cell, bool includeDiagonal = false)
        {
            return TryGetDatabase(out IMapGridDatabase database)
                ? database.GetNeighbors(mapId, cell, includeDiagonal)
                : Array.Empty<Vector3Int>();
        }

        public bool TryApplyOverride(
            string sourceId,
            IReadOnlyList<Vector3Int> cells,
            MapGridCellFlags addFlags,
            MapGridCellFlags removeFlags = MapGridCellFlags.None)
        {
            if (!TryGetDatabase(out IMapGridDatabase database) || database.CurrentMapData == null)
            {
                Debug.LogWarning("[MapGridManager] Cannot apply runtime override because current map is not loaded.");
                return false;
            }

            if (!ValidateOverrideRequest(sourceId, cells))
            {
                return false;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                if (!database.TryGetCell(cells[i], out _))
                {
                    Debug.LogWarning($"[MapGridManager] Runtime override rejected because cell is outside current map. Source:{sourceId}, Cell:{cells[i]}");
                    return false;
                }
            }

            for (int i = 0; i < cells.Count; i++)
            {
                database.SetRuntimeOverride(sourceId, cells[i], addFlags, removeFlags);
            }

            return true;
        }

        public bool TryApplyOverride(MapGridRuntimeOverrideRecord record)
        {
            if (!TryGetDatabase(out IMapGridDatabase database))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(record.mapId))
            {
                Debug.LogWarning($"[MapGridManager] Runtime override rejected because mapId is empty. Source:{record.sourceId}");
                return false;
            }

            if (!database.IsLoaded(record.mapId))
            {
                Debug.LogWarning($"[MapGridManager] Runtime override rejected because map is not loaded. Map:{record.mapId}, Source:{record.sourceId}");
                return false;
            }

            if (!ValidateOverrideRequest(record.sourceId, record.occupiedCells))
            {
                return false;
            }

            for (int i = 0; i < record.occupiedCells.Count; i++)
            {
                Vector3Int cell = record.occupiedCells[i];
                if (!database.TryGetCell(record.mapId, cell, out _))
                {
                    Debug.LogWarning($"[MapGridManager] Runtime override rejected because cell is outside map. Map:{record.mapId}, Source:{record.sourceId}, Cell:{cell}");
                    return false;
                }
            }

            for (int i = 0; i < record.occupiedCells.Count; i++)
            {
                database.SetRuntimeOverride(
                    record.mapId,
                    record.sourceId,
                    record.occupiedCells[i],
                    record.addFlags,
                    record.removeFlags);
            }

            return true;
        }

        public void ClearOverride(string sourceId, Vector3Int cell)
        {
            if (TryGetDatabase(out IMapGridDatabase database))
            {
                database.ClearRuntimeOverride(sourceId, cell);
            }
        }

        public void ClearOverride(string mapId, string sourceId, Vector3Int cell)
        {
            if (TryGetDatabase(out IMapGridDatabase database))
            {
                database.ClearRuntimeOverride(mapId, sourceId, cell);
            }
        }

        public void ClearOverrides(string sourceId)
        {
            if (TryGetDatabase(out IMapGridDatabase database))
            {
                database.ClearRuntimeOverrides(sourceId);
            }
        }

        public void ClearOverrides(string mapId, string sourceId)
        {
            if (TryGetDatabase(out IMapGridDatabase database))
            {
                database.ClearRuntimeOverrides(mapId, sourceId);
            }
        }

        public void ClearAllOverrides()
        {
            if (TryGetDatabase(out IMapGridDatabase database))
            {
                database.ClearAllRuntimeOverrides();
            }
        }

        public void ClearAllOverrides(string mapId)
        {
            if (TryGetDatabase(out IMapGridDatabase database))
            {
                database.ClearAllRuntimeOverrides(mapId);
            }
        }

        private static bool TryGetDatabase(out IMapGridDatabase database)
        {
            return GameDatabase.TryGet(out database);
        }

        private static bool ValidateOverrideRequest(string sourceId, IReadOnlyList<Vector3Int> cells)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                Debug.LogWarning("[MapGridManager] Runtime override sourceId is empty.");
                return false;
            }

            if (cells == null || cells.Count == 0)
            {
                Debug.LogWarning($"[MapGridManager] Runtime override cells are empty. Source:{sourceId}");
                return false;
            }

            return true;
        }
    }
}
