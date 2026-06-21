using CursorSystem;
using GameData;
using UnityEngine;

namespace FarmSystem
{
    /// <summary>
    /// 作物表现实体，挂在作物 prefab 上，负责保存表现身份并按当前阶段刷新 Sprite 与点击区域。
    /// </summary>
    public sealed class FarmCropVisualEntity : MonoBehaviour, IItemInteractable
    {
        private const string CropSortingLayerName = "instance";

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Collider2D cropCollider;

        /// <summary>
        /// 当前表现对象所属地图 ID。
        /// </summary>
        public string MapId { get; private set; }

        /// <summary>
        /// 当前表现对象所在地图格子。
        /// </summary>
        public Vector3Int Cell { get; private set; }

        /// <summary>
        /// 当前表现对象对应的作物配置 ID。
        /// </summary>
        public int CropDataId { get; private set; }

        /// <summary>
        /// 当前表现对象持有的作物运行时状态快照。
        /// </summary>
        public PlantedCropState CurrentState { get; private set; }

        // 组件启用前缓存 prefab 上必需的表现组件。
        private void Awake()
        {
            CacheRequiredComponents();
        }

        /// <summary>
        /// 绑定作物表现身份与运行时状态快照。
        /// </summary>
        public void Bind(string mapId, Vector3Int cell, int cropDataId, PlantedCropState state)
        {
            MapId = mapId;
            Cell = cell;
            CropDataId = cropDataId;
            CurrentState = CloneState(state);
            gameObject.name = $"Crop_{cell.x}_{cell.y}_{cell.z}";
        }

        /// <summary>
        /// 判断当前选中物品是否可以与该作物实体交互。
        /// </summary>
        public bool CanInteract(ItemInteractionContext context)
        {
            return IsHarvestTool(context.SelectedItemType) &&
                   TryCreateHarvestContext(context, out ItemInteractionContext harvestContext) &&
                   FarmLandManager.Instance.CanHarvest(harvestContext);
        }

        /// <summary>
        /// 尝试对该作物实体执行交互。
        /// </summary>
        public bool TryInteract(ItemInteractionContext context)
        {
            if (!IsHarvestTool(context.SelectedItemType) ||
                !TryCreateHarvestContext(context, out ItemInteractionContext harvestContext))
            {
                return false;
            }

            return FarmLandManager.Instance.TryHarvest(harvestContext);
        }
        /// <summary>
        /// 应用当前成长阶段 Sprite，并同步渲染排序与点击区域。
        /// </summary>
        public void ApplyStageSprite(Sprite sprite)
        {
            CacheRequiredComponents();
            RequireConfiguredComponents();

            spriteRenderer.sprite = sprite;

            cropCollider.isTrigger = true;
            RebuildCollider(sprite);
        }

        // 从自身或子节点查找 SpriteRenderer 与 Collider2D。
        private void CacheRequiredComponents()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }

            if (cropCollider == null)
            {
                cropCollider = GetComponentInChildren<Collider2D>(true);
            }
        }

        // 缺少必需组件时直接报错，方便 prefab 配置问题暴露在 Console 中。
        private void RequireConfiguredComponents()
        {
            if (spriteRenderer == null)
            {
                throw new MissingComponentException($"[FarmCropVisualEntity] 作物 prefab 缺少 SpriteRenderer: {name}");
            }

            if (cropCollider == null)
            {
                throw new MissingComponentException($"[FarmCropVisualEntity] 作物 prefab 缺少 Collider2D: {name}");
            }
        }

        // 根据当前 Sprite 的本地包围盒重建点击区域。
        private void RebuildCollider(Sprite sprite)
        {
            if (sprite == null)
            {
                Debug.LogError($"[FarmCropVisualEntity] 作物阶段 Sprite 为空，无法重建点击区域。mapId={MapId}, cell={Cell}, cropDataId={CropDataId}", this);
                return;
            }

            if (cropCollider is BoxCollider2D boxCollider)
            {
                Bounds bounds = sprite.bounds;
                boxCollider.size = bounds.size;
                boxCollider.offset = bounds.center;
                return;
            }

            Debug.LogError($"[FarmCropVisualEntity] 当前 Collider2D 不是 BoxCollider2D，无法按 Sprite 自动重算尺寸: {cropCollider.GetType().Name}", this);
        }

        // 判断当前选中物品是否是作物实体允许的收获工具。
        private static bool IsHarvestTool(E_ItemType itemType)
        {
            return itemType == E_ItemType.CollectTool || itemType == E_ItemType.ReapTool;
        }

        // 判断目标格是否仍在当前物品允许的曼哈顿距离内。
        private static bool IsCellInRange(Vector3Int originCell, Vector3Int targetCell, int itemUseRadius)
        {
            int distance = Mathf.Abs(targetCell.x - originCell.x) + Mathf.Abs(targetCell.y - originCell.y);
            return distance <= Mathf.Max(0, itemUseRadius);
        }
        // 使用作物实体绑定的格子重新生成收获上下文，避免点击碰撞体外沿时使用鼠标所在格。
        private bool TryCreateHarvestContext(ItemInteractionContext sourceContext, out ItemInteractionContext harvestContext)
        {
            harvestContext = default;
            if (string.IsNullOrWhiteSpace(MapId) ||
                MapGridManager.Instance.CurrentMapId != MapId ||
                !IsCellInRange(sourceContext.OriginCell, Cell, sourceContext.ItemUseRadius) ||
                !MapGridManager.Instance.TryGetCell(MapId, Cell, out MapGridCellInfo cellInfo))
            {
                return false;
            }

            harvestContext = new ItemInteractionContext(
                sourceContext.Player,
                sourceContext.SelectedItemData,
                sourceContext.MouseScreenPosition,
                sourceContext.MouseWorldPosition,
                sourceContext.OriginCell,
                Cell,
                sourceContext.ItemUseRadius,
                true,
                gameObject,
                CursorTargetType.Entity,
                cellInfo);
            return true;
        }
        // 复制状态，避免表现层持有 Farm 内部状态引用。
        private static PlantedCropState CloneState(PlantedCropState state)
        {
            if (state == null)
            {
                return null;
            }

            return new PlantedCropState
            {
                CropDataId = state.CropDataId,
                CurrentStageIndex = state.CurrentStageIndex,
                CurrentStageElapsedDays = state.CurrentStageElapsedDays
            };
        }
    }
}