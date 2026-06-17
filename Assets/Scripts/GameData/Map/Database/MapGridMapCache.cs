using System;
using System.Collections.Generic;
using WS_Modules.DataStructure;

namespace GameData
{
    /// <summary>
    /// MapGrid runtime map cache that keeps pinned maps and evictable LRU maps behind one API.
    /// </summary>
    public sealed class MapGridMapCache
    {
        private readonly Dictionary<string, MapGridMapState> pinnedMaps =
            new Dictionary<string, MapGridMapState>(StringComparer.Ordinal);

        private readonly LruCache<string, MapGridMapState> lruMaps;

        public MapGridMapCache(int lruCapacity)
        {
            lruMaps = new LruCache<string, MapGridMapState>(lruCapacity);
            lruMaps.CapacityExceeded += _ => CapacityExceeded?.Invoke(this);
        }

        public event Action<MapGridMapCache> CapacityExceeded;

        public int LruCapacity
        {
            get => lruMaps.Capacity;
            set => lruMaps.Capacity = value;
        }

        public int PinnedCount => pinnedMaps.Count;
        public int LruCount => lruMaps.Count;
        public int Count => PinnedCount + LruCount;

        public IEnumerable<MapGridMapState> PinnedStates => pinnedMaps.Values;

        public bool Contains(string mapId)
        {
            return ContainsPinned(mapId) || ContainsLru(mapId);
        }

        public bool ContainsPinned(string mapId)
        {
            return !string.IsNullOrWhiteSpace(mapId) && pinnedMaps.ContainsKey(mapId);
        }

        public bool ContainsLru(string mapId)
        {
            return !string.IsNullOrWhiteSpace(mapId) && lruMaps.Contains(mapId);
        }

        public bool TryGet(string mapId, out MapGridMapState state)
        {
            if (string.IsNullOrWhiteSpace(mapId))
            {
                state = null;
                return false;
            }

            if (pinnedMaps.TryGetValue(mapId, out state))
            {
                return true;
            }

            return lruMaps.TryGet(mapId, out state);
        }

        public bool TryGetPinned(string mapId, out MapGridMapState state)
        {
            if (string.IsNullOrWhiteSpace(mapId))
            {
                state = null;
                return false;
            }

            return pinnedMaps.TryGetValue(mapId, out state);
        }

        public bool TryRemoveLru(string mapId, out MapGridMapState state)
        {
            if (string.IsNullOrWhiteSpace(mapId) || !lruMaps.TryPeek(mapId, out state))
            {
                state = null;
                return false;
            }

            lruMaps.Remove(mapId);
            return true;
        }

        public bool RemovePinned(string mapId)
        {
            return !string.IsNullOrWhiteSpace(mapId) && pinnedMaps.Remove(mapId);
        }

        public void Store(MapGridMapState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.IsPinned)
            {
                SetPinned(state);
                return;
            }

            SetLru(state);
        }

        public void SetPinned(MapGridMapState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            lruMaps.Remove(state.mapId);
            pinnedMaps[state.mapId] = state;
        }

        public void SetLru(MapGridMapState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            pinnedMaps.Remove(state.mapId);
            lruMaps.Set(state.mapId, state);
        }

        public bool TryRemoveLeastRecentlyUsed(out string mapId, out MapGridMapState state)
        {
            return lruMaps.TryRemoveLeastRecentlyUsed(out mapId, out state);
        }

        public void Clear()
        {
            pinnedMaps.Clear();
            lruMaps.Clear();
        }

#if UNITY_EDITOR
        #region Editor Debug

        public IEnumerable<MapGridMapState> LruStates
        {
            get
            {
                foreach (KeyValuePair<string, MapGridMapState> pair in lruMaps.EnumerateMostRecentlyUsed())
                {
                    yield return pair.Value;
                }
            }
        }

        #endregion
#endif
    }
}
