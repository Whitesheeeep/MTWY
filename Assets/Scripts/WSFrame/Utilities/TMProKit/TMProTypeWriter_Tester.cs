using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// TMProTypeWriter 的测试脚本。
/// </summary>
public class TMProTypeWriter_Tester : MonoBehaviour
{
    #region Enums
    /// <summary>
    /// 打印状态。
    /// </summary>
    private enum TypingState
    {
        Idle,
        Typing,
        Completed
    }
    #endregion

    #region Inspector
    [Header("组件引用")]
    [Tooltip("需要测试的 TMProTypeWriter 组件")]
    [LabelText("打字机组件")]
    [Required("请将场景中的 TMProTypeWriter 组件拖拽到此处")]
    public TMProTypeWriter typeWriter;

    [Header("测试设置")]
    [Tooltip("要进行打印测试的文本内容")]
    [LabelText("测试文本")]
    [TextArea(3, 5)]
    public string textToType = "这是一段用于测试的文本。\n点击鼠标或按空格键开始，再次点击可跳过。";
    #endregion

    #region Fields
    private TypingState currentState = TypingState.Idle;
    #endregion

    #region Unity Lifecycle
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            HandleClick();
        }
    }
    #endregion

    #region Test API
    [Button("开始/跳过/重置", ButtonSizes.Large)]
    [PropertyOrder(1)]
    private void HandleClick()
    {
        switch (currentState)
        {
            case TypingState.Idle:
                StartTyping();
                break;
            case TypingState.Typing:
                SkipTyping();
                break;
            case TypingState.Completed:
                ResetTyping();
                break;
        }
    }

    private async void StartTyping()
    {
        if (typeWriter == null)
        {
            Debug.LogError("错误：打字机组件（TypeWriter）未在 Inspector 中设置！");
            return;
        }

        Debug.Log("开始显示文本...");
        currentState = TypingState.Typing;

        await typeWriter.ShowText(textToType);

        Debug.Log("文本显示完成。");
        currentState = TypingState.Completed;
    }

    private void SkipTyping()
    {
        if (typeWriter == null)
        {
            return;
        }

        Debug.Log("跳过文本显示...");
        typeWriter.Skip();
    }

    private void ResetTyping()
    {
        if (typeWriter == null)
        {
            return;
        }

        Debug.Log("重置状态。");
        typeWriter.StopReveal(true);
        currentState = TypingState.Idle;
    }
    #endregion
}
