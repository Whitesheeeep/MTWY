using System;
using UnityEngine.Tilemaps;

namespace GameData
{
    [Serializable]
    public sealed class MapGridTilemapLayer
    {
        public Tilemap tilemap;
        public MapGridCellFlags flags;
        public bool affectsBounds = true;
    }
}
