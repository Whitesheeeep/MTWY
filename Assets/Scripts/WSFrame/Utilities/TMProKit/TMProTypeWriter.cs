using Cysharp.Threading.Tasks;
using System.Threading;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))] // 确保该组件始终附加到具有 TMP_Text 组件的游戏对象上
public class TMProTypeWriter : MonoBehaviour
{
    /// <summary>
    /// 文本显示模式
    /// </summary>
    public enum RevealMode
    {
        [Tooltip("直接显示全部文本")]
        Direct,
        [Tooltip("所有文字同时淡入")]
        Fade,
        [Tooltip("逐字打印")]
        Typing
    }
    
    /// <summary>
    /// 打字机当前状态
    /// </summary>
    public enum WriterState
    {
        [Tooltip("空闲")]
        Idle,
        [Tooltip("正在显示中")]
        Revealing,
        [Tooltip("显示完成")]
        Completed
    }
    
    [Tooltip("当前打字机的状态")]
    [LabelText("当前状态")]
    [ShowInInspector, ReadOnly]
    public WriterState CurrentState { get; private set; } = WriterState.Idle;

    [Header("显示设置")]
    [Tooltip("控制文本显示的模式")]
    [LabelText("显示模式")]
    public RevealMode revealMode = RevealMode.Typing;
    
    [Header("控制打字或淡入的速度")]
    [LabelText("打字速度/淡入速度")]
    [Range(0f, 100f)]
    public float typingSpeed = 20f;
    [LabelText("显示速度（每秒进度 0-1）")]
    [Range(0f, 1f)]
    public float fadeSpeed = .5f;
    
    [Space]
    [LabelText("不允许直接显示文本时间段（秒）")]
    [Tooltip("在文本开始显示后的这段时间内，跳过功能将被禁用，防止误操作")]
    public float noSkipDuration = 1f;
    private float _revealStartTime = 0f; // 记录文本开始显示的时间

    private TMP_Text _textMeshPro; // TextMeshPro 组件的引用
    private CancellationTokenSource _cancellationTokenSource; // 用于取消 UniTask 任务

    private void Awake()
    {
        _textMeshPro = GetComponent<TMP_Text>(); // 获取 TextMeshPro 组件
    }

    private void OnDestroy()
    {
        _cancellationTokenSource?.Cancel(); // 在对象销毁时取消所有正在运行的异步任务
        _cancellationTokenSource?.Dispose(); // 释放 CancellationTokenSource 占用的资源
    }

    /// <summary>
    /// 根据选择的模式显示文本
    /// </summary>
    /// <param name="text">要显示的文本</param>
    /// <param name="cancellationToken">用于外部取消的 Token</param>
    public async UniTask ShowText(string text, CancellationToken cancellationToken = default)
    {
        // 如果传入空字符串，则视为空闲状态
        if (string.IsNullOrEmpty(text))
        {
            CurrentState = WriterState.Idle;
            _textMeshPro.text = string.Empty;
            return;
        }
        
        CurrentState = WriterState.Revealing; // 设置状态为显示中
        
        // 准备新的 CancellationTokenSource 用于管理本次任务
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _cancellationTokenSource.Token;

        // 设置文本并强制更新网格信息
        _textMeshPro.text = text;
        _textMeshPro.ForceMeshUpdate();
        _revealStartTime = Time.time; // 记录文本开始显示的时间
        
        // 如果后续想要使用的模式变多，可以考虑使用策略模式或工厂模式来管理不同的显示方式
        try
        {
            switch (revealMode)
            {
                case RevealMode.Direct:
                    SetAllCharactersAlpha(255);
                    _textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                    break;
                
                case RevealMode.Fade:
                    await FadeInTextAsync(token);
                    break;

                case RevealMode.Typing:
                    await RevealTextAsync(token);
                    break;
            }
        }
        catch (System.OperationCanceledException)
        {
            // 捕获并忽略取消异常
            Debug.Log("文本显示任务被取消。");
        }
        finally
        {
            // 无论任务是完成还是被取消，最终都将状态设置为完成
            if (CurrentState == WriterState.Revealing)
            {
                CurrentState = WriterState.Completed;
            }
        }
    }
    
