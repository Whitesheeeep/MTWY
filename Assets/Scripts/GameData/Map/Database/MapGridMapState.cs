namespace GameData
{
    public readonly struct MapGridLoadedMapDebugInfo
    {
        public MapGridLoadedMapDebugInfo(
            string mapId,
            string cacheKind,
            string assetName,
            string resourceKey,
            bool loadedFromCatalog,
            bool pinFromCurrentScene,
            bool pinFromCatalog,
            UnityEngine.Vector3Int originCell,
            int width,
            int height,
            int cellCount,
            int overrideCellCount,
            int overrideRecordCount)
        {
            MapId = mapId;
            CacheKind = cacheKind;
            AssetName = assetName;
            ResourceKey = resourceKey;
            LoadedFromCatalog = loadedFromCatalog;
            PinFromCurrentScene = pinFromCurrentScene;
            PinFromCatalog = pinFromCatalog;
            OriginCell = originCell;
            Width = width;
            Height = height;
            CellCount = cellCount;
            OverrideCellCount = overrideCellCount;
            OverrideRecordCount = overrideRecordCount;
        }

        public string MapId { get; }
        public string CacheKind { get; }
        public string AssetName { get; }
        public string ResourceKey { get; }
        public bool LoadedFromCatalog { get; }
        public bool PinFromCurrentScene { get; }
        public bool PinFromCatalog { get; }
        public UnityEngine.Vector3Int OriginCell { get; }
        public int Width { get; }
        public int Height { get; }
        public int CellCount { get; }
        public int OverrideCellCount { get; }
        public int OverrideRecordCount { get; }
    }

    public sealed class MapGridMapState
    {
        public string mapId;
        public string resourceKey;
        public bool loadedFromCatalog;
        public MapGridStaticModule staticModule;
        public MapGridRuntimeOverrideModule overrideModule;
        public bool pinFromCurrentScene;
        public bool pinFromCatalog;

        public bool IsPinned => pinFromCurrentScene || pinFromCatalog;
    }
}
