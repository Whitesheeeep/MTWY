using System.Collections.Generic;
using UnityEngine;

namespace GameData
{
    [CreateAssetMenu(fileName = "MapGridData", menuName = "GameData/Map/Grid Data", order = 0)]
    public sealed class MapGridData_SO : ScriptableObject
    {
        public string mapId;
        public string sceneName;
        public Vector3Int originCell;
        public int width;
        public int height;
        public Vector3 cellSize = Vector3.one;
        public List<MapGridCellData> cells = new List<MapGridCellData>();

        public bool IsValid => !string.IsNullOrWhiteSpace(mapId) && width > 0 && height > 0 && cells != null;
    }
}
