using UnityEngine;

namespace GameData.Editor
{
    internal sealed class MapGridCheckerReportItem
    {
        public MapGridCheckerReportItem(MapGridCheckerSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
            HasCell = false;
            Cell = Vector3Int.zero;
        }

        public MapGridCheckerReportItem(MapGridCheckerSeverity severity, string message, Vector3Int cell)
        {
            Severity = severity;
            Message = message;
            HasCell = true;
            Cell = cell;
        }

        public MapGridCheckerSeverity Severity { get; }
        public string Message { get; }
        public bool HasCell { get; }
        public Vector3Int Cell { get; }

        public string CellText => HasCell ? $"Cell {Cell.x}, {Cell.y}, {Cell.z}" : "-";
    }
}
