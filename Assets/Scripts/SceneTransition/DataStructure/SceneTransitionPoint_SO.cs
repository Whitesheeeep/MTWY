using UnityEngine;
using WS_Modules;

namespace GameData.SceneTransition
{
    /// <summary>
    /// A named logical point in one scene. The cell is authoritative; worldOffset only fine tunes
    /// the final entity position after converting the cell through the loaded MapGrid.
    /// </summary>
    [CreateAssetMenu(fileName = "SceneTransitionPoint", menuName = "GameData/Scene/Transition Point")]
    public sealed class SceneTransitionPoint_SO : ScriptableObject
    {
        public string displayName;

        [WSScene]
        public string sceneName;

        public Vector3Int cell;
        public Vector3 worldOffset;
    }
}
