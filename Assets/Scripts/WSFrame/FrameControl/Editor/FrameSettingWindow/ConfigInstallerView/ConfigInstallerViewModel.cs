using System;
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using WS_Modules.ConfigInstaller;
using Object = UnityEngine.Object;

namespace WS_Modules
{
    internal sealed class ConfigInstallerViewModel
    {
        private const string ConfigAssetFolder = "Assets/Scripts/WSFrame/ConfigInstaller/Assets";
        private const string InstallerPrefabPath = "Assets/Scripts/WSFrame/ConfigInstaller/Assets/FrameworkConfigInstaller.prefab";

        private readonly List<TreeViewItemData<ConfigTreeNodeViewData>> rootItems = new();
        private readonly Dictionary<int, ConfigTreeNodeViewData> nodeMap = new();
        private int nextId;

        public event Action StateChanged;
        public event Action TreeChanged;
        public event Action SelectionChanged;

        public FrameworkConfigInstaller Installer { get; private set; }
        public bool IsUsingPrefabInstaller { get; private set; }
        public ConfigRegisterNodeBase RootNode { get; private set; }
        public ConfigTreeNodeViewData SelectedNode { get; private set; }
        public IList<TreeViewItemData<ConfigTreeNodeViewData>> RootItems => rootItems;

        public void Refresh()
        {
            Installer = ResolveInstaller(out bool isPrefab);
            IsUsingPrefabInstaller = isPrefab;
            RootNode = Installer == null ? null : GetInstallerRootNode(Installer);
            RebuildTree();
            StateChanged?.Invoke();
            TreeChanged?.Invoke();
            SelectionChanged?.Invoke();
        }

        public void SetInstaller(FrameworkConfigInstaller installer)
        {
            Installer = installer;
            IsUsingPrefabInstaller = installer != null && EditorUtility.IsPersistent(installer);
            RootNode = Installer == null ? null : GetInstallerRootNode(Installer);
            RebuildTree();
            StateChanged?.Invoke();
            TreeChanged?.Invoke();
            SelectionChanged?.Invoke();
        }

        public void SetRootNode(ConfigRegisterNodeBase rootNode)
        {
            if (Installer == null)
            {
                return;
            }

            // FrameworkConfigInstaller 的字段是 private SerializeField，这里统一通过 SerializedObject 修改，
            // 避免为编辑器面板额外暴露运行时 API。
            SerializedObject serializedInstaller = new SerializedObject(Installer);
            SerializedProperty rootProperty = serializedInstaller.FindProperty("rootNode");
            rootProperty.objectReferenceValue = rootNode;
            serializedInstaller.ApplyModifiedProperties();
            EditorUtility.SetDirty(Installer);
            SaveAssets();

            RootNode = rootNode;
            RebuildTree();
            StateChanged?.Invoke();
            TreeChanged?.Invoke();
            SelectionChanged?.Invoke();
        }

        public void Select(ConfigTreeNodeViewData node)
        {
            SelectedNode = node;
            SelectionChanged?.Invoke();
        }

        public void RefreshTreeFromModel()
        {
            RebuildTree();
            TreeChanged?.Invoke();
            SelectionChanged?.Invoke();
        }

        public FrameworkConfigRootNode CreateOrFindRootNode()
        {
            FrameworkConfigRootNode root = FindFirstAsset<FrameworkConfigRootNode>();
            if (root == null)
            {
                root = CreateNodeAsset<FrameworkConfigRootNode>("FrameworkConfigRootNode");
            }

            SetRootNode(root);
            return root;
        }

        public GameDatabaseRegisterModule CreateOrFindGameDatabaseModule()
        {
            return FindFirstAsset<GameDatabaseRegisterModule>() ??
                   CreateNodeAsset<GameDatabaseRegisterModule>("GameDatabaseRegisterModule");
        }

        public ItemDatabaseRegisterNode CreateOrFindItemDatabaseNode()
        {
            return FindFirstAsset<ItemDatabaseRegisterNode>() ??
                   CreateNodeAsset<ItemDatabaseRegisterNode>("ItemDatabaseRegisterNode");
        }

        public void AddChildToSelectedComposite(ConfigRegisterNodeBase child)
        {
            if (SelectedNode?.Node is not CompositeConfigRegisterNode composite || child == null)
            {
                return;
            }

            AddChild(composite, child);
        }

        public void AddChildToRoot(ConfigRegisterNodeBase child)
        {
            if (RootNode is CompositeConfigRegisterNode composite && child != null)
            {
                AddChild(composite, child);
            }
        }

        public void RemoveSelectedNode()
        {
            if (SelectedNode?.Parent?.Node is not CompositeConfigRegisterNode parent || SelectedNode.Node == null)
            {
                return;
            }

            SerializedObject serializedParent = new SerializedObject(parent);
            SerializedProperty children = serializedParent.FindProperty("children");
            int index = FindChildIndex(children, SelectedNode.Node);
            if (index < 0)
            {
                return;
            }

            children.DeleteArrayElementAtIndex(index);
            serializedParent.ApplyModifiedProperties();
            EditorUtility.SetDirty(parent);
            SaveAssets();
            SelectedNode = SelectedNode.Parent;
            RebuildTree();
            TreeChanged?.Invoke();
            SelectionChanged?.Invoke();
        }

        public void MoveSelectedNode(int offset)
        {
            if (SelectedNode?.Parent?.Node is not CompositeConfigRegisterNode parent || SelectedNode.Node == null)
            {
                return;
            }

            SerializedObject serializedParent = new SerializedObject(parent);
            SerializedProperty children = serializedParent.FindProperty("children");
            int oldIndex = FindChildIndex(children, SelectedNode.Node);
            int newIndex = oldIndex + offset;
            if (oldIndex < 0 || newIndex < 0 || newIndex >= children.arraySize)
            {
                return;
            }

            children.MoveArrayElement(oldIndex, newIndex);
            serializedParent.ApplyModifiedProperties();
            EditorUtility.SetDirty(parent);
            SaveAssets();
            RebuildTree();
            TreeChanged?.Invoke();
            SelectionChanged?.Invoke();
        }

