using System.Collections.Generic;
using UnityEngine;

namespace GameData.Editor
{
    internal sealed class MapGridCheckerResult
    {
        public readonly List<MapGridCheckerReportItem> reports = new();
        public readonly Dictionary<Vector3Int, MapGridCellData> soCells = new();
        public readonly Dictionary<Vector3Int, MapGridCellFlags> expectedFlags = new();
        public readonly HashSet<Vector3Int> mismatchCells = new();

        public Grid grid;
        public BoundsInt expectedBounds;
        public bool hasExpectedBounds;
        public int reportLimit;
        public bool reportLimitReached;

        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public int InfoCount { get; private set; }

        public void AddReport(MapGridCheckerSeverity severity, string message)
        {
            AddReport(new MapGridCheckerReportItem(severity, message));
        }

        public void AddReport(MapGridCheckerSeverity severity, string message, Vector3Int cell)
        {
            AddReport(new MapGridCheckerReportItem(severity, message, cell));
        }

        public void AddReportIgnoringLimit(MapGridCheckerSeverity severity, string message)
        {
            MapGridCheckerReportItem item = new MapGridCheckerReportItem(severity, message);
            Count(item.Severity);
            reports.Add(item);
        }

        private void AddReport(MapGridCheckerReportItem item)
        {
            Count(item.Severity);

            if (reportLimit > 0 && reports.Count >= reportLimit)
            {
                reportLimitReached = true;
                return;
            }

            reports.Add(item);
        }

        private void Count(MapGridCheckerSeverity severity)
        {
            switch (severity)
            {
                case MapGridCheckerSeverity.Error:
                    ErrorCount++;
                    break;
                case MapGridCheckerSeverity.Warning:
                    WarningCount++;
                    break;
                default:
                    InfoCount++;
                    break;
            }
        }
    }
}
