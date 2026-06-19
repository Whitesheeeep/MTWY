using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameData.CharacterSchedule
{
    /// <summary>
    /// 角色静态定义数据库运行时实现。
    /// </summary>
    public sealed class CharacterDefinitionDatabase : ICharacterDefinitionDatabase
    {
        private readonly Dictionary<string, CharacterDefinition_SO> definitions =
            new Dictionary<string, CharacterDefinition_SO>(StringComparer.Ordinal);

        /// <summary>
        /// 构建数据库并加载初始定义。
        /// </summary>
        public CharacterDefinitionDatabase(IEnumerable<CharacterDefinition_SO> sourceDefinitions)
        {
            Load(sourceDefinitions);
        }

        /// <summary>
        /// 重新加载角色定义集合。重复 characterId 会保留第一次出现的定义。
        /// </summary>
        public void Load(IEnumerable<CharacterDefinition_SO> sourceDefinitions)
        {
            definitions.Clear();
            if (sourceDefinitions == null)
            {
                return;
            }

            foreach (CharacterDefinition_SO definition in sourceDefinitions)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.characterId))
                {
                    continue;
                }

                if (definitions.ContainsKey(definition.characterId))
                {
                    Debug.LogWarning($"[CharacterDefinitionDatabase] Duplicate characterId ignored: {definition.characterId}", definition);
                    continue;
                }

                definitions.Add(definition.characterId, definition);
            }
        }

        /// <summary>
        /// 通过角色 ID 查询角色定义。
        /// </summary>
        public bool TryGet(string characterId, out CharacterDefinition_SO definition)
        {
            definition = null;
            return !string.IsNullOrWhiteSpace(characterId) && definitions.TryGetValue(characterId, out definition);
        }

        /// <summary>
        /// 获取全部角色定义。ScheduleManager 初始化默认状态时使用。
        /// </summary>
        public IEnumerable<CharacterDefinition_SO> GetAll()
        {
            return definitions.Values;
        }

        /// <summary>
        /// 清空数据库。
        /// </summary>
        public void Clear()
        {
            definitions.Clear();
        }
    }
}
