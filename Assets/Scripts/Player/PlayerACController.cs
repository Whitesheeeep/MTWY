using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player 动画 Controller 注册表，负责收集子部位 AC Controller。
/// </summary>
public class PlayerACController : MonoBehaviour
{
    private readonly Dictionary<PlayerPartType, PlayerPartACController> partControllers = new();

    private void Awake()
    {
        RegisterChildren();
    }

    private void RegisterChildren()
    {
        partControllers.Clear();

        PlayerPartACController[] controllers = GetComponentsInChildren<PlayerPartACController>(true);
        for (int i = 0; i < controllers.Length; i++)
        {
            PlayerPartACController controller = controllers[i];
            if (controller == null)
            {
                continue;
            }

            if (partControllers.ContainsKey(controller.PartType))
            {
                Debug.LogWarning($"Duplicate player part AC controller: {controller.PartType}", controller);
                continue;
            }

            partControllers.Add(controller.PartType, controller);
        }
    }

    /// <summary>
    /// 尝试获取指定部位的 AC Controller。
    /// </summary>
    public bool TryGet(PlayerPartType partType, out PlayerPartACController controller)
    {
        return partControllers.TryGetValue(partType, out controller);
    }
}
