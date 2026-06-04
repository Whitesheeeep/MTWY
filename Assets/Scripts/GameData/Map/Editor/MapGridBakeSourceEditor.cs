using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameData.Editor
{
    /// <summary>
    /// MapGridBakeSource 的 Inspector 工具，提供图层自动填充和静态地图数据 Bake。
    /// </summary>
    [CustomEditor(typeof(MapGridBakeSource))]
    public sealed class MapGridBakeSourceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            MapGridBakeSource source = (MapGridBakeSource)target;

            if (GUILayout.Button("Auto Fill Layers From Children"))
            {
                AutoFillLayers(source);
            }

            if (GUILayout.Button("Bake Map Grid Data"))
            {
                Bake(source);
            }
        }

        private static void AutoFillLayers(MapGridBakeSource source)
        {
            Undo.RecordObject(source, "Auto Fill Map Grid Layers");
            source.layers.Clear();

            Tilemap[] tilemaps = source.GetComponentsInChildren<Tilemap>(true);
            foreach (Tilemap tilemap in tilemaps)
            {
                if (!TryGetFlagsByName(tilemap.name, out MapGridCellFlags flags))
                {
                    continue;
                }

                source.layers.Add(new MapGridTilemapLayer
                {
                    tilemap = tilemap,
                    flags = flags,
                    affectsBounds = true
                });
            }

            EditorUtility.SetDirty(source);
            EditorSceneManager.MarkSceneDirty(source.gameObject.scene);
        }

        private static void Bake(MapGridBakeSource source)
        {
            EnsureOutputData(source);

            if (source.outputData == null)
            {
                Debug.LogError($"[MapGridBakeSourceEditor] Output MapGridData_SO is not assigned on {source.name}.");
                return;
            }

            if (source.layers == null || source.layers.Count == 0)
            {
                Debug.LogError($"[MapGridBakeSourceEditor] No Tilemap layers configured on {source.name}.");
                return;
            }

            if (!TryGetCombinedBounds(source.layers, out BoundsInt bounds))
            {
                Debug.LogError($"[MapGridBakeSourceEditor] No valid Tilemap bounds found on {source.name}.");
                return;
            }

            Undo.RecordObject(source.outputData, "Bake Map Grid Data");

            // mapId 当前约定等于场景名，避免地图 ID 与 SceneSystem 场景名分叉。
            string mapId = ResolveMapId(source);
            source.outputData.mapId = mapId;
            source.outputData.originCell = new Vector3Int(bounds.xMin, bounds.yMin, bounds.zMin);
            source.outputData.width = bounds.size.x;
            source.outputData.height = bounds.size.y;
            source.outputData.cellSize = GetCellSize(source);
            source.outputData.cells = BuildCells(source.layers, bounds);

            EditorUtility.SetDirty(source.outputData);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(source.gameObject.scene);
            Debug.Log($"[MapGridBakeSourceEditor] Baked map grid. Map:{mapId}, Size:{bounds.size.x}x{bounds.size.y}, Cells:{source.outputData.cells.Count}");
        }

        private static bool TryGetCombinedBounds(List<MapGridTilemapLayer> layers, out BoundsInt combinedBounds)
        {
            combinedBounds = new BoundsInt();
            bool hasBounds = false;

            // 使用所有 affectsBounds 图层的并集作为统一 Grid 范围。
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

        private static void EnsureOutputData(MapGridBakeSource source)
        {
            if (source.outputData != null)
            {
                return;
            }

            string mapId = ResolveMapId(source);

            const string folderPath = "Assets/Scripts/GameData/Map/SO";
            if (!AssetDatabase.IsValidFolder("Assets/Scripts/GameData/Map/SO"))
            {
                AssetDatabase.CreateFolder("Assets/Scripts/GameData/Map", "SO");
            }

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{mapId}MapGridData.asset");
            MapGridData_SO data = ScriptableObject.CreateInstance<MapGridData_SO>();
            AssetDatabase.CreateAsset(data, assetPath);

            Undo.RecordObject(source, "Assign Map Grid Output Data");
            source.outputData = data;
            EditorUtility.SetDirty(source);
            EditorSceneManager.MarkSceneDirty(source.gameObject.scene);
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

        private static List<MapGridCellData> BuildCells(List<MapGridTilemapLayer> layers, BoundsInt bounds)
        {
            List<MapGridCellData> cells = new List<MapGridCellData>(bounds.size.x * bounds.size.y);
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    Vector3Int cellPosition = new Vector3Int(x, y, bounds.zMin);
                    MapGridCellFlags flags = MapGridCellFlags.None;

                    // 同一个 cell 可以由多个逻辑 Tilemap 共同贡献 flags。
                    foreach (MapGridTilemapLayer layer in layers)
                    {
                        if (layer == null || layer.tilemap == null || !layer.tilemap.HasTile(cellPosition))
                        {
                            continue;
                        }

                        flags |= layer.flags;
                    }

                    cells.Add(new MapGridCellData
                    {
                        cellPosition = cellPosition,
                        gridX = x - bounds.xMin,
                        gridY = y - bounds.yMin,
                        staticFlags = flags
                    });
                }
            }

            return cells;
        }

        private static Vector3 GetCellSize(MapGridBakeSource source)
        {
            Grid grid = source.GetComponentInParent<Grid>();
            return grid != null ? grid.cellSize : Vector3.one;
        }

        private static bool TryGetFlagsByName(string layerName, out MapGridCellFlags flags)
        {
            string normalized = layerName.Replace(" ", string.Empty).ToLowerInvariant();
            switch (normalized)
            {
                case "collision":
                    flags = MapGridCellFlags.Blocked;
                    return true;
                case "water":
                    flags = MapGridCellFlags.Water;
                    return true;
                case "candig":
                case "dig":
                    flags = MapGridCellFlags.CanDig;
                    return true;
                case "candropitem":
                    flags = MapGridCellFlags.CanDropItem;
                    return true;
                case "canplacefurniture":
                    flags = MapGridCellFlags.CanPlaceFurniture;
                    return true;
                case "npcobstacle":
                    flags = MapGridCellFlags.NpcObstacle;
                    return true;
                default:
                    flags = MapGridCellFlags.None;
                    return false;
            }
        }
    }
}
