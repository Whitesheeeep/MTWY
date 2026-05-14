using System;
using UnityEngine;
using WS_Modules.Extensions;
using WS_Modules.InputModule;
using WS_Modules.Singleton;

public class Player : AutoSingletonMonoBase<Player>
{
    private Rigidbody2D rb;

    public float speed = 5f;

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody2D>();
        rb.velocity = Vector3.zero; // 确保初始速度为零，避免物理引擎的残留影响
    }

    private void Start()
    {

    }

    private void FixedUpdate()
    {
        rb.MovePosition(transform.position + (InputMgr.Instance.MoveDir * (speed * Time.fixedDeltaTime)).ToVector3_XY());
    }
}
