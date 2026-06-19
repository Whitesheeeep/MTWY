using System;
using CursorSystem;
using GameData;
using UnityEngine;
using WS_Modules.CustomEventSystem;
using WS_Modules.Extensions;
using WS_Modules.InputModule;
using WS_Modules.LogModule;
using WS_Modules.Singleton;
using EventSystem = WS_Modules.CustomEventSystem.EventSystem;

/// <summary>
/// Player runtime entry. Owns movement input state, body FSM, hold state, and current selected tool data.
/// </summary>
public class Player : AutoSingletonMonoBase<Player>
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private PlayerACController playerACController;

    private Rigidbody2D rb;
    private PlayerFSMController fsmController;
    private IItemDatabase database;

    public float speed = 5f;
    public float runSpeed = 8f;

    public Vector2 MoveDir => InputMgr.Instance.MoveDir;
    public bool IsRunPressed => InputMgr.Instance.IsRunPressed;
    public PlayerDirection CurrentDirection => InputMgr.Instance.LastMoveDirection;
    public Vector2 CurrentDirectionVector => DirectionToVector(CurrentDirection);
    public bool IsHandHolding { get; private set; }

    /// <summary>
    /// Current selected tool data. Empty, invalid, or non-tool bar selections clear this value.
    /// </summary>
    public ItemData CurrentToolData { get; private set; }

    public bool HasCurrentTool => CurrentToolData != null;
    public E_ItemType CurrentToolType => CurrentToolData != null ? CurrentToolData.itemType : E_ItemType.None;
    public event Action ToolChanged;

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody2D>();
        rb.velocity = Vector3.zero;

        if (playerACController == null)
        {
            playerACController = GetComponent<PlayerACController>();
        }

        if (playerACController == null)
        {
            playerACController = GetComponentInChildren<PlayerACController>(true);
        }
    }

    private void OnEnable()
    {
        EventSystem.Register_Int<InventoryBarSlotSelectedEventArgs>(
                (int)E_InventoryEvent.BarSlotSelected,
                OnBarSlotSelected)
            .UnRegisterWhenGameObjectDisabled(gameObject);
    }

    private void Start()
    {
        fsmController = new PlayerFSMController(this);
        fsmController.OnEnter();

        database = GameDatabase.Get<IItemDatabase>();
    }

    private void Update()
    {
        fsmController?.OnUpdate();
    }

    private void FixedUpdate()
    {
        fsmController?.OnFixedUpdate();

        float currentSpeed = IsRunPressed ? runSpeed : speed;
        rb.MovePosition(rb.position + (MoveDir * (currentSpeed * Time.fixedDeltaTime)));
    }

    public void SetHandHolding(bool isHandHolding)
    {
        IsHandHolding = isHandHolding;
    }

    public void SetArmHolding(bool isArmHolding)
    {
        SetHandHolding(isArmHolding);
    }

    public bool TryGetPartAC(PlayerPartType partType, out PlayerPartACController controller)
    {
        controller = null;

        if (playerACController == null)
        {
            Debug.LogError("Missing PlayerACController.", this);
            return false;
        }

        return playerACController.TryGet(partType, out controller);
    }

    public void ClearHoldState()
    {
        SetSRSprite(null);
        IsHandHolding = false;
    }

    private static Vector2 DirectionToVector(PlayerDirection direction)
    {
        switch (direction)
        {
            case PlayerDirection.Up:
                return Vector2.up;
            case PlayerDirection.Left:
                return Vector2.left;
            case PlayerDirection.Right:
                return Vector2.right;
            case PlayerDirection.Down:
            default:
                return Vector2.down;
        }
    }

    private void SetSRSprite(Sprite sprite)
    {
        sr.sprite = sprite;
    }

    private void OnBarSlotSelected(InventoryBarSlotSelectedEventArgs args)
    {
        if (args.ItemId == -1)
        {
            ClearHoldState();
            SetCurrentTool(null);
            return;
        }

        if (!EnsureItemDatabase() || !database.TryGet(args.ItemId, out ItemData itemData))
        {
            ClearHoldState();
            SetCurrentTool(null);
            return;
        }

        if (itemData is { canCarried: true })
        {
            WSLog.Log("[Player] Holding item: " + itemData.name);
            SetSRSprite(itemData.worldIcon);
            IsHandHolding = true;
        }
        else
        {
            SetSRSprite(null);
            IsHandHolding = false;
        }

        SetCurrentTool(ToolTypeUtility.IsTool(itemData.itemType) ? itemData : null);
    }

    private void SetCurrentTool(ItemData itemData)
    {
        if (ReferenceEquals(CurrentToolData, itemData))
        {
            return;
        }

        CurrentToolData = itemData;
        ToolChanged?.Invoke();
    }

    private bool EnsureItemDatabase()
    {
        if (database != null)
        {
            return true;
        }

        return GameDatabase.TryGet(out database);
    }
}
