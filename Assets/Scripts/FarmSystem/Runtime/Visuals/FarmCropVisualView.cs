using System.Collections.Generic;
using GameData;
using UnityEngine;
using WS_Modules.Singleton;

namespace FarmSystem
{
    /// <summary>
    /// 作物 SpriteRenderer 表现层，只读取 Farm 状态并刷新 prefab，不执行播种、成长或收获业务。
    /// </summary>
    public sealed class FarmCropVisualView : AutoSingletonMonoBase<FarmCropVisualView>
    {
        [SerializeField] private GameObject cropPrefab;
        [SerializeField] private Transform cropRoot;
        [SerializeField] private bool redrawOnEnable = true;

        private readonly Dictionary<Vector3Int, FarmCropVisualEntity> activeEntities =
            new Dictionary<Vector3Int, FarmCropVisualEntity>();

        // 启用时订阅 Farm 与地图加载事件，并按需重绘当前地图作物。
        private void OnEnable()
        {
            FarmLandManager.Instance.CropPlanted += OnCropPlanted;
            FarmLandManager.Instance.CropStageChanged += OnCropStageChanged;
            FarmLandManager.Instance.CropHarvested += OnCropHarvested;
            MapGridManager.Instance.CurrentMapLoaded += OnCurrentMapLoaded;

            if (redrawOnEnable)
            {
                RedrawCurrentMap();
            }
        }

        // 禁用时取消事件订阅，避免失效 View 继续接收刷新。
        private void OnDisable()
        {
            FarmLandManager.Instance.CropPlanted -= OnCropPlanted;
            FarmLandManager.Instance.CropStageChanged -= OnCropStageChanged;
            FarmLandManager.Instance.CropHarvested -= OnCropHarvested;
            MapGridManager.Instance.CurrentMapLoaded -= OnCurrentMapLoaded;
        }

        // 当前地图绑定成功后重建该地图的作物表现。
        private void OnCurrentMapLoaded(MapGridCurrentMapLoadedEventArgs args)
        {
            RedrawMap(args.MapId);
        }

        // 播种成功后为当前地图目标格创建或刷新作物表现。
        private void OnCropPlanted(FarmCropPlantedEventArgs args)
        {
            if (!IsCurrentMap(args.MapId))
            {
                return;
            }

            RefreshCrop(args.MapId, args.Cell, args.CropData, args.CurrentState);
        }

        // 阶段变化后刷新当前地图目标格的作物 Sprite。
        private void OnCropStageChanged(FarmCropStageChangedEventArgs args)
        {
            if (!IsCurrentMap(args.MapId))
            {
                return;
            }

            RefreshCrop(args.MapId, args.Cell, args.CropData, args.CurrentState);
        }

        // 收获后根据是否再生决定刷新或移除作物表现。
        private void OnCropHarvested(FarmCropHarvestedEventArgs args)
        {
            if (!IsCurrentMap(args.MapId))
            {
                return;
            }

            if (args.Regrew && args.CurrentState != null)
            {
                RefreshCrop(args.MapId, args.Cell, args.CropData, args.CurrentState);
                return;
            }

            RemoveCrop(args.Cell);
        }

        // 使用当前地图 ID 发起一次完整重绘。
        private void RedrawCurrentMap()
        {
            string currentMapId = MapGridManager.Instance.CurrentMapId;
            if (string.IsNullOrWhiteSpace(currentMapId))
            {
                return;
            }

            RedrawMap(currentMapId);
        }

        // 清理旧实例并根据指定地图的 Farm 作物状态重建表现。
        private void RedrawMap(string mapId)
        {
            ClearAllEntities();
            if (string.IsNullOrWhiteSpace(mapId))
            {
                return;
            }

            IReadOnlyList<FarmCropCellSnapshot> crops = FarmLandManager.Instance.GetPlantedCrops(mapId);
            for (int i = 0; i < crops.Count; i++)
            {
                FarmCropCellSnapshot crop = crops[i];
                if (crop.State == null)
                {
                    Debug.LogError($"[FarmCropVisualView] 作物状态为空，无法重绘。mapId={crop.MapId}, cell={crop.Cell}", this);
                    continue;
                }

                if (!TryGetCropData(crop.State.CropDataId, out CropData cropData))
                {
                    Debug.LogError($"[FarmCropVisualView] 找不到作物配置，无法重绘。cropDataId={crop.State.CropDataId}, mapId={crop.MapId}, cell={crop.Cell}", this);
                    continue;
                }

                RefreshCrop(crop.MapId, crop.Cell, cropData, crop.State);
            }
        }

