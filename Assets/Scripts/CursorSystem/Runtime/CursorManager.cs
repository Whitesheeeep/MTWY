using System;
using GameData;
using UnityEngine;
using WS_Modules.InputModule;
using WS_Modules.Singleton;

namespace CursorSystem
{
    /// <summary>
    /// 光标系统的中心状态拥有者，负责悬停目标检测、Grid 范围判断和光标状态刷新。
    /// </summary>
    public sealed class CursorManager : SingletonMonoBase<CursorManager>
    {
        [SerializeField] private Sprite defaultCursorIcon;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private LayerMask interactableLayerMask = Physics2D.DefaultRaycastLayers;
        [SerializeField] private Transform rangeOrigin;

        private const int InteractableHitCapacity = 16;
        private readonly Collider2D[] interactableHits = new Collider2D[InteractableHitCapacity];

        private CursorState currentState;
        private bool loggedInteractableHitOverflow;

        public event Action CursorChanged;

        public CursorState CurrentState => currentState;

        protected override void Awake()
        {
            base.Awake();
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }
        }

        private void OnEnable()
        {
            Player player = GetPlayer();
            if (player != null)
            {
                player.SelectedItemChanged += HandleSelectedItemChanged;
            }

            RefreshState(true);
        }

        private void OnDisable()
        {
            Player player = GetPlayer();
            if (player != null)
            {
                player.SelectedItemChanged -= HandleSelectedItemChanged;
            }
        }

        private void Update()
        {
            RefreshState(false);
        }

        private void HandleSelectedItemChanged()
        {
            RefreshState(true);
        }

        private void RefreshState(bool forceNotify)
        {
            CursorState nextState = BuildState();
            bool shouldNotify = forceNotify || !currentState.Equals(nextState);
            currentState = nextState;

            if (!shouldNotify)
            {
                return;
            }

            CursorChanged?.Invoke();
        }

        private CursorState BuildState()
        {
            global::Player player = GetPlayer();
            ItemData selectedItemData = player != null ? player.CurrentSelectedItemData : null;
            Sprite icon = selectedItemData != null ? selectedItemData.icon : defaultCursorIcon;
            Vector2 mouseScreenPosition = InputMgr.Instance.MouseScreenPosition;
            Vector3 mouseWorldPosition = GetMouseWorldPosition(mouseScreenPosition);

            GameObject target = null;
            IItemInteractable interactable = null;
            CursorTargetType targetType = CursorTargetType.None;
            ItemInteractionContext interactionContext = default;
            bool hasInteractionContext = false;
            MapGridCellInfo cellInfo = default;
            bool inToolRange = false;
            bool canInteract = false;
            Vector3Int originCell = Vector3Int.zero;
            Vector3Int targetCell = Vector3Int.zero;

            if (selectedItemData == null)
            {
                return new CursorState(
                    icon,
                    CursorVisualState.Normal,
                    selectedItemData,
                    target,
                    interactable,
                    targetType,
                    interactionContext,
                    hasInteractionContext,
                    cellInfo,
                    inToolRange,
                    canInteract,
                    originCell,
                    targetCell);
            }

            MapGridManager mapGrid = MapGridManager.Instance;
            bool hasGridRange = TryGetGridRange(
                mapGrid,
                selectedItemData,
                mouseWorldPosition,
                out originCell,
                out targetCell,
                out inToolRange);
            if (hasGridRange && inToolRange)
            {
                bool hasCellInfo = mapGrid.TryGetCell(targetCell, out cellInfo);
                if (hasCellInfo)
                {
                    canInteract = TryGetEntityInteraction(
                        player,
                        selectedItemData,
                        mouseScreenPosition,
                        mouseWorldPosition,
                        originCell,
                        targetCell,
                        cellInfo,
                        inToolRange,
                        out target,
                        out interactable,
                        out interactionContext);

                    if (canInteract)
                    {
                        hasInteractionContext = true;
                        targetType = CursorTargetType.Entity;
                    }
                    else
                    {
                        canInteract = TryGetMapCellInteraction(selectedItemData, cellInfo);
                        if (canInteract)
                        {
                            interactionContext = new ItemInteractionContext(
                                player,
                                selectedItemData,
                                mouseScreenPosition,
                                mouseWorldPosition,
                                originCell,
                                targetCell,
                                Mathf.Max(0, selectedItemData.itemUseRadius),
                                inToolRange,
                                null,
                                CursorTargetType.MapCell,
                                cellInfo);
                            hasInteractionContext = true;
                            targetType = CursorTargetType.MapCell;
                        }
                    }
                }
            }

            CursorVisualState visualState = canInteract ? CursorVisualState.Interactable : CursorVisualState.Normal;
            return new CursorState(
                icon,
                visualState,
                selectedItemData,
                target,
                interactable,
                targetType,
                interactionContext,
                hasInteractionContext,
                cellInfo,
                inToolRange,
                canInteract,
                originCell,
                targetCell);
        }

