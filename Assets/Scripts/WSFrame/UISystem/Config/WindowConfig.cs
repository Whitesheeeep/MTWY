using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WS_Modules.UIModule
{
    [CreateAssetMenu(fileName = "WindowConfig", menuName = "WSFrame/WindowConfig", order = 0)]
    public class WindowConfig : ScriptableObject
    {
        public List<WindowConfigData> windowConfigList = new();

        public WindowConfigData GetWindowData(string windowName, bool logError = true)
        {
            return windowConfigList.FirstOrDefault(w => w.windowName == windowName);
        }
        
        #if UNITY_EDITOR
        [ContextMenu("GetWindowConfig")]
        public void GeneratorWindowConfig(MenuCommand menuCommand)
        {
            var setting = WSFrameRoot.Instance?.FrameSetting ?? GetSetting();
            string[] windowRootArr = setting.uiManagerSetting.WindowPrefabFolderPathArr;
            
            //检测预制体路径或名称没有改变，如果没有就不需要生成配置
            bool needUpdate = false;
            foreach (var item in windowRootArr)
            {
                string[] filePathArr = Directory.GetFiles(Application.dataPath.Replace("Assets", "") + item, "*.prefab",
                    SearchOption.AllDirectories);
                foreach (var path in filePathArr)
                {
                    if (path.EndsWith(".meta")) continue;
                    WindowConfigData windowData = GetWindowData(Path.GetFileNameWithoutExtension(path), false);

                    string windowPath = windowData == null ? string.Empty : windowData.windowPrefabPath;
                    //路径不存在或路径不一致
                    if (string.IsNullOrEmpty(windowPath) || (!string.IsNullOrEmpty(windowPath) &&
                                                             windowPath.GetHashCode() != path.GetHashCode()))
                    {
                        needUpdate = true;
                        break;
                    }
                }
            }

            if (!needUpdate)
            {
                Debug.Log("预制体个数没有发生改变，不生成窗口配置");
                return;
            }

            windowConfigList.Clear();
            foreach (var item in windowRootArr)
            {
                //获取预制体文件夹读取路径
                string floder = Application.dataPath.Replace("Assets", "") + item;
                //获取文件夹下的所有Prefab文件
                string[] filePathArr = Directory.GetFiles(floder, "*.prefab", SearchOption.AllDirectories);
                foreach (var path in filePathArr)
                {
                    if (path.EndsWith(".meta"))
                    {
                        continue;
                    }

                    //获取预制体名字
                    string fileName = Path.GetFileNameWithoutExtension(path);
                    WindowConfigData data = new WindowConfigData { windowName = fileName, windowPrefabPath = fileName };
                    windowConfigList.Add(data);
                }
            }
// #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
// #endif
        }

        private static WSFrameSetting GetSetting()
        {
            var settings = AssetDatabase.FindAssets("t:WSFrameSetting");
            if (settings.Length == 0)
            {
                Debug.LogError("Can not find WSFrameSetting asset.");
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(settings[0]);
            return AssetDatabase.LoadAssetAtPath<WSFrameSetting>(path);
        }
        #endif
    }

    /// <summary>
    /// 配置 UI window 预制体的名字和路径
    /// </summary>
    [Serializable]
    public class WindowConfigData
    {
        public string windowName;
        public string windowPrefabPath;
    }
}