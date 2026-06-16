using UnityEngine;
using WS_Modules.ConfigInstaller;

namespace GameData
{
    /// <summary>
    /// Registers the MapGrid database and injects the optional multi-map catalog.
    /// </summary>
    [CreateAssetMenu(fileName = "MapGridDatabaseRegisterNode", menuName = "GameData/Database/Map Grid Register Node", order = 2)]
    public sealed class MapGridDatabaseRegisterNode : ConfigRegisterNodeBase
    {
        [SerializeField] private MapGridCatalog_SO catalog;

        public override void Register()
        {
            GameDatabase.Register<IMapGridDatabase>(new MapGridDatabase(catalog));
            Debug.Log("[MapGridDatabaseRegisterNode] Registered MapGridDatabase.");
        }
    }
}
