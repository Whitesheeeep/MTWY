using System;
using GameData;
using UnityEngine;
using WS_Modules.InputModule;
using WS_Modules.Singleton;

namespace CursorSystem
{
    /// <summary>
    /// Central state owner for the tool cursor, hover target detection, and Grid range checks.
    /// </summary>
    public sealed class CursorManager : SingletonMonoBase<CursorManager>
    {
        [SerializeField] private Sprite defaultCursorIcon;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private LayerMask interactableLayerMask = Physics2D.DefaultRaycastLayers;
        [SerializeField] private Transform rangeOrigin;

        private CursorState currentState;

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
            global::Player player = GetPlayer();
            if (player != null)
            {
                player.ToolChanged += HandleToolChanged;
            }

            RefreshState(true);
        }

        private void OnDisable()
        {
            Player player = GetPlayer();
            if (player != null)
            {
                player.ToolChanged -= HandleToolChanged;
            }
        }

        private void Update()
        {
            RefreshState(false);
        }

        private void HandleToolChanged()
        {
            RefreshState(true);
        }

        private void RefreshState(bool forceNotify)
        {
            CursorState nextState = BuildState();
            if (!forceNotify && currentState.Equals(nextState))
            {
                return;
            }

            currentState = nextState;
            CursorChanged?.Invoke();
        }

        private CursorState BuildState()
        {
            global::Player player = GetPlayer();
            ItemData toolData = player != null ? player.CurrentToolData : null;
            Sprite icon = toolData != null ? toolData.icon : defaultCursorIcon;
            Vector2 mouseScreenPosition = InputMgr.Instance.MouseScreenPosition;
            Vector3 mouseWorldPosition = GetMouseWorldPosition(mouseScreenPosition);

            GameObject target = null;
            IToolInteractable interactable = null;
            CursorTargetType targetType = CursorTargetType.None;
            MapGridCellInfo cellInfo = default;
            bool inToolRange = false;
            bool canInteract = false;
            Vector3Int originCell = Vector3Int.zero;
            Vector3Int targetCell = Vector3Int.zero;

            if (toolData != null)
            {
                bool hasGridRange = TryGetGridRange(mouseWorldPosition, out originCell, out targetCell, out inToolRange);
                if (hasGridRange && inToolRange)
                {
                    canInteract = TryGetEntityInteraction(
                        player,
                        toolData,
                        mouseScreenPosition,
                        mouseWorldPosition,
                        originCell,
                        targetCell,
                        inToolRange,
                        out target,
                        out interactable);

                    if (canInteract)
                    {
                        targetType = CursorTargetType.Entity;
                    }
                    else
                    {
                        canInteract = TryGetMapCellInteraction(toolData, targetCell, out cellInfo);
                        if (canInteract)
                        {
                            targetType = CursorTargetType.MapCell;
                        }
                    }
                }
            }

            CursorVisualState visualState = canInteract ? CursorVisualState.Interactable : CursorVisualState.Normal;
            return new CursorState(
                icon,
                visualState,
                toolData,
                target,
                interactable,
                targetType,
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
            ItemData toolData,
            Vector2 mouseScreenPosition,
            Vector3 mouseWorldPosition,
            Vector3Int originCell,
            Vector3Int targetCell,
            bool inToolRange,
            out GameObject target,
            out IToolInteractable interactable)
        {
            target = null;
            interactable = null;

            Collider2D[] colliders = Physics2D.OverlapPointAll(mouseWorldPosition, interactableLayerMask);
            for (int i = 0; i < colliders.Length; i++)
            {
                IToolInteractable candidate = FindInteractableInParents(colliders[i]);
                if (candidate == null)
                {
                    continue;
                }

                GameObject candidateTarget = colliders[i].gameObject;
                ToolInteractionContext context = new ToolInteractionContext(
                    player,
                    toolData,
                    mouseScreenPosition,
                    mouseWorldPosition,
                    originCell,
                    targetCell,
                    Mathf.Max(0, toolData.itemUseRadius),
                    inToolRange,
                    candidateTarget,
                    CursorTargetType.Entity);

                if (!candidate.CanInteract(context))
                {
                    continue;
                }

                target = candidateTarget;
                interactable = candidate;
                return true;
            }

            return false;
        }

        private bool TryGetMapCellInteraction(
            ItemData toolData,
            Vector3Int targetCell,
            out MapGridCellInfo cellInfo)
        {
            cellInfo = default;
            if (!GameDatabase.TryGet(out IMapGridDatabase mapGrid) || !mapGrid.TryGetCell(targetCell, out cellInfo))
            {
                return false;
            }

            return ToolMapCellInteractionRules.CanInteract(toolData, cellInfo);
        }

        private bool TryGetGridRange(
            Vector3 mouseWorldPosition,
            out Vector3Int originCell,
            out Vector3Int targetCell,
            out bool inToolRange)
        {
            originCell = Vector3Int.zero;
            targetCell = Vector3Int.zero;
            inToolRange = false;

            if (rangeOrigin == null || !GameDatabase.TryGet(out IMapGridDatabase mapGrid) || !mapGrid.HasCurrentGrid)
            {
                return false;
            }

            global::Player player = GetPlayer();
            ItemData toolData = player != null ? player.CurrentToolData : null;
            int radius = toolData != null ? Mathf.Max(0, toolData.itemUseRadius) : 0;
            originCell = mapGrid.WorldToCell(rangeOrigin.position);
            targetCell = mapGrid.WorldToCell(mouseWorldPosition);
            int distance = Mathf.Abs(targetCell.x - originCell.x) + Mathf.Abs(targetCell.y - originCell.y);
            inToolRange = distance <= radius;
            return true;
        }

        private static IToolInteractable FindInteractableInParents(Collider2D collider)
        {
            MonoBehaviour[] behaviours = collider.GetComponentsInParent<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IToolInteractable interactable)
                {
                    return interactable;
                }
            }

            return null;
        }

        private static Player GetPlayer()
        {
            return Player.IsCreated ? Player.Instance : null;
        }
    }
}
