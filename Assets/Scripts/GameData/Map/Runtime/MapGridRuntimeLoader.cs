using UnityEngine;

namespace GameData
{
    public sealed class MapGridRuntimeLoader : MonoBehaviour
    {
        [SerializeField] private MapGridData_SO mapGridData;

        private void OnEnable()
        {
            if (mapGridData == null)
            {
                Debug.LogError($"[MapGridRuntimeLoader] MapGridData is not assigned on {name}.");
                return;
            }

            GameDatabase.Get<IMapGridDatabase>().LoadMap(mapGridData);
        }

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
