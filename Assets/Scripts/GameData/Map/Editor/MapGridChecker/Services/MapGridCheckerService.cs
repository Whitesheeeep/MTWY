using System.Collections.Generic;
using UnityEngine;

namespace GameData.Editor
{
    internal static class MapGridCheckerService
    {
        private const int MaxReportRows = 1000;

        public static MapGridCheckerResult Check(MapGridBakeSource source, MapGridData_SO mapData)
        {
            MapGridCheckerResult result = new MapGridCheckerResult
            {
                reportLimit = MaxReportRows
            };

            if (source == null)
            {
                result.AddReport(MapGridCheckerSeverity.Error, "MapGridBakeSource is not selected.");
                return result;
            }

            if (mapData == null)
            {
                result.AddReport(MapGridCheckerSeverity.Error, "MapGridData_SO is not selected.");
            }
            else
            {
                BuildSoCellMap(mapData, result);
            }

            result.grid = source.GetComponentInParent<Grid>();
            if (result.grid == null)
            {
                result.AddReport(MapGridCheckerSeverity.Error, "Grid is missing. SceneView overlay cannot draw world positions.");
            }

            if (source.layers == null || source.layers.Count == 0)
            {
                result.AddReport(MapGridCheckerSeverity.Error, "MapGridBakeSource has no layers.");
                return result;
            }

            if (!TryGetCombinedBounds(source.layers, out BoundsInt expectedBounds))
            {
                result.AddReport(MapGridCheckerSeverity.Error, "No valid Tilemap bounds found from affectsBounds layers.");
                return result;
            }

            result.expectedBounds = expectedBounds;
            result.hasExpectedBounds = true;
            BuildExpectedFlags(source.layers, expectedBounds, result.expectedFlags);

            if (mapData != null)
            {
                CompareMapHeader(source, mapData, expectedBounds, result);
                CompareCells(mapData, expectedBounds, result);
            }

            if (result.reportLimitReached)
            {
                result.AddReportIgnoringLimit(
                    MapGridCheckerSeverity.Warning,
                    $"Report list reached {MaxReportRows} rows. Fix visible issues, then refresh.");
            }

            if (result.ErrorCount == 0 && result.WarningCount == 0)
            {
                result.AddReport(MapGridCheckerSeverity.Info, "MapGridData_SO matches current Tilemap scan.");
            }

            return result;
        }

        private static void BuildSoCellMap(MapGridData_SO mapData, MapGridCheckerResult result)
        {
            if (mapData.cells == null)
            {
                result.AddReport(MapGridCheckerSeverity.Error, "MapGridData_SO cells list is null.");
                return;
            }

            foreach (MapGridCellData cell in mapData.cells)
            {
                if (result.soCells.ContainsKey(cell.cellPosition))
                {
                    result.AddReport(MapGridCheckerSeverity.Error, "Duplicate SO cell.", cell.cellPosition);
                    result.mismatchCells.Add(cell.cellPosition);
                    continue;
                }

                result.soCells.Add(cell.cellPosition, cell);
            }
        }

        private static void CompareMapHeader(
            MapGridBakeSource source,
            MapGridData_SO mapData,
            BoundsInt expectedBounds,
            MapGridCheckerResult result)
        {
            string expectedMapId = ResolveMapId(source);
            if (mapData.mapId != expectedMapId)
            {
                result.AddReport(MapGridCheckerSeverity.Warning, $"MapId mismatch. SO:{mapData.mapId}, Expected:{expectedMapId}.");
            }

            Vector3Int expectedOrigin = new Vector3Int(expectedBounds.xMin, expectedBounds.yMin, expectedBounds.zMin);
            if (mapData.originCell != expectedOrigin)
            {
                result.AddReport(MapGridCheckerSeverity.Error, $"Origin mismatch. SO:{mapData.originCell}, Expected:{expectedOrigin}.");
            }

            if (mapData.width != expectedBounds.size.x)
            {
                result.AddReport(MapGridCheckerSeverity.Error, $"Width mismatch. SO:{mapData.width}, Expected:{expectedBounds.size.x}.");
            }

            if (mapData.height != expectedBounds.size.y)
            {
                result.AddReport(MapGridCheckerSeverity.Error, $"Height mismatch. SO:{mapData.height}, Expected:{expectedBounds.size.y}.");
            }

            Vector3 expectedCellSize = result.grid != null ? result.grid.cellSize : Vector3.one;
            if (!Approximately(mapData.cellSize, expectedCellSize))
            {
                result.AddReport(MapGridCheckerSeverity.Warning, $"CellSize mismatch. SO:{mapData.cellSize}, Expected:{expectedCellSize}.");
            }
        }

