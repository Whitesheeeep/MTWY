using UnityEngine;
using WS_Modules.ConfigInstaller;

namespace GameData.SceneTransition
{
    [CreateAssetMenu(fileName = "SceneTransitionRegisterNode", menuName = "GameData/Scene/Transition Register Node")]
    public sealed class SceneTransitionRegisterNode : ConfigRegisterNodeBase
    {
        [SerializeField] private SceneTransitionGraph_SO transitionGraph;

        public override void Register()
        {
            SceneTransitionSystem.Initialize(transitionGraph);
        }
    }
}
