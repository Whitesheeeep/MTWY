using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 场景内地图运行时加载器，负责把本场景对应的 MapGridData_SO 加载进 MapGridDatabase。
    /// </summary>
    public sealed class MapGridRuntimeLoader : MonoBehaviour
    {
        /// <summary>
        /// 当前场景对应的地图静态数据。
        /// </summary>
        [SerializeField] private MapGridData_SO mapGridData;

        /// <summary>
        /// 场景启用时加载地图数据。
        /// </summary>
        private void OnEnable()
        {
            if (mapGridData == null)
            {
                Debug.LogError($"[MapGridRuntimeLoader] MapGridData is not assigned on {name}.");
                return;
            }

            GameDatabase.Get<IMapGridDatabase>().LoadMap(mapGridData);
        }

        /// <summary>
        /// 场景卸载或对象禁用时，仅当数据库当前持有自己的数据时才卸载。
        /// </summary>
        private void OnDisable()
        {
            if (!GameDatabase.TryGet(out IMapGridDatabase database))
            {
                return;
            }

            if (database.CurrentMapData == mapGridData)
            {
                database.UnloadCurrentMap();
            }
        }
    }
}