        // 刷新单个格子的作物表现、位置、状态与阶段 Sprite。
        private void RefreshCrop(string mapId, Vector3Int cell, CropData cropData, PlantedCropState state)
        {
            if (state == null)
            {
                Debug.LogError($"[FarmCropVisualView] 作物状态为空，无法刷新。mapId={mapId}, cell={cell}", this);
                return;
            }

            if (!TryResolveStageSprite(cropData, state, out Sprite stageSprite))
            {
                return;
            }

            FarmCropVisualEntity entity = GetOrCreateEntity(cell);
            entity.transform.position = MapGridManager.Instance.GetCellCenterWorld(cell);
            entity.transform.rotation = Quaternion.identity;
            entity.transform.localScale = Vector3.one;
            entity.Bind(mapId, cell, cropData.Id, state);
            entity.ApplyStageSprite(stageSprite);
        }

        // 获取已有实体，或用 prefab 创建新的作物表现实体。
        private FarmCropVisualEntity GetOrCreateEntity(Vector3Int cell)
        {
            if (activeEntities.TryGetValue(cell, out FarmCropVisualEntity entity) && entity != null)
            {
                return entity;
            }

            if (cropPrefab == null)
            {
                Debug.LogError("[FarmCropVisualView] cropPrefab 未配置，无法创建作物表现对象。", this);
                throw new MissingReferenceException("FarmCropVisualView.cropPrefab is not assigned.");
            }

            EnsureCropRoot();
            GameObject instance = Instantiate(cropPrefab, cropRoot);
            if (!instance.TryGetComponent(out entity))
            {
                Debug.LogError($"[FarmCropVisualView] 作物 prefab 缺少 FarmCropVisualEntity 组件: {cropPrefab.name}", instance);
                throw new MissingComponentException($"作物 prefab 缺少 FarmCropVisualEntity 组件: {cropPrefab.name}");
            }

            activeEntities[cell] = entity;
            return entity;
        }

        // 从作物配置与运行时阶段中解析当前应显示的 Sprite。
        private bool TryResolveStageSprite(CropData cropData, PlantedCropState state, out Sprite sprite)
        {
            sprite = null;
            if (cropData == null)
            {
                Debug.LogError("[FarmCropVisualView] CropData 为空，无法解析作物阶段 Sprite。", this);
                return false;
            }

            if (cropData.GrowthStages == null ||
                state.CurrentStageIndex < 0 ||
                state.CurrentStageIndex >= cropData.GrowthStages.Count)
            {
                Debug.LogError($"[FarmCropVisualView] 作物阶段索引越界。cropDataId={cropData.Id}, stageIndex={state.CurrentStageIndex}", this);
                return false;
            }

            CropGrowthStageData stage = cropData.GrowthStages[state.CurrentStageIndex];
            if (stage == null)
            {
                Debug.LogError($"[FarmCropVisualView] 作物阶段配置为空。cropDataId={cropData.Id}, stageIndex={state.CurrentStageIndex}", this);
                return false;
            }

            if (stage.StageSprite == null)
            {
                Debug.LogError($"[FarmCropVisualView] 作物阶段 Sprite 未配置。cropDataId={cropData.Id}, stageIndex={state.CurrentStageIndex}", this);
                return false;
            }

            sprite = stage.StageSprite;
            return true;
        }

        // 移除指定格子的作物表现实体。
        private void RemoveCrop(Vector3Int cell)
        {
            if (!activeEntities.TryGetValue(cell, out FarmCropVisualEntity entity))
            {
                return;
            }

            activeEntities.Remove(cell);
            if (entity == null)
            {
                return;
            }

            Destroy(entity.gameObject);
        }

        // 清空当前 View 管理的所有作物表现实体。
        private void ClearAllEntities()
        {
            foreach (FarmCropVisualEntity entity in activeEntities.Values)
            {
                if (entity != null)
                {
                    Destroy(entity.gameObject);
                }
            }

            activeEntities.Clear();
        }

        // 确保运行时有一个用于承载作物表现对象的根节点。
        private void EnsureCropRoot()
        {
            if (cropRoot != null)
            {
                return;
            }

            GameObject rootObject = new GameObject("FarmCropVisualRoot");
            rootObject.transform.SetParent(transform, false);
            cropRoot = rootObject.transform;
        }

        // 通过 CropDatabase 查询作物配置。
        private static bool TryGetCropData(int cropDataId, out CropData cropData)
        {
            cropData = null;
            return GameDatabase.TryGet(out ICropDatabase cropDatabase) &&
                   cropDatabase.TryGet(cropDataId, out cropData);
        }

        // 判断事件所属地图是否为当前绑定地图。
        private static bool IsCurrentMap(string mapId)
        {
            string currentMapId = MapGridManager.Instance.CurrentMapId;
            return !string.IsNullOrWhiteSpace(currentMapId) && mapId == currentMapId;
        }
    }
}