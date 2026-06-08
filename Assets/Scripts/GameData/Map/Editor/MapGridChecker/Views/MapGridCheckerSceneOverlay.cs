using UnityEditor;
using UnityEngine;

namespace GameData.Editor
{
    internal static class MapGridCheckerSceneOverlay
    {
        public static void Draw(SceneView sceneView, MapGridCheckerViewModel viewModel)
        {
            if (viewModel == null || !viewModel.ShowOverlay || viewModel.Result?.grid == null || viewModel.MapData == null)
            {
                return;
            }

            Grid grid = viewModel.Result.grid;
            foreach (MapGridCellData cell in viewModel.GetVisibleSoCells())
            {
                bool isMismatch = viewModel.Result.mismatchCells.Contains(cell.cellPosition);
                DrawCell(grid, cell.cellPosition, cell.staticFlags, isMismatch);
            }
        }

        private static void DrawCell(Grid grid, Vector3Int cell, MapGridCellFlags flags, bool isMismatch)
        {
            Vector3 center = grid.GetCellCenterWorld(cell);
            Vector3 bottomLeft = grid.CellToWorld(cell);
            Vector3 bottomRight = grid.CellToWorld(cell + Vector3Int.right);
            Vector3 topRight = grid.CellToWorld(cell + Vector3Int.right + Vector3Int.up);
            Vector3 topLeft = grid.CellToWorld(cell + Vector3Int.up);
            Vector3[] vertices = { bottomLeft, bottomRight, topRight, topLeft };

            Color fill = GetFillColor(flags);
            Color outline = isMismatch ? new Color(1f, 0.08f, 0.08f, 1f) : new Color(fill.r, fill.g, fill.b, 0.85f);
            Handles.DrawSolidRectangleWithOutline(vertices, fill, outline);

            if (isMismatch)
            {
                Handles.color = outline;
                Handles.Label(center, "Mismatch");
            }
        }

        private static Color GetFillColor(MapGridCellFlags flags)
        {
            if ((flags & MapGridCellFlags.Blocked) != 0)
            {
                return new Color(0.85f, 0.18f, 0.18f, 0.25f);
            }

            if ((flags & MapGridCellFlags.Water) != 0)
            {
                return new Color(0.12f, 0.45f, 0.95f, 0.25f);
            }

            if ((flags & MapGridCellFlags.NpcObstacle) != 0)
            {
                return new Color(0.9f, 0.42f, 0.12f, 0.25f);
            }

            if ((flags & MapGridCellFlags.CanPlaceFurniture) != 0)
            {
                return new Color(0.55f, 0.36f, 0.95f, 0.22f);
            }

            if ((flags & MapGridCellFlags.CanDig) != 0)
            {
                return new Color(0.48f, 0.75f, 0.28f, 0.22f);
            }

            if ((flags & MapGridCellFlags.CanDropItem) != 0)
            {
                return new Color(0.95f, 0.78f, 0.18f, 0.22f);
            }

            return new Color(0.7f, 0.7f, 0.7f, 0.08f);
        }
    }
}
