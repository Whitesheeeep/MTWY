using System.Collections.Generic;
using UnityEngine;

namespace FarmSystem
{
    /// <summary>
    /// 维护农田土地的耕地状态。
    /// </summary>
    public sealed class FarmSoilModule
    {
        // 只记录“某张地图的某个格子是否已经被耕作”。
        // 不负责 Tile 表现、地表贴图、粒子或可视化对象创建。
        private readonly Dictionary<string, HashSet<Vector3Int>> tilledCellsByMapId =
            new Dictionary<string, HashSet<Vector3Int>>();

        public bool IsTilled(string mapId, Vector3Int cell)
        {
            return tilledCellsByMapId.TryGetValue(mapId, out HashSet<Vector3Int> cells) &&
                   cells.Contains(cell);
        }

        public IEnumerable<Vector3Int> GetTilledCells(string mapId)
        {
            return tilledCellsByMapId.TryGetValue(mapId, out HashSet<Vector3Int> cells)
                ? cells
                : System.Array.Empty<Vector3Int>();
        }

        public bool TryTill(string mapId, Vector3Int cell)
        {
            HashSet<Vector3Int> cells = GetOrCreateSet(mapId);
            if (cells.Contains(cell))
            {
                return false;
            }

            cells.Add(cell);
            return true;
        }

        public bool ClearTill(string mapId, Vector3Int cell)
        {
            if (!tilledCellsByMapId.TryGetValue(mapId, out HashSet<Vector3Int> cells))
            {
                return false;
            }

            bool removed = cells.Remove(cell);
            if (cells.Count == 0)
            {
                tilledCellsByMapId.Remove(mapId);
            }

            return removed;
        }

        private HashSet<Vector3Int> GetOrCreateSet(string mapId)
        {
            if (!tilledCellsByMapId.TryGetValue(mapId, out HashSet<Vector3Int> cells))
            {
                cells = new HashSet<Vector3Int>();
                tilledCellsByMapId.Add(mapId, cells);
            }

            return cells;
        }
    }
}