        private static void CompareCells(MapGridData_SO mapData, BoundsInt expectedBounds, MapGridCheckerResult result)
        {
            foreach (KeyValuePair<Vector3Int, MapGridCellFlags> expected in result.expectedFlags)
            {
                if (!result.soCells.TryGetValue(expected.Key, out MapGridCellData soCell))
                {
                    result.AddReport(MapGridCheckerSeverity.Error, "SO is missing expected cell.", expected.Key);
                    result.mismatchCells.Add(expected.Key);
                    continue;
                }

                int expectedGridX = expected.Key.x - expectedBounds.xMin;
                int expectedGridY = expected.Key.y - expectedBounds.yMin;
                if (soCell.gridX != expectedGridX || soCell.gridY != expectedGridY)
                {
                    result.AddReport(
                        MapGridCheckerSeverity.Error,
                        $"Grid index mismatch. SO:({soCell.gridX},{soCell.gridY}), Expected:({expectedGridX},{expectedGridY}).",
                        expected.Key);
                    result.mismatchCells.Add(expected.Key);
                }

                if (soCell.staticFlags != expected.Value)
                {
                    result.AddReport(
                        MapGridCheckerSeverity.Warning,
                        $"StaticFlags mismatch. SO:{soCell.staticFlags}, Expected:{expected.Value}.",
                        expected.Key);
                    result.mismatchCells.Add(expected.Key);
                }
            }

            foreach (MapGridCellData soCell in result.soCells.Values)
            {
                if (result.expectedFlags.ContainsKey(soCell.cellPosition))
                {
                    continue;
                }

                result.AddReport(MapGridCheckerSeverity.Error, "SO has extra cell outside current Tilemap bounds.", soCell.cellPosition);
                result.mismatchCells.Add(soCell.cellPosition);
            }

            int expectedCount = expectedBounds.size.x * expectedBounds.size.y;
            if (mapData.cells != null && mapData.cells.Count != expectedCount)
            {
                result.AddReport(MapGridCheckerSeverity.Warning, $"Cell count mismatch. SO:{mapData.cells.Count}, Expected:{expectedCount}.");
            }
        }

        private static bool TryGetCombinedBounds(List<MapGridTilemapLayer> layers, out BoundsInt combinedBounds)
        {
            combinedBounds = new BoundsInt();
            bool hasBounds = false;

            foreach (MapGridTilemapLayer layer in layers)
            {
                if (layer == null || layer.tilemap == null || !layer.affectsBounds)
                {
                    continue;
                }

                BoundsInt bounds = layer.tilemap.cellBounds;
                if (bounds.size.x <= 0 || bounds.size.y <= 0)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combinedBounds = bounds;
                    hasBounds = true;
                    continue;
                }

                int xMin = Mathf.Min(combinedBounds.xMin, bounds.xMin);
                int yMin = Mathf.Min(combinedBounds.yMin, bounds.yMin);
                int zMin = Mathf.Min(combinedBounds.zMin, bounds.zMin);
                int xMax = Mathf.Max(combinedBounds.xMax, bounds.xMax);
                int yMax = Mathf.Max(combinedBounds.yMax, bounds.yMax);
                int zMax = Mathf.Max(combinedBounds.zMax, bounds.zMax);
                combinedBounds = new BoundsInt(xMin, yMin, zMin, xMax - xMin, yMax - yMin, zMax - zMin);
            }

            return hasBounds;
        }

        private static void BuildExpectedFlags(
            List<MapGridTilemapLayer> layers,
            BoundsInt bounds,
            Dictionary<Vector3Int, MapGridCellFlags> expectedFlags)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    Vector3Int cellPosition = new Vector3Int(x, y, bounds.zMin);
                    MapGridCellFlags flags = MapGridCellFlags.None;

                    foreach (MapGridTilemapLayer layer in layers)
                    {
                        if (layer == null || layer.tilemap == null || !layer.tilemap.HasTile(cellPosition))
                        {
                            continue;
                        }

                        flags |= layer.flags;
                    }

                    expectedFlags[cellPosition] = flags;
                }
            }
        }

        private static string ResolveMapId(MapGridBakeSource source)
        {
            if (!string.IsNullOrWhiteSpace(source.mapId))
            {
                return source.mapId;
            }

            string sceneName = source.gameObject.scene.name;
            return string.IsNullOrWhiteSpace(sceneName) ? source.name : sceneName;
        }

        private static bool Approximately(Vector3 a, Vector3 b)
        {
            return Mathf.Approximately(a.x, b.x)
                && Mathf.Approximately(a.y, b.y)
                && Mathf.Approximately(a.z, b.z);
        }
    }
}
