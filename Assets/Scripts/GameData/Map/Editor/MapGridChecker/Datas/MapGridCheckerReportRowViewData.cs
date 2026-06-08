namespace GameData.Editor
{
    internal sealed class MapGridCheckerReportRowViewData
    {
        public MapGridCheckerReportRowViewData(MapGridCheckerReportItem report)
        {
            Report = report;
        }

        public MapGridCheckerReportItem Report { get; }
        public string SeverityText => Report.Severity.ToString();
        public string CellText => Report.CellText;
        public string Message => Report.Message;
    }
}
