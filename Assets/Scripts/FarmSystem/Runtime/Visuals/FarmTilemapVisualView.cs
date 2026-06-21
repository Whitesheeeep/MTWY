using GameData;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FarmSystem
{
    /// <summary>
    /// Farm Tilemap 表现层。只读取 Farm 状态并刷新 Tilemap，不执行任何农田业务。
    /// </summary>
    public sealed class FarmTilemapVisualView : MonoBehaviour
    {
        private const string DefaultTilledTilemapTag = "FarmTilledTilemap";
        private const string DefaultWateredTilemapTag = "FarmWateredTilemap";

        [SerializeField] private string tilledTilemapTag = DefaultTilledTilemapTag;
        [SerializeField] private string wateredTilemapTag = DefaultWateredTilemapTag;
        [SerializeField] private TileBase tilledTile;
        [SerializeField] private TileBase wateredTile;
        [SerializeField] private bool redrawOnEnable = true;

        private Tilemap tilledTilemap;
        private Tilemap wateredTilemap;
        private bool loggedMissingTilledTile;
        private bool loggedMissingWateredTile;

        private void OnEnable()
        {
            FarmLandManager.Instance.CellStateChanged += OnFarmCellStateChanged;
            MapGridManager.Instance.CurrentMapLoaded += OnCurrentMapLoaded;

            ResolveTilemaps(logMissing: true);
            if (redrawOnEnable)
            {
                RedrawCurrentMap();
            }
        }

        private void OnDisable()
        {
            FarmLandManager.Instance.CellStateChanged -= OnFarmCellStateChanged;
            MapGridManager.Instance.CurrentMapLoaded -= OnCurrentMapLoaded;
        }

        private void OnCurrentMapLoaded(MapGridCurrentMapLoadedEventArgs args)
        {
            tilledTilemap = null;
            wateredTilemap = null;
            loggedMissingTilledTile = false;
            loggedMissingWateredTile = false;

            ResolveTilemaps(logMissing: true);
            RedrawMap(args.MapId);
        }

        private void OnFarmCellStateChanged(FarmCellStateChangedEventArgs args)
        {
            string currentMapId = MapGridManager.Instance.CurrentMapId;
            if (string.IsNullOrWhiteSpace(currentMapId) || args.MapId != currentMapId)
            {
                return;
            }

            ResolveTilemaps(logMissing: false);
            RefreshCell(args.State);
        }

        private void RedrawCurrentMap()
        {
            string currentMapId = MapGridManager.Instance.CurrentMapId;
            if (string.IsNullOrWhiteSpace(currentMapId))
            {
                Debug.Log("[FarmTilemapVisualView] Current map id is empty, skip redraw.");
                return;
            }

            RedrawMap(currentMapId);
        }

        private void RedrawMap(string mapId)
        {
            if (string.IsNullOrWhiteSpace(mapId))
            {
                Debug.Log("[FarmTilemapVisualView] Map id is empty, skip redraw.");
                return;
            }

            ResolveTilemaps(logMissing: true);

            if (tilledTilemap != null)
            {
                tilledTilemap.ClearAllTiles();
            }

            if (wateredTilemap != null)
            {
                wateredTilemap.ClearAllTiles();
            }

            if (tilledTilemap != null && TryGetTilledTile(out TileBase resolvedTilledTile))
            {
                foreach (Vector3Int cell in FarmLandManager.Instance.GetTilledCells(mapId))
                {
                    tilledTilemap.SetTile(cell, resolvedTilledTile);
                }
            }

            if (wateredTilemap != null && TryGetWateredTile(out TileBase resolvedWateredTile))
            {
                foreach (Vector3Int cell in FarmLandManager.Instance.GetWateredCells(mapId))
                {
                    wateredTilemap.SetTile(cell, resolvedWateredTile);
                }
            }
        }

        private void RefreshCell(FarmCellState state)
        {
            if (tilledTilemap != null && TryGetTilledTile(out TileBase resolvedTilledTile))
            {
                tilledTilemap.SetTile(state.Cell, state.IsTilled ? resolvedTilledTile : null);
            }

            if (wateredTilemap != null && TryGetWateredTile(out TileBase resolvedWateredTile))
            {
                wateredTilemap.SetTile(state.Cell, state.IsWatered ? resolvedWateredTile : null);
            }
        }

        private void ResolveTilemaps(bool logMissing)
        {
            if (tilledTilemap == null)
            {
                tilledTilemap = FindTilemapByTag(tilledTilemapTag, "tilled", logMissing);
            }

            if (wateredTilemap == null)
            {
                wateredTilemap = FindTilemapByTag(wateredTilemapTag, "watered", logMissing);
            }
        }

        private Tilemap FindTilemapByTag(string tilemapTag, string layerName, bool logMissing)
        {
            if (string.IsNullOrWhiteSpace(tilemapTag))
            {
                if (logMissing)
                {
                    Debug.Log($"[FarmTilemapVisualView] {layerName} tilemap tag is empty.");
                }

                return null;
            }

            GameObject target = null;
            try
            {
                target = GameObject.FindGameObjectWithTag(tilemapTag);
            }
            catch (UnityException)
            {
                if (logMissing)
                {
                    Debug.Log($"[FarmTilemapVisualView] Tag '{tilemapTag}' is not defined, skip {layerName} tilemap.");
                }

                return null;
            }

            if (target == null)
            {
                if (logMissing)
                {
                    Debug.Log($"[FarmTilemapVisualView] No GameObject found with tag '{tilemapTag}', skip {layerName} tilemap.");
                }

                return null;
            }

            if (!target.TryGetComponent(out Tilemap tilemap))
            {
                if (logMissing)
                {
                    Debug.LogWarning($"[FarmTilemapVisualView] GameObject '{target.name}' uses tag '{tilemapTag}' but has no Tilemap component.");
                }

                return null;
            }

            return tilemap;
        }

        private bool TryGetTilledTile(out TileBase tile)
        {
            tile = tilledTile;
            if (tile != null)
            {
                return true;
            }

            if (!loggedMissingTilledTile)
            {
                Debug.Log("[FarmTilemapVisualView] Tilled tile is not assigned, skip tilled visual refresh.");
                loggedMissingTilledTile = true;
            }

            return false;
        }

        private bool TryGetWateredTile(out TileBase tile)
        {
            tile = wateredTile;
            if (tile != null)
            {
                return true;
            }

            if (!loggedMissingWateredTile)
            {
                Debug.Log("[FarmTilemapVisualView] Watered tile is not assigned, skip watered visual refresh.");
                loggedMissingWateredTile = true;
            }

            return false;
        }
    }
}
