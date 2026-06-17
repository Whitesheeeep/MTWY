using System.Collections.Generic;
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// Owns runtime flag overrides for cells on the currently loaded map.
    /// </summary>
    public sealed class MapGridRuntimeOverrideModule
    {
        private readonly Dictionary<Vector3Int, List<MapGridRuntimeOverride>> runtimeOverrides =
            new Dictionary<Vector3Int, List<MapGridRuntimeOverride>>();

        public void SetOverride(
            string sourceId,
            Vector3Int cell,
            MapGridCellFlags addFlags,
            MapGridCellFlags removeFlags)
        {
            if (!runtimeOverrides.TryGetValue(cell, out List<MapGridRuntimeOverride> overrides))
            {
                overrides = new List<MapGridRuntimeOverride>();
                runtimeOverrides.Add(cell, overrides);
            }

            overrides.RemoveAll(item => item.SourceId == sourceId);
            overrides.Add(new MapGridRuntimeOverride(sourceId, addFlags, removeFlags));
        }

        public void ClearOverride(string sourceId, Vector3Int cell)
        {
            if (string.IsNullOrWhiteSpace(sourceId) ||
                !runtimeOverrides.TryGetValue(cell, out List<MapGridRuntimeOverride> overrides))
            {
                return;
            }

            overrides.RemoveAll(item => item.SourceId == sourceId);
            if (overrides.Count == 0)
            {
                runtimeOverrides.Remove(cell);
            }
        }

        public void ClearOverrides(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                return;
            }

            List<Vector3Int> emptyCells = null;
            foreach (KeyValuePair<Vector3Int, List<MapGridRuntimeOverride>> pair in runtimeOverrides)
            {
                pair.Value.RemoveAll(item => item.SourceId == sourceId);
                if (pair.Value.Count == 0)
                {
                    emptyCells ??= new List<Vector3Int>();
                    emptyCells.Add(pair.Key);
                }
            }

            if (emptyCells == null)
            {
                return;
            }

            foreach (Vector3Int cell in emptyCells)
            {
                runtimeOverrides.Remove(cell);
            }
        }

        public void ClearAll()
        {
            runtimeOverrides.Clear();
        }

        public MapGridCellFlags Apply(Vector3Int cell, MapGridCellFlags staticFlags)
        {
            if (!runtimeOverrides.TryGetValue(cell, out List<MapGridRuntimeOverride> overrides))
            {
                return staticFlags;
            }

            MapGridCellFlags finalFlags = staticFlags;
            foreach (MapGridRuntimeOverride runtimeOverride in overrides)
            {
                finalFlags |= runtimeOverride.AddFlags;
                finalFlags &= ~runtimeOverride.RemoveFlags;
            }

            return finalFlags;
        }

#if UNITY_EDITOR
        #region Editor Debug

        public int OverrideCellCount => runtimeOverrides.Count;

        public int OverrideRecordCount
        {
            get
            {
                int count = 0;
                foreach (KeyValuePair<Vector3Int, List<MapGridRuntimeOverride>> pair in runtimeOverrides)
                {
                    count += pair.Value.Count;
                }

                return count;
            }
        }

        #endregion
#endif
    }
}
