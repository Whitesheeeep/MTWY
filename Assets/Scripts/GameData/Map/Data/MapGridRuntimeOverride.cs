namespace GameData
{
    public readonly struct MapGridRuntimeOverride
    {
        public MapGridRuntimeOverride(string sourceId, MapGridCellFlags addFlags, MapGridCellFlags removeFlags)
        {
            SourceId = sourceId;
            AddFlags = addFlags;
            RemoveFlags = removeFlags;
        }

        public string SourceId { get; }
        public MapGridCellFlags AddFlags { get; }
        public MapGridCellFlags RemoveFlags { get; }
    }
}
