namespace GameData
{
    /// <summary>
    /// 单个系统对某个格子的运行时覆盖。
    /// </summary>
    public readonly struct MapGridRuntimeOverride
    {
        /// <summary>
        /// 创建一条运行时覆盖记录。
        /// </summary>
        public MapGridRuntimeOverride(string sourceId, MapGridCellFlags addFlags, MapGridCellFlags removeFlags)
        {
            SourceId = sourceId;
            AddFlags = addFlags;
            RemoveFlags = removeFlags;
        }

        /// <summary>
        /// 覆盖来源 ID，例如 Furniture:chair_001 或 Crop:wheat_002。
        /// </summary>
        public string SourceId { get; }

        /// <summary>
        /// 查询时要追加到格子最终属性中的 flags。
        /// </summary>
        public MapGridCellFlags AddFlags { get; }

        /// <summary>
        /// 查询时要从格子最终属性中移除的 flags。
        /// </summary>
        public MapGridCellFlags RemoveFlags { get; }
    }
}
