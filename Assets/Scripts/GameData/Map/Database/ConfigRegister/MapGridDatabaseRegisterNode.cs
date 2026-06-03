using UnityEngine;
using WS_Modules.ConfigInstaller;

namespace GameData
{
    [CreateAssetMenu(fileName = "MapGridDatabaseRegisterNode", menuName = "GameData/Database/Map Grid Register Node", order = 2)]
    public sealed class MapGridDatabaseRegisterNode : ConfigRegisterNodeBase
    {
        public override void Register()
        {
            GameDatabase.Register<IMapGridDatabase>(new MapGridDatabase());
            Debug.Log("[MapGridDatabaseRegisterNode] Registered MapGridDatabase.");
        }
    }
}
