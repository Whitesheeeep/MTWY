using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using WS_Modules;

namespace GameData
{
    /// <summary>
    /// 场景内地图 Loader。运行时只保存 mapId 和当前场景 Grid，地图数据统一由 Catalog/Addressables 加载。
    /// </summary>
    public sealed class MapGridRuntimeLoader : MonoBehaviour
    {
        [Header("Map Data")]
        [InfoBox("运行时只配置 mapId。实际 MapGridData_SO 由 MapGridCatalog 的 Addressables key 加载。")]
        [SerializeField, WSScene] private string mapId;

        [Header("Scene Grid")]
        [SerializeField] private Grid grid;

        private int loadVersion;
        private string loadedMapId;

        private void OnEnable()
        {
            loadVersion++;
            LoadMapAsync(loadVersion).Forget();
        }

        private void OnDisable()
        {
            loadVersion++;

            if (!string.IsNullOrWhiteSpace(loadedMapId) &&
                string.Equals(MapGridManager.Instance.CurrentMapId, loadedMapId, StringComparison.Ordinal))
            {
                MapGridManager.Instance.UnloadCurrentMap();
            }

            loadedMapId = string.Empty;
        }

        private async UniTaskVoid LoadMapAsync(int version)
        {
            if (string.IsNullOrWhiteSpace(mapId))
            {
                Debug.LogError($"[MapGridRuntimeLoader] MapId is not assigned on {name}.");
                return;
            }

            if (grid == null)
            {
                Debug.LogError($"[MapGridRuntimeLoader] Grid is not assigned on {name}.");
                return;
            }

            bool loaded = await MapGridManager.Instance.LoadCurrentMapAsync(mapId, grid);
            if (version != loadVersion || !isActiveAndEnabled)
            {
                return;
            }

            if (!loaded)
            {
                Debug.LogError($"[MapGridRuntimeLoader] Failed to load current map. Map:{mapId}, Loader:{name}.");
                return;
            }

            loadedMapId = mapId;
        }
    }
}