        public void AddGameDatabaseModuleToRoot()
        {
            GameDatabaseRegisterModule module = CreateOrFindGameDatabaseModule();
            AddChildToRoot(module);
        }

        public void AddItemNodeToSelectedOrGameDatabaseModule()
        {
            ItemDatabaseRegisterNode itemNode = CreateOrFindItemDatabaseNode();
            CompositeConfigRegisterNode target = SelectedNode?.Node as CompositeConfigRegisterNode;
            target ??= FindFirstAsset<GameDatabaseRegisterModule>();
            if (target != null)
            {
                AddChild(target, itemNode);
            }
        }

        public void RegisterAll()
        {
            Installer?.RegisterAll();
        }

        public void Ping(Object target)
        {
            if (target == null)
            {
                return;
            }

            EditorGUIUtility.PingObject(target);
            Selection.activeObject = target;
        }

        private void AddChild(CompositeConfigRegisterNode parent, ConfigRegisterNodeBase child)
        {
            // ViewModel 只关心 ConfigRegisterNodeBase 树结构，不读取 ItemDataList_SO 等具体业务配置。
            // 具体节点资产的字段由右侧 Inspector 自己绘制和保存。
            SerializedObject serializedParent = new SerializedObject(parent);
            SerializedProperty children = serializedParent.FindProperty("children");
            if (FindChildIndex(children, child) >= 0)
            {
                Debug.LogWarning($"[ConfigInstaller] Child already exists: {child.name}");
                return;
            }

            int index = children.arraySize;
            children.InsertArrayElementAtIndex(index);
            children.GetArrayElementAtIndex(index).objectReferenceValue = child;
            serializedParent.ApplyModifiedProperties();
            EditorUtility.SetDirty(parent);
            SaveAssets();
            RebuildTree();
            TreeChanged?.Invoke();
            SelectionChanged?.Invoke();
        }

        private void RebuildTree()
        {
            ConfigRegisterNodeBase previousSelection = SelectedNode?.Node;
            rootItems.Clear();
            nodeMap.Clear();
            nextId = 1;

            if (RootNode == null)
            {
                SelectedNode = null;
                return;
            }

            TreeViewItemData<ConfigTreeNodeViewData> rootItem = BuildTreeItem(RootNode, null, 0);
            rootItems.Add(rootItem);

            SelectedNode = FindViewData(previousSelection) ?? rootItem.data;
        }

        private TreeViewItemData<ConfigTreeNodeViewData> BuildTreeItem(
            ConfigRegisterNodeBase node,
            ConfigTreeNodeViewData parent,
            int depth)
        {
            int id = nextId++;
            ConfigTreeNodeViewData viewData = new ConfigTreeNodeViewData(id, depth, node, parent);
            nodeMap[id] = viewData;

            List<TreeViewItemData<ConfigTreeNodeViewData>> childrenItems = new List<TreeViewItemData<ConfigTreeNodeViewData>>();
            if (node is CompositeConfigRegisterNode composite)
            {
                SerializedObject serializedNode = new SerializedObject(composite);
                SerializedProperty children = serializedNode.FindProperty("children");
                for (int i = 0; i < children.arraySize; i++)
                {
                    ConfigRegisterNodeBase child = children.GetArrayElementAtIndex(i).objectReferenceValue as ConfigRegisterNodeBase;
                    childrenItems.Add(BuildTreeItem(child, viewData, depth + 1));
                }
            }

            return new TreeViewItemData<ConfigTreeNodeViewData>(id, viewData, childrenItems);
        }

        private static FrameworkConfigInstaller ResolveInstaller(out bool isPrefab)
        {
            isPrefab = false;
            FrameworkConfigInstaller sceneInstaller = Resources.FindObjectsOfTypeAll<FrameworkConfigInstaller>()
                .FirstOrDefault(installer => installer != null && !EditorUtility.IsPersistent(installer));
            if (sceneInstaller != null)
            {
                return sceneInstaller;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(InstallerPrefabPath);
            FrameworkConfigInstaller prefabInstaller = prefab == null ? null : prefab.GetComponent<FrameworkConfigInstaller>();
            isPrefab = prefabInstaller != null;
            return prefabInstaller;
        }

        private static ConfigRegisterNodeBase GetInstallerRootNode(FrameworkConfigInstaller installer)
        {
            SerializedObject serializedInstaller = new SerializedObject(installer);
            return serializedInstaller.FindProperty("rootNode").objectReferenceValue as ConfigRegisterNodeBase;
        }

        private static int FindChildIndex(SerializedProperty children, ConfigRegisterNodeBase child)
        {
            for (int i = 0; i < children.arraySize; i++)
            {
                if (children.GetArrayElementAtIndex(i).objectReferenceValue == child)
                {
                    return i;
                }
            }

            return -1;
        }

        private ConfigTreeNodeViewData FindViewData(ConfigRegisterNodeBase node)
        {
            if (node == null)
            {
                return null;
            }

            foreach (ConfigTreeNodeViewData viewData in nodeMap.Values)
            {
                if (viewData.Node == node)
                {
                    return viewData;
                }
            }

            return null;
        }

        private static T FindFirstAsset<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length == 0)
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static T CreateNodeAsset<T>(string fileName) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{ConfigAssetFolder}/{fileName}.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return asset;
        }

        private static void SaveAssets()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
