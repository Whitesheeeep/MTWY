using System.Collections.Generic;
using UnityEngine;

namespace GameData
{
    public sealed class MapGridBakeSource : MonoBehaviour
    {
        public string mapId;
        public MapGridData_SO outputData;
        public List<MapGridTilemapLayer> layers = new List<MapGridTilemapLayer>();
    }
}
