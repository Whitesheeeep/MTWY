namespace CursorSystem
{
    /// <summary>
    /// 可被光标交互系统识别并响应当前选中物品的世界目标。
    /// </summary>
    public interface IItemInteractable
    {
        /// <summary>
        /// 判断当前选中物品在指定上下文中是否允许与该目标交互。
        /// </summary>
        bool CanInteract(ItemInteractionContext context);

        /// <summary>
        /// 执行当前选中物品对该目标的交互行为。
        /// </summary>
        bool TryInteract(ItemInteractionContext context);
    }
}
