using UnityEngine;
using WS_Modules;

namespace GameData.CharacterSchedule
{
    /// <summary>
    /// 角色静态定义。用于创建运行时状态、生成当前场景 Agent，并提供默认移动参数。
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterDefinition", menuName = "GameData/Character/Definition")]
    public sealed class CharacterDefinition_SO : ScriptableObject
    {
        /// <summary>
        /// 角色唯一 ID。需要与 CharacterSchedule_SO.characterId 保持一致。
        /// </summary>
        public string characterId;

        /// <summary>
        /// 编辑器和调试日志中显示的角色名。
        /// </summary>
        public string displayName;

        /// <summary>
        /// 角色 Agent 预制体资源键。当前通过 PoolManager 使用该 key 生成实体。
        /// </summary>
        [WSAddressableKey]
        public string prefabKey;

        /// <summary>
        /// 没有存档数据时角色所在的默认地图。语义上与 MapGrid mapId / SceneName 对齐。
        /// </summary>
        [WSScene]
        public string defaultMapId;

        /// <summary>
        /// 没有存档数据时角色所在的默认逻辑格子。
        /// </summary>
        public Vector3Int defaultCell;

        /// <summary>
        /// 日程项没有覆盖速度时使用的默认移动速度。
        /// </summary>
        public float defaultMoveSpeed = 2.5f;
    }
}
