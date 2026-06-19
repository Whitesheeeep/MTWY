using System;
using System.Collections.Generic;
using UnityEngine;
using WS_Modules;

namespace GameData
{
    [CreateAssetMenu(fileName = "MapGridCatalog", menuName = "GameData/Map/Grid Catalog", order = 1)]
    public sealed class MapGridCatalog_SO : ScriptableObject
    {
        public List<MapGridCatalogEntry> entries = new List<MapGridCatalogEntry>();
        public int maxCachedMaps = 4;
    }

    [Serializable]
    public struct MapGridCatalogEntry
    {
        [WSScene] public string mapId;
        [WSAddressableKey("MapGrid", "SO")]
        public string resourceKey;
        public bool pinOnLoad;
    }
}
