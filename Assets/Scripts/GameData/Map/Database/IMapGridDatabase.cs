using System.Collections.Generic;
using UnityEngine;

namespace GameData
{
    public interface IMapGridDatabase : IGameSubDatabase
    {
        string CurrentMapId { get; }
        MapGridData_SO CurrentMapData { get; }

        void LoadMap(MapGridData_SO mapData);
        void UnloadCurrentMap();
        void ReloadCurrentMap();

        bool TryGetCell(Vector3Int cell, out MapGridCellInfo info);
        bool TryGetCell(string mapId, Vector3Int cell, out MapGridCellInfo info);
        bool HasFlag(Vector3Int cell, MapGridCellFlags flag);
        bool HasFlag(string mapId, Vector3Int cell, MapGridCellFlags flag);
        bool IsWalkable(Vector3Int cell);
        bool IsWalkable(string mapId, Vector3Int cell);
        IEnumerable<Vector3Int> GetNeighbors(Vector3Int cell, bool includeDiagonal = false);
        IEnumerable<Vector3Int> GetNeighbors(string mapId, Vector3Int cell, bool includeDiagonal = false);

        void SetRuntimeOverride(string sourceId, Vector3Int cell, MapGridCellFlags addFlags, MapGridCellFlags removeFlags = MapGridCellFlags.None);
        void ClearRuntimeOverride(string sourceId, Vector3Int cell);
        void ClearRuntimeOverrides(string sourceId);
        void ClearAllRuntimeOverrides();
    }
}
