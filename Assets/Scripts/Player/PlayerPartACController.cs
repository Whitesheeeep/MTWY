using UnityEngine;

/// <summary>
/// Player 单个部位的 Animator 适配器，只负责播放状态名和更新方向参数。
/// </summary>
public class PlayerPartACController : MonoBehaviour
{
    private static readonly int DirX = Animator.StringToHash("DirX");
    private static readonly int DirY = Animator.StringToHash("DirY");

    [SerializeField] private PlayerPartType partType;
    [SerializeField] private Animator animator;

    /// <summary>
    /// 当前 AC Controller 对应的 Player 部位。
    /// </summary>
    public PlayerPartType PartType => partType;

    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    /// <summary>
    /// 播放 Animator Controller 中的指定短状态名。
    /// </summary>
    public void Play(string animationName)
    {
        if (animator == null)
        {
            Debug.LogError($"Missing Animator on {nameof(PlayerPartACController)}: {partType}", this);
            return;
        }

        if (string.IsNullOrEmpty(animationName))
        {
            Debug.LogError($"Invalid animation name for player part: {partType}", this);
            return;
        }

        animator.Play(animationName);
    }

    /// <summary>
    /// 设置二维方向参数 DirX 和 DirY。
    /// </summary>
    public void SetDirection(Vector2 direction)
    {
        if (animator == null)
        {
            Debug.LogError($"Missing Animator on {nameof(PlayerPartACController)}: {partType}", this);
            return;
        }

        animator.SetFloat(DirX, direction.x);
        animator.SetFloat(DirY, direction.y);
    }
}
