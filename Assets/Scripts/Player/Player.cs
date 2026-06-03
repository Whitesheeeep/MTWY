using System;
using GameData;
using UnityEngine;
using UnityEngine.EventSystems;
using WS_Modules.CustomEventSystem;
using WS_Modules.Extensions;
using WS_Modules.InputModule;
using WS_Modules.LogModule;
using WS_Modules.Singleton;
using EventSystem = WS_Modules.CustomEventSystem.EventSystem;

/// <summary>
/// Player 运行时入口，负责移动、输入状态暴露和部位 FSM 总控。
/// </summary>
public class Player : AutoSingletonMonoBase<Player>
{
    [SerializeField]
    private SpriteRenderer sr;
    [SerializeField]
    private PlayerACController playerACController;

    private Rigidbody2D rb;
    private PlayerFSMController fsmController;
    private IItemDatabase database;

    /// <summary>
    /// 普通移动速度。
    /// </summary>
    public float speed = 5f;

    /// <summary>
    /// 奔跑移动速度。
    /// </summary>
    public float runSpeed = 8f;

    /// <summary>
    /// 当前输入移动方向。
    /// </summary>
    public Vector2 MoveDir => InputMgr.Instance.MoveDir;

    /// <summary>
    /// 当前是否按下奔跑输入。
    /// </summary>
    public bool IsRunPressed => InputMgr.Instance.IsRunPressed;

    /// <summary>
    /// 最近一次有效移动方向。
    /// </summary>
    public PlayerDirection CurrentDirection => InputMgr.Instance.LastMoveDirection;

    /// <summary>
    /// 最近一次有效移动方向对应的 Vector2。
    /// </summary>
    public Vector2 CurrentDirectionVector => DirectionToVector(CurrentDirection);

    /// <summary>
    /// 手部当前是否处于持有状态。
    /// </summary>
    public bool IsHandHolding { get; private set; }

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
        EventSystem.Register_Int<InventoryBarSlotSelectedEventArgs>((int)E_InventoryEvent.BarSlotSelected,
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
        rb.MovePosition(transform.position + (MoveDir * (currentSpeed * Time.fixedDeltaTime)).ToVector3_XY());
    }

    /// <summary>
    /// 设置手部持有状态。
    /// </summary>
    public void SetHandHolding(bool isHandHolding)
    {
        IsHandHolding = isHandHolding;
    }

    /// <summary>
    /// 设置手部持有状态的兼容入口，保留 Arm 命名调用。
    /// </summary>
    public void SetArmHolding(bool isArmHolding)
    {
        SetHandHolding(isArmHolding);
    }

    /// <summary>
    /// 尝试从 Player AC 注册表获取指定部位的 AC Controller。
    /// </summary>
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

    private void SetSRSprite(Sprite sprite) => sr.sprite = sprite;

    #region BarSlotSelected 事件 Handler
    private void OnBarSlotSelected(InventoryBarSlotSelectedEventArgs args)
    {
        if (args.ItemId == -1)
        {
            ClearHoldState();
            return;
        }

        if (database.TryGet(args.ItemId, out ItemData itemData))
        {
            if (itemData is not { canCarried: true })
            {
                SetSRSprite(null);
                IsHandHolding = false;
                return;
            }

            WSLog.Log("[Player] Holding item: " + itemData.name);
            // 这里可以添加根据选中物品更新玩家状态或动画的逻辑
            SetSRSprite(itemData.worldIcon);
            IsHandHolding = true;
        }
    }

    public void ClearHoldState()
    {
        SetSRSprite(null);
        IsHandHolding = false;
    }
    #endregion
}