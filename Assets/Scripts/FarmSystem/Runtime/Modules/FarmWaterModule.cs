using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmSystem
{
    /// <summary>
    /// 维护农田格子的湿润状态。存在记录即表示湿润，移除记录即表示干燥。
    /// </summary>
    public sealed class FarmWaterModule
    {
        // 只记录湿润格子的到期时间，不负责时间轮、Tile 表现、音效或动画。
        private readonly Dictionary<string, Dictionary<Vector3Int, FarmWaterState>> waterStatesByMapId =
            new Dictionary<string, Dictionary<Vector3Int, FarmWaterState>>();

        public bool IsWatered(string mapId, Vector3Int cell)
        {
            return waterStatesByMapId.TryGetValue(mapId, out Dictionary<Vector3Int, FarmWaterState> cells) &&
                   cells.ContainsKey(cell);
        }

        public bool TryGetWaterState(string mapId, Vector3Int cell, out FarmWaterState state)
        {
            if (waterStatesByMapId.TryGetValue(mapId, out Dictionary<Vector3Int, FarmWaterState> cells) &&
                cells.TryGetValue(cell, out state))
            {
                return true;
            }

            state = default;
            return false;
        }

        public IEnumerable<Vector3Int> GetWateredCells(string mapId)
        {
            return waterStatesByMapId.TryGetValue(mapId, out Dictionary<Vector3Int, FarmWaterState> cells)
                ? cells.Keys
                : Array.Empty<Vector3Int>();
        }

        public void SetWatered(string mapId, Vector3Int cell, long expireTotalMinutes)
        {
            Dictionary<Vector3Int, FarmWaterState> cells = GetOrCreateMap(mapId);
            cells[cell] = new FarmWaterState(mapId, cell, expireTotalMinutes);
        }

        public bool TryDrain(string mapId, Vector3Int cell, out FarmWaterState previousState)
        {
            previousState = default;
            if (!waterStatesByMapId.TryGetValue(mapId, out Dictionary<Vector3Int, FarmWaterState> cells) ||
                !cells.TryGetValue(cell, out previousState))
            {
                return false;
            }

            cells.Remove(cell);
            if (cells.Count == 0)
            {
                waterStatesByMapId.Remove(mapId);
            }

            return true;
        }

        public bool ClearWater(string mapId, Vector3Int cell)
        {
            return TryDrain(mapId, cell, out _);
        }

        private Dictionary<Vector3Int, FarmWaterState> GetOrCreateMap(string mapId)
        {
            if (!waterStatesByMapId.TryGetValue(mapId, out Dictionary<Vector3Int, FarmWaterState> cells))
            {
                cells = new Dictionary<Vector3Int, FarmWaterState>();
                waterStatesByMapId.Add(mapId, cells);
            }

            return cells;
        }
    }
}
