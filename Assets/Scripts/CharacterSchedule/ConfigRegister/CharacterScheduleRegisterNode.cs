using System.Collections.Generic;
using GameData;
using UnityEngine;
using WS_Modules.ConfigInstaller;

namespace GameData.CharacterSchedule
{
    /// <summary>
    /// 通过 ConfigInstaller 注入角色定义和日程配置。
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterScheduleRegisterNode", menuName = "GameData/Character/Schedule Register Node")]
    public sealed class CharacterScheduleRegisterNode : ConfigRegisterNodeBase
    {
        /// <summary>
        /// 参与运行时的角色静态定义。
        /// </summary>
        [SerializeField] private List<CharacterDefinition_SO> definitions = new List<CharacterDefinition_SO>();

        /// <summary>
        /// 参与运行时评估的角色日程配置。
        /// </summary>
        [SerializeField] private List<CharacterSchedule_SO> schedules = new List<CharacterSchedule_SO>();

        /// <summary>
        /// 注册角色定义数据库，并初始化 CharacterScheduleManager。
        /// </summary>
        public override void Register()
        {
            CharacterDefinitionDatabase definitionDatabase = new CharacterDefinitionDatabase(definitions);
            GameDatabase.Register<ICharacterDefinitionDatabase>(definitionDatabase);
            CharacterScheduleManager.Instance.Initialize(definitionDatabase, schedules);

            Debug.Log($"[CharacterScheduleRegisterNode] Registered {definitions?.Count ?? 0} character definitions and {schedules?.Count ?? 0} schedules.");
        }
    }
}
