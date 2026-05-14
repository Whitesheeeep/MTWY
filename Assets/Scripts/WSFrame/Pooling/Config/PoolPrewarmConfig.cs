using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace WS_Modules.Pooling
{
    /// <summary>
    /// 全局对象池预热配置资源。
    /// 该类只保存配置数据，不执行预热逻辑；实际应用由 <see cref="GlobalPoolPrewarmProcessor"/> 完成。
    /// </summary>
    [CreateAssetMenu(fileName = "PoolPrewarmConfig", menuName = "WSFrame/PoolPrewarmConfig", order = 0)]
    public class PoolPrewarmConfig : ScriptableObject
    {
        /// <summary>
        /// GameObject 对象池预热列表。key 对应资源加载路径或资源标识。
        /// </summary>
        [LabelText("GameObject Prewarm Items")]
        public List<GameObjectPoolPrewarmItem> gameObjectPrewarmItems = new();

        /// <summary>
        /// 普通 class 对象池预热列表。class 类型由生成的 <see cref="ClassPoolPrewarmId"/> 标识。
        /// </summary>
        [LabelText("Class Prewarm Items")]
        public List<ClassPoolPrewarmItem> classPrewarmItems = new();
    }

    /// <summary>
    /// 单个 GameObject 对象池预热配置项。
    /// </summary>
    [Serializable]
    public class GameObjectPoolPrewarmItem
    {
        /// <summary>
        /// 是否启用该配置项。
        /// </summary>
        [LabelText("Enabled")]
        public bool enable = true;

        /// <summary>
        /// 资源加载 key，例如 Resources 下的 Cube 或 TestFolder/Cube1。
        /// </summary>
        [LabelText("Resource Key")]
        [Tooltip("Resource load key, for example Cube or TestFolder/Cube1 under Resources.")]
        public string key;

        /// <summary>
        /// 初始化时预创建的对象数量。
        /// </summary>
        [LabelText("Init Count")]
        public int initCount = 1;

        /// <summary>
        /// 池最大容量，-1 表示不限制容量。
        /// </summary>
        [LabelText("Max Capacity")]
        [Tooltip("-1 means unlimited capacity.")]
        public int maxCapacity = -1;
    }

    /// <summary>
    /// 单个普通 class 对象池预热配置项。
    /// 类型选择来自生成的 ClassPoolPrewarmRegistry，运行时不进行 Type.GetType 字符串解析。
    /// </summary>
    [Serializable]
    public class ClassPoolPrewarmItem
    {
        /// <summary>
        /// 是否启用该配置项。
        /// </summary>
        [LabelText("Enabled")]
        public bool enable = true;

        /// <summary>
        /// 由 Editor 生成器生成的稳定类型 ID。
        /// </summary>
        [LabelText("Class Type")]
        [ValueDropdown(nameof(GetClassIdDropdown))]
        [OnValueChanged(nameof(OnClassIdChanged))]
        public ClassPoolPrewarmId classId;

        /// <summary>
        /// 仅用于 Inspector 展示的类型名称，不参与运行时查找。
        /// </summary>
        [LabelText("Display Name"), ReadOnly]
        public string displayName;

        /// <summary>
        /// 初始化时预创建的对象数量。
        /// </summary>
        [LabelText("Init Count")]
        public int initCount = 1;

        /// <summary>
        /// 池最大容量，-1 表示不限制容量。
        /// </summary>
        [LabelText("Max Capacity")]
        [Tooltip("-1 means unlimited capacity.")]
        public int maxCapacity = -1;

#if UNITY_EDITOR
        /// <summary>
        /// Odin 下拉选项。显示名称来自生成表，保存值为 ClassPoolPrewarmId。
        /// </summary>
        private IEnumerable<ValueDropdownItem<ClassPoolPrewarmId>> GetClassIdDropdown()
        {
            yield return new ValueDropdownItem<ClassPoolPrewarmId>("None", ClassPoolPrewarmId.None);

            foreach (var entry in ClassPoolPrewarmRegistry.Entries)
            {
                yield return new ValueDropdownItem<ClassPoolPrewarmId>(entry.DisplayName, entry.Id);
            }
        }

        /// <summary>
        /// 同步 Inspector 展示名称，便于在折叠状态下快速确认配置类型。
        /// </summary>
        private void OnClassIdChanged()
        {
            displayName = ClassPoolPrewarmRegistry.GetDisplayName(classId);
        }
#endif
    }
}
