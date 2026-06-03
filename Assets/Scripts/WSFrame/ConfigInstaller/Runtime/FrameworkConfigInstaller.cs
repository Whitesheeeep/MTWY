using UnityEngine;

namespace WS_Modules.ConfigInstaller
{
    /// <summary>
    /// 框架配置安装器。只执行一个根节点，具体配置树由根节点组合。
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)]
    public sealed class FrameworkConfigInstaller : MonoBehaviour
    {
        [SerializeField, Tooltip("配置注册树的根节点。启动时只会从这个节点开始执行 Register，具体子节点由该 RootNode 组合。")]
        private ConfigRegisterNodeBase rootNode;

        [SerializeField, Tooltip("是否在 Awake 时自动执行 rootNode.Register。关闭后可通过右键菜单 Register All 手动执行。")]
        private bool registerOnAwake = true;

        [SerializeField, Tooltip("是否在执行 Register 后销毁安装器对象。")]
        private bool destroyAfterRegister = true;

        private void Awake()
        {
            if (!registerOnAwake)
            {
                return;
            }

            RegisterAll();
        }

        [ContextMenu("Register All")]
        public void RegisterAll()
        {
            rootNode?.Register();

            if (destroyAfterRegister)
            {
                Destroy(this);
            }
        }
    }
}
