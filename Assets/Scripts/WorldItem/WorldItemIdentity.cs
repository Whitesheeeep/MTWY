using Sirenix.OdinInspector;
using UnityEngine;

namespace WorldItems
{
    /// <summary>
    /// 世界物品运行时身份组件，用于让场景中的 Item 反查到对应的数据记录。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldItemIdentity : MonoBehaviour
    {
        [ReadOnly] public int InstanceId { get; private set; }
        public bool HasIdentity => InstanceId > 0;

        public void Initialize(int instanceId)
        {
            InstanceId = instanceId;
        }

        public void Clear()
        {
            InstanceId = 0;
        }
    }
}
