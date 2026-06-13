namespace CursorSystem
{
    /// <summary>
    /// 可被工具检测系统识别的世界交互目标。
    /// </summary>
    public interface IToolInteractable
    {
        /// <summary>
        /// 判断当前工具上下文是否允许与该目标交互。
        /// </summary>
        bool CanInteract(ToolInteractionContext context);
    }
}
