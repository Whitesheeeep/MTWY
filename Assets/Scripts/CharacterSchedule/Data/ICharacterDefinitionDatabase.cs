namespace GameData.CharacterSchedule
{
    /// <summary>
    /// 角色静态定义数据库。注册到 GameDatabase 后供 AgentManager 等运行时系统查询。
    /// </summary>
    public interface ICharacterDefinitionDatabase : IGameSubDatabase
    {
        /// <summary>
        /// 通过角色 ID 查询角色静态定义。
        /// </summary>
        bool TryGet(string characterId, out CharacterDefinition_SO definition);
    }
}
