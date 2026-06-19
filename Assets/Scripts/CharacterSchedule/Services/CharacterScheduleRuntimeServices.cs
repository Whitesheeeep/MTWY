using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Gameplay.TimeSystem;
using UnityEngine;

namespace GameData.CharacterSchedule
{
    /// <summary>
    /// 当前 Entry 评估期间的只读上下文服务。
    /// </summary>
    internal sealed class CharacterScheduleEvaluationService : ICharacterScheduleEvaluationService
    {
        public string CharacterId { get; private set; }
        public CharacterRuntimeState State { get; private set; }
        public CharacterScheduleEntry Entry { get; private set; }

        /// <summary>
        /// 开始评估一个 Entry。
        /// </summary>
        public void Begin(string characterId, CharacterRuntimeState state, CharacterScheduleEntry entry)
        {
            CharacterId = characterId;
            State = state;
            Entry = entry;
        }

        /// <summary>
        /// 结束当前 Entry 评估，清空临时上下文。
        /// </summary>
        public void End()
        {
            CharacterId = string.Empty;
            State = null;
            Entry = null;
        }
    }

    /// <summary>
    /// GameTimeManager 的适配服务。
    /// </summary>
    internal sealed class CharacterScheduleTimeService : ICharacterScheduleTimeService
    {
        public GameTimeData CurrentTime => GameTimeManager.Instance != null
            ? GameTimeManager.Instance.CurrentTime.Value
            : default;
    }

    /// <summary>
    /// CharacterScheduleManager 内部 flag 字典的只读适配服务。
    /// </summary>
    internal sealed class CharacterScheduleFlagService : ICharacterScheduleFlagService
    {
        private readonly Dictionary<string, bool> flags;

        public CharacterScheduleFlagService(Dictionary<string, bool> flags)
        {
            this.flags = flags;
        }

        public bool GetFlag(string flagId)
        {
            return !string.IsNullOrWhiteSpace(flagId) &&
                   flags.TryGetValue(flagId, out bool value) &&
                   value;
        }
    }

    /// <summary>
    /// MapGridManager 的适配服务。
    /// </summary>
    internal sealed class CharacterScheduleMapService : ICharacterScheduleMapService
    {
        public UniTask<bool> EnsureLoadedAsync(string mapId)
        {
            return MapGridManager.Instance.EnsureLoadedAsync(mapId);
        }

        public bool IsWalkable(string mapId, Vector3Int cell)
        {
            return MapGridManager.Instance.IsWalkable(mapId, cell);
        }
    }
}
