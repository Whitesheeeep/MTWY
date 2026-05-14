using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using Sirenix.OdinInspector;

/// <summary>
/// TMProTypeWriter 的测试脚本。
/// </summary>
public class TMProTypeWriter_Tester : MonoBehaviour
{
    /// <summary>
    /// 打印状态
    /// </summary>
    private enum TypingState
    {
        Idle,      // 空闲，等待开始
        Typing,    // 正在打印中
        Completed  // 打印完成
    }

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

    private TypingState _currentState = TypingState.Idle;
    private CancellationTokenSource _cancellationTokenSource;

    private void Awake()
    {
        // 初始化用于取消 UniTask 的 CancellationTokenSource
        _cancellationTokenSource = new CancellationTokenSource();
    }

    private void OnDestroy()
    {
        // 在对象销毁时确保取消任务并释放资源
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }

    private void Update()
    {
        // 检测鼠标左键或空格键按下
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            switch (_currentState)
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
    }

    [Button("开始/跳过/重置", ButtonSizes.Large)]
    [PropertyOrder(1)]
    private void HandleClick()
    {
        switch (_currentState)
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
        _currentState = TypingState.Typing;

        // 重置 CancellationTokenSource
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            // 调用新的 ShowText 方法
            await typeWriter.ShowText(textToType, _cancellationTokenSource.Token);
            
            // 如果任务正常完成（未被取消），则更新状态
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                Debug.Log("文本显示完成。");
                _currentState = TypingState.Completed;
            }
        }
        catch (System.OperationCanceledException)
        {
            // 当任务被取消时（例如通过 SkipTyping），这里会捕获异常
            Debug.Log("文本显示被跳过。");
            _currentState = TypingState.Completed;
        }
    }

    private void SkipTyping()
    {
        if (typeWriter == null) return;

        Debug.Log("跳过文本显示...");
        
        // 调用新的 Skip 方法
        typeWriter.Skip();
    }
    
    private void ResetTyping()
    {
        if (typeWriter == null) return;

        Debug.Log("重置状态。");
        
        // 清空文本并重置状态
        typeWriter.ShowText("", CancellationToken.None).Forget();
        _currentState = TypingState.Idle;
    }
}
