using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameData.Editor
{
    internal sealed class MapGridCheckerViewModel
    {
        public event Action Changed;
        public event Action ReportsChanged;

        public MapGridBakeSource Source { get; private set; }
        public MapGridData_SO MapData { get; private set; }
        public MapGridCheckerResult Result { get; private set; } = new();
        public IReadOnlyList<MapGridCheckerReportItem> Reports => Result.reports;

        public bool ShowOverlay { get; private set; } = true;
        public bool ShowNone { get; private set; } = true;
        public bool ShowBlocked { get; private set; } = true;
        public bool ShowWater { get; private set; } = true;
        public bool ShowCanDig { get; private set; } = true;
        public bool ShowCanDropItem { get; private set; } = true;
        public bool ShowCanPlaceFurniture { get; private set; } = true;
        public bool ShowNpcObstacle { get; private set; } = true;

        public string SummaryText
        {
            get
            {
                if (Result == null)
                {
                    return "No check result.";
                }

                return $"Errors:{Result.ErrorCount}  Warnings:{Result.WarningCount}  Info:{Result.InfoCount}  Reports:{Result.reports.Count}";
            }
        }

        public string BoundsText
        {
            get
            {
                if (MapData == null)
                {
                    return "SO: -";
                }

                return $"SO Origin:{MapData.originCell}  Size:{MapData.width}x{MapData.height}  Cells:{MapData.cells?.Count ?? 0}";
            }
        }

        public string ExpectedText
        {
            get
            {
                if (Result == null || !Result.hasExpectedBounds)
                {
                    return "Expected: -";
                }

                BoundsInt bounds = Result.expectedBounds;
                return $"Expected Origin:({bounds.xMin}, {bounds.yMin}, {bounds.zMin})  Size:{bounds.size.x}x{bounds.size.y}  Cells:{Result.expectedFlags.Count}";
            }
        }

        public void LoadSelection()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                return;
            }

            MapGridBakeSource source = selected.GetComponent<MapGridBakeSource>()
                ?? selected.GetComponentInParent<MapGridBakeSource>()
                ?? selected.GetComponentInChildren<MapGridBakeSource>();

            if (source != null)
            {
                SetSource(source);
            }
        }

        public void SetSource(MapGridBakeSource source)
        {
            Source = source;
            MapData = source != null ? source.outputData : null;
            RefreshCheck();
        }

        public void SetMapData(MapGridData_SO mapData)
        {
            MapData = mapData;
            RefreshCheck();
        }

        public void RefreshCheck()
        {
            Result = MapGridCheckerService.Check(Source, MapData);
            ReportsChanged?.Invoke();
            Changed?.Invoke();
            SceneView.RepaintAll();
        }

        public void HandleUndoRedo()
        {
            RefreshCheck();
        }

        public void SetShowOverlay(bool value)
        {
            ShowOverlay = value;
            Changed?.Invoke();
            SceneView.RepaintAll();
        }

        public void SetFlagFilter(MapGridCellFlags flag, bool value)
        {
            switch (flag)
            {
                case MapGridCellFlags.None:
                    ShowNone = value;
                    break;
                case MapGridCellFlags.Blocked:
                    ShowBlocked = value;
                    break;
                case MapGridCellFlags.Water:
                    ShowWater = value;
                    break;
                case MapGridCellFlags.CanDig:
                    ShowCanDig = value;
                    break;
                case MapGridCellFlags.CanDropItem:
                    ShowCanDropItem = value;
                    break;
                case MapGridCellFlags.CanPlaceFurniture:
                    ShowCanPlaceFurniture = value;
                    break;
                case MapGridCellFlags.NpcObstacle:
                    ShowNpcObstacle = value;
                    break;
            }

            Changed?.Invoke();
            SceneView.RepaintAll();
        }

        public IEnumerable<MapGridCellData> GetVisibleSoCells()
        {
            if (Result?.soCells == null)
            {
                return Enumerable.Empty<MapGridCellData>();
            }

            return Result.soCells.Values.Where(cell => ShouldShow(cell.staticFlags));
        }

        private bool ShouldShow(MapGridCellFlags flags)
        {
            if (flags == MapGridCellFlags.None)
            {
                return ShowNone;
            }

            return ((flags & MapGridCellFlags.Blocked) != 0 && ShowBlocked)
                || ((flags & MapGridCellFlags.Water) != 0 && ShowWater)
                || ((flags & MapGridCellFlags.CanDig) != 0 && ShowCanDig)
                || ((flags & MapGridCellFlags.CanDropItem) != 0 && ShowCanDropItem)
                || ((flags & MapGridCellFlags.CanPlaceFurniture) != 0 && ShowCanPlaceFurniture)
                || ((flags & MapGridCellFlags.NpcObstacle) != 0 && ShowNpcObstacle);
        }
    }
}