        private Vector3 GetMouseWorldPosition(Vector2 mouseScreenPosition)
        {
            Camera camera = worldCamera != null ? worldCamera : Camera.main;
            if (camera == null)
            {
                return Vector3.zero;
            }

            Vector3 screenPosition = new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, -camera.transform.position.z);
            return camera.ScreenToWorldPoint(screenPosition);
        }

        private bool TryGetEntityInteraction(
            global::Player player,
            ItemData selectedItemData,
            Vector2 mouseScreenPosition,
            Vector3 mouseWorldPosition,
            Vector3Int originCell,
            Vector3Int targetCell,
            MapGridCellInfo cellInfo,
            bool inToolRange,
            out GameObject target,
            out IItemInteractable interactable,
            out ItemInteractionContext interactionContext)
        {
            target = null;
            interactable = null;
            interactionContext = default;

            int hitCount = Physics2D.OverlapPointNonAlloc(mouseWorldPosition, interactableHits, interactableLayerMask);
            if (hitCount == interactableHits.Length && !loggedInteractableHitOverflow)
            {
                loggedInteractableHitOverflow = true;
                Debug.LogWarning($"[CursorManager] Interactable hit buffer is full ({interactableHits.Length}). Increase capacity to avoid missed hover targets.", this);
            }

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = interactableHits[i];
                IItemInteractable candidate = FindInteractableInParents(hit);
                if (candidate == null)
                {
                    continue;
                }

                GameObject candidateTarget = hit.gameObject;
                ItemInteractionContext context = new ItemInteractionContext(
                    player,
                    selectedItemData,
                    mouseScreenPosition,
                    mouseWorldPosition,
                    originCell,
                    targetCell,
                    Mathf.Max(0, selectedItemData.itemUseRadius),
                    inToolRange,
                    candidateTarget,
                    CursorTargetType.Entity,
                    cellInfo);

                if (!candidate.CanInteract(context))
                {
                    continue;
                }

                target = candidateTarget;
                interactable = candidate;
                interactionContext = context;
                return true;
            }

            return false;
        }

        private bool TryGetMapCellInteraction(
            ItemData selectedItemData,
            MapGridCellInfo cellInfo)
        {
            return ItemMapCellInteractionRules.CanInteract(selectedItemData, cellInfo);
        }

        private bool TryGetGridRange(
            MapGridManager mapGrid,
            ItemData selectedItemData,
            Vector3 mouseWorldPosition,
            out Vector3Int originCell,
            out Vector3Int targetCell,
            out bool inToolRange)
        {
            originCell = Vector3Int.zero;
            targetCell = Vector3Int.zero;
            inToolRange = false;

            if (rangeOrigin == null || !mapGrid.HasCurrentGrid)
            {
                return false;
            }

            int radius = Mathf.Max(0, selectedItemData.itemUseRadius);
            originCell = mapGrid.WorldToCell(rangeOrigin.position);
            targetCell = mapGrid.WorldToCell(mouseWorldPosition);
            int distance = Mathf.Abs(targetCell.x - originCell.x) + Mathf.Abs(targetCell.y - originCell.y);
            inToolRange = distance <= radius;
            return true;
        }

        private static IItemInteractable FindInteractableInParents(Collider2D collider)
        {
            return collider.GetComponentInParent<IItemInteractable>();
        }

        private static Player GetPlayer()
        {
            return Player.IsCreated ? Player.Instance : null;
        }
    }
}
