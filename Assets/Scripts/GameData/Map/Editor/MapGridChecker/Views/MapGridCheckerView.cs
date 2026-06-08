using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GameData.Editor
{
    internal sealed class MapGridCheckerView
    {
        private readonly VisualElement root;
        private readonly MapGridCheckerViewModel viewModel;
        private readonly VisualTreeAsset reportRowTemplate;
        private readonly List<MapGridCheckerReportRowViewData> rows = new();

        private ObjectField sourceField;
        private ObjectField mapDataField;
        private Button refreshButton;
        private Toggle overlayToggle;
        private Toggle noneToggle;
        private Toggle blockedToggle;
        private Toggle waterToggle;
        private Toggle canDigToggle;
        private Toggle canDropItemToggle;
        private Toggle canPlaceFurnitureToggle;
        private Toggle npcObstacleToggle;
        private Label summaryLabel;
        private Label boundsLabel;
        private Label expectedLabel;
        private ListView reportListView;

        private bool isRefreshing;

        public MapGridCheckerView(
            VisualElement root,
            MapGridCheckerViewModel viewModel,
            VisualTreeAsset reportRowTemplate)
        {
            this.root = root;
            this.viewModel = viewModel;
            this.reportRowTemplate = reportRowTemplate;
        }

        public void Bind()
        {
            QueryElements();
            ConfigureFields();
            ConfigureReportList();
            RegisterViewModelEvents();
            RefreshAll();
        }

        private void QueryElements()
        {
            sourceField = root.Q<ObjectField>("SourceField");
            mapDataField = root.Q<ObjectField>("MapDataField");
            refreshButton = root.Q<Button>("RefreshButton");
            overlayToggle = root.Q<Toggle>("OverlayToggle");
            noneToggle = root.Q<Toggle>("NoneToggle");
            blockedToggle = root.Q<Toggle>("BlockedToggle");
            waterToggle = root.Q<Toggle>("WaterToggle");
            canDigToggle = root.Q<Toggle>("CanDigToggle");
            canDropItemToggle = root.Q<Toggle>("CanDropItemToggle");
            canPlaceFurnitureToggle = root.Q<Toggle>("CanPlaceFurnitureToggle");
            npcObstacleToggle = root.Q<Toggle>("NpcObstacleToggle");
            summaryLabel = root.Q<Label>("SummaryLabel");
            boundsLabel = root.Q<Label>("BoundsLabel");
            expectedLabel = root.Q<Label>("ExpectedLabel");
            reportListView = root.Q<ListView>("ReportListView");
        }

        private void ConfigureFields()
        {
            sourceField.objectType = typeof(MapGridBakeSource);
            sourceField.allowSceneObjects = true;
            mapDataField.objectType = typeof(MapGridData_SO);
            mapDataField.allowSceneObjects = false;

            sourceField.RegisterValueChangedCallback(evt =>
            {
                if (isRefreshing)
                {
                    return;
                }

                viewModel.SetSource(evt.newValue as MapGridBakeSource);
            });

            mapDataField.RegisterValueChangedCallback(evt =>
            {
                if (isRefreshing)
                {
                    return;
                }

                viewModel.SetMapData(evt.newValue as MapGridData_SO);
            });

            refreshButton.clicked += viewModel.RefreshCheck;
            overlayToggle.RegisterValueChangedCallback(evt => viewModel.SetShowOverlay(evt.newValue));
            noneToggle.RegisterValueChangedCallback(evt => viewModel.SetFlagFilter(MapGridCellFlags.None, evt.newValue));
            blockedToggle.RegisterValueChangedCallback(evt => viewModel.SetFlagFilter(MapGridCellFlags.Blocked, evt.newValue));
            waterToggle.RegisterValueChangedCallback(evt => viewModel.SetFlagFilter(MapGridCellFlags.Water, evt.newValue));
            canDigToggle.RegisterValueChangedCallback(evt => viewModel.SetFlagFilter(MapGridCellFlags.CanDig, evt.newValue));
            canDropItemToggle.RegisterValueChangedCallback(evt => viewModel.SetFlagFilter(MapGridCellFlags.CanDropItem, evt.newValue));
            canPlaceFurnitureToggle.RegisterValueChangedCallback(evt => viewModel.SetFlagFilter(MapGridCellFlags.CanPlaceFurniture, evt.newValue));
            npcObstacleToggle.RegisterValueChangedCallback(evt => viewModel.SetFlagFilter(MapGridCellFlags.NpcObstacle, evt.newValue));
        }

        private void ConfigureReportList()
        {
            reportListView.fixedItemHeight = 44;
            reportListView.selectionType = SelectionType.Single;
            reportListView.itemsSource = rows;
            reportListView.makeItem = MakeReportRow;
            reportListView.bindItem = BindReportRow;
        }

        private VisualElement MakeReportRow()
        {
            return reportRowTemplate.CloneTree();
        }

        private void BindReportRow(VisualElement element, int index)
        {
            if (index < 0 || index >= rows.Count)
            {
                return;
            }

            MapGridCheckerReportRowViewData row = rows[index];
            element.EnableInClassList("severity-info", row.Report.Severity == MapGridCheckerSeverity.Info);
            element.EnableInClassList("severity-warning", row.Report.Severity == MapGridCheckerSeverity.Warning);
            element.EnableInClassList("severity-error", row.Report.Severity == MapGridCheckerSeverity.Error);
            element.Q<Label>("SeverityLabel").text = row.SeverityText;
            element.Q<Label>("CellLabel").text = row.CellText;
            element.Q<Label>("MessageLabel").text = row.Message;
        }

        private void RegisterViewModelEvents()
        {
            viewModel.Changed += RefreshState;
            viewModel.ReportsChanged += RefreshReports;
        }

        private void RefreshAll()
        {
            RefreshState();
            RefreshReports();
        }

        private void RefreshState()
        {
            isRefreshing = true;
            sourceField.SetValueWithoutNotify(viewModel.Source);
            mapDataField.SetValueWithoutNotify(viewModel.MapData);
            overlayToggle.SetValueWithoutNotify(viewModel.ShowOverlay);
            noneToggle.SetValueWithoutNotify(viewModel.ShowNone);
            blockedToggle.SetValueWithoutNotify(viewModel.ShowBlocked);
            waterToggle.SetValueWithoutNotify(viewModel.ShowWater);
            canDigToggle.SetValueWithoutNotify(viewModel.ShowCanDig);
            canDropItemToggle.SetValueWithoutNotify(viewModel.ShowCanDropItem);
            canPlaceFurnitureToggle.SetValueWithoutNotify(viewModel.ShowCanPlaceFurniture);
            npcObstacleToggle.SetValueWithoutNotify(viewModel.ShowNpcObstacle);
            summaryLabel.text = viewModel.SummaryText;
            boundsLabel.text = viewModel.BoundsText;
            expectedLabel.text = viewModel.ExpectedText;
            isRefreshing = false;
        }

        private void RefreshReports()
        {
            rows.Clear();
            foreach (MapGridCheckerReportItem report in viewModel.Reports)
            {
                rows.Add(new MapGridCheckerReportRowViewData(report));
            }

            reportListView.Rebuild();
        }
    }
}