    /// <summary>
    /// 直接跳过动画，立即显示所有文本
    /// </summary>
    public void Skip()
    {
        if (CurrentState != WriterState.Revealing) return; // 如果不是正在显示中，则不执行任何操作
        
        if (Time.time - _revealStartTime < noSkipDuration)
        {
            Debug.Log("跳过功能暂时不可用。");
            return; 
        } 
        // 取消任何正在运行的异步任务，这会导致 ShowText 中的 catch 块被执行
        _cancellationTokenSource?.Cancel();
        
        // 立即显示所有文本
        SetAllCharactersAlpha(255);
        _textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        
        CurrentState = WriterState.Completed; // 更新状态为完成
    }

    /// <summary>
    /// 异步逐字显示文本
    /// </summary>
    private async UniTask RevealTextAsync(CancellationToken cancellationToken)
    {
        SetAllCharactersAlpha(0); // 开始前确保所有字符透明
        
        TMP_TextInfo textInfo = _textMeshPro.textInfo;
        int totalCharacters = textInfo.characterCount;

        for (int i = 0; i < totalCharacters; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (textInfo.characterInfo[i].isVisible)
            {
                SetCharacterAlpha(i, 255);
                _textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            }

            await UniTask.Delay((int)(100 / typingSpeed), cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// 异步淡入显示所有文本（优化版）
    /// </summary>
    private async UniTask FadeInTextAsync(CancellationToken cancellationToken)
    {
        // 定义一个固定的更新间隔，例如 33 毫秒，约等于 30fps
        const int FADE_UPDATE_INTERVAL_MS = 33;

        SetAllCharactersAlpha(0); // 开始前确保所有字符透明
        
        float currentAlpha = 0f;
        while (currentAlpha < 255f)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 根据速度和固定的时间间隔计算 alpha 增量
            // speed * (33/1000) * 255, 这里的 speed 表示每秒淡入的进度（0-1）
            // 为了让 speed 的值更直观，我们调整一下计算
            float alphaIncrement = fadeSpeed * (FADE_UPDATE_INTERVAL_MS / 1000f) * 255f;
            currentAlpha = Mathf.Min(255f, currentAlpha + alphaIncrement);
            
            SetAllCharactersAlpha((byte)currentAlpha);
            _textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            // 等待固定的时间间隔
            await UniTask.Delay(FADE_UPDATE_INTERVAL_MS, cancellationToken: cancellationToken);
        }
        
        // 确保最终 alpha 为 255
        SetAllCharactersAlpha(255);
        _textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    /// <summary>
    /// 设置所有字符的Alpha值
    /// </summary>
    private void SetAllCharactersAlpha(byte alpha)
    {
        // 必须在修改前确保 textInfo 是最新的
        if (!_textMeshPro.textInfo.Equals(default(TMP_TextInfo)))
        {
            for (int i = 0; i < _textMeshPro.textInfo.characterCount; i++)
            {
                SetCharacterAlpha(i, alpha);
            }
        }
    }

    /// <summary>
    /// 设置单个字符的Alpha值
    /// </summary>
    private void SetCharacterAlpha(int charIndex, byte alpha)
    {
        if (charIndex >= _textMeshPro.textInfo.characterCount)
            return;

        TMP_CharacterInfo charInfo = _textMeshPro.textInfo.characterInfo[charIndex];
        
        // 跳过不可见字符，比如空格，换行等
        if (!charInfo.isVisible)
            return;

        // 通过 charInfo 获取材质索引和顶点索引，materialReferenceIndex 表示使用的材质索引，对应 meshInfo 数组
        // 确定在哪个材质的网格中修改顶点颜色
        int materialIndex = charInfo.materialReferenceIndex;
        // meshInfo 是一个数组，每个材质对应一个 meshInfo
        // 具体类型 TMP_MeshInfo 包含顶点、UV、颜色等信息，我们需要修改颜色信息
        Color32[] vertexColors = _textMeshPro.textInfo.meshInfo[materialIndex].colors32;
        // 确定在该层数组中的起始位置
        int vertexIndex = charInfo.vertexIndex;

        // 设置字符的四个顶点的Alpha值，使其透明或不透明，顺序为：bottom left, top left, top right, bottom right
        vertexColors[vertexIndex + 0].a = alpha;
        vertexColors[vertexIndex + 1].a = alpha;
        vertexColors[vertexIndex + 2].a = alpha;
        vertexColors[vertexIndex + 3].a = alpha;
    }
}
