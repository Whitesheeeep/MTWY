using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameData.CharacterSchedule
{
    /// <summary>
    /// 日程条件或规划逻辑访问地图数据的服务。
    /// </summary>
    public interface ICharacterScheduleMapService
    {
        /// <summary>
        /// 确保指定地图的 MapGrid 数据已经加载到缓存。
        /// </summary>
        UniTask<bool> EnsureLoadedAsync(string mapId);

        /// <summary>
        /// 查询指定地图中的 cell 是否可行走。
        /// </summary>
        bool IsWalkable(string mapId, Vector3Int cell);
    }
}
