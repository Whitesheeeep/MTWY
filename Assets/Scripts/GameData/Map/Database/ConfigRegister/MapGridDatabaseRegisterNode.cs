using UnityEngine;
using WS_Modules.ConfigInstaller;

namespace GameData
{
    /// <summary>
    /// GameDatabase 注册节点，负责在框架启动配置链中注册地图 Grid 数据库。
    /// </summary>
    [CreateAssetMenu(fileName = "MapGridDatabaseRegisterNode", menuName = "GameData/Database/Map Grid Register Node", order = 2)]
    public sealed class MapGridDatabaseRegisterNode : ConfigRegisterNodeBase
    {
        /// <summary>
        /// 注册 IMapGridDatabase 服务本体。当前地图由场景中的 MapGridRuntimeLoader 加载。
        /// </summary>
        public override void Register()
        {
            GameDatabase.Register<IMapGridDatabase>(new MapGridDatabase());
            Debug.Log("[MapGridDatabaseRegisterNode] Registered MapGridDatabase.");
        }
    }
}
