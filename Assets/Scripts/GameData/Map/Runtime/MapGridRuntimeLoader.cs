using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using WS_Modules;
using WS_Modules.ResLoadModule;

namespace GameData
{
    /// <summary>
    /// Scene-local loader that binds the scene Grid and its MapGridData to MapGridManager.
    /// </summary>
    public sealed class MapGridRuntimeLoader : MonoBehaviour
    {
        [Header("Map Data")]
        [InfoBox("优先使用 MapGridData Key 加载地图数据，如果 Key 无效或未配置，则使用直接引用的 MapGridData。")]
        [SerializeField, WSAddressableKey("MapGrid", "SO")] private string mapGridDataKey;
        [SerializeField] private MapGridData_SO mapGridData;

        [Header("Scene Grid")]
        [SerializeField] private Grid grid;

        private int loadVersion;
        private MapGridData_SO loadedMapData;

        private void OnEnable()
        {
            loadVersion++;
            LoadMapAsync(loadVersion).Forget();
        }

        private void OnDisable()
        {
            loadVersion++;

            if (loadedMapData != null && MapGridManager.Instance.CurrentMapData == loadedMapData)
            {
                MapGridManager.Instance.UnloadCurrentMap();
            }

            loadedMapData = null;
        }

        private async UniTaskVoid LoadMapAsync(int version)
        {
            if (grid == null)
            {
                Debug.LogError($"[MapGridRuntimeLoader] Grid is not assigned on {name}.");
                return;
            }

            MapGridData_SO resolvedMapData = await ResolveMapDataAsync();
            if (version != loadVersion || !isActiveAndEnabled)
            {
                return;
            }

            if (resolvedMapData == null)
            {
                Debug.LogError($"[MapGridRuntimeLoader] MapGridData is not configured on {name}. Assign mapGridDataKey or mapGridData.");
                return;
            }

            loadedMapData = resolvedMapData;
            MapGridManager.Instance.LoadCurrentMap(resolvedMapData, grid);
        }

        private async UniTask<MapGridData_SO> ResolveMapDataAsync()
        {
            if (!string.IsNullOrWhiteSpace(mapGridDataKey))
            {
                try
                {
                    MapGridData_SO keyData = await ResSystem.Instance.LoadAsync<MapGridData_SO>(mapGridDataKey);
                    if (keyData != null)
                    {
                        return keyData;
                    }

                    Debug.LogError($"[MapGridRuntimeLoader] Failed to load MapGridData_SO by key '{mapGridDataKey}' on {name}.");
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            return mapGridData;
        }
    }
}
