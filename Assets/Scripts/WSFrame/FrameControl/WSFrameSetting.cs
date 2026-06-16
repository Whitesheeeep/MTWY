using System;
using Sirenix.OdinInspector;
using UnityEngine;
using WS_Modules.Pooling;
using WS_Modules.UIModule;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
#endif

namespace WS_Modules
{
    [CreateAssetMenu(fileName = "FrameSetting", menuName = "WSFrame/FrameSetting", order = 0)]
    public class WSFrameSetting : ScriptableObject
    {
        [LabelText("Log 控制")]
        public LogSetting logSetting = new LogSetting();

        [LabelText("资源加载方式"), EnumToggleButtons]
        [InfoBox("请处理 Resources 文件夹", InfoMessageType.Warning, "@resLoadType == E_ResLoadType.Addressable")]
        public E_ResLoadType resLoadType = E_ResLoadType.Resources;

        [LabelText("音量系统设置")]
        public AudioSystemSetting audioSystemSetting = new AudioSystemSetting();

        [LabelText("UI 管理设置")]
        public UIManagerSetting uiManagerSetting = new UIManagerSetting();

        [SerializeField, LabelText("对象池设置")]
        private PoolingSetting poolingSetting = new PoolingSetting();

        public PoolingSetting PoolingSettings
        {
            get
            {
                poolingSetting ??= new PoolingSetting();
                poolingSetting.SetResLoadType(resLoadType);
                return poolingSetting;
            }
        }


        [Serializable]
        public class LogSetting
        {
            [Header("Log 各种颜色")]
            public Color infoColor = Color.white;
            public Color warningColor = Color.yellow;
            public Color errorColor = Color.red;
            public Color succeedColor = Color.green;
            [LabelText("是否启用日志系统"), OnValueChanged("EnableLogValueChanged")]
            public bool enableLog = true;
            [LabelText("是否写入时间戳"), OnValueChanged("EnableLogValueChanged")]
            public bool enableWriteTime = true;
            [LabelText("是否写入线程ID"), OnValueChanged("EnableLogValueChanged")]
            public bool enableWriteThreadID = false;
            [LabelText("是否写入堆栈信息"), OnValueChanged("EnableLogValueChanged")]
            public bool enableWriteTrace = true;
            [LabelText("是否保存日志到文件"), OnValueChanged("EnableLogValueChanged")]
            public bool enableSaveToFile = false;
            [LabelText("保存日志类型"), HideIf("CheckSaveState"), OnValueChanged("EnableLogValueChanged")]
            public WS_Modules.LogModule.LogLevel saveLogTypes = WS_Modules.LogModule.LogLevel.All;
            [LabelText("自定义保存文件名（为空则使用默认文件名，会根据时间创建），并且为覆盖式的"), HideIf("CheckSaveState"),
             OnValueChanged("EnableLogValueChanged")]
            [InfoBox("自定义文件名会覆盖默认的按时间命名的日志文件，并且是覆盖式的保存")]
            public string customSaveFileName = "";
            [LabelText("保存路径（相对于持久化数据路径）"), HideIf("CheckSaveState"), OnValueChanged("EnableLogValueChanged")]
            public string savePath = "/WSFrame/Logs/";

            public void Reset()
            {
                infoColor = Color.white;
                warningColor = Color.yellow;
                errorColor = Color.red;
                succeedColor = Color.green;

                enableLog = true;
                enableWriteTime = true;
                enableWriteThreadID = false;
                enableWriteTrace = true;
                enableSaveToFile = false;
                saveLogTypes = LogModule.LogLevel.All;
                customSaveFileName = "";
                savePath = "/WSFrame/Logs/";
            }

#if UNITY_EDITOR
            /// <summary>
            /// 在编辑器中初始化设置变更监听
            /// </summary>
            public void InitOnEditor()
            {
                EnableLogValueChanged();
            }

            [Button("打开日志保存目录"), HideIf("CheckSaveState")]
            private void OpenLogSaveDirectory()
            {
                string fullPath = savePath.StartsWith("/")
                    ? Application.persistentDataPath + savePath
                    : Application.persistentDataPath + "/" + savePath;
                System.IO.Directory.CreateDirectory(fullPath); // 确保目录存在
                EditorUtility.RevealInFinder(fullPath);
            }

            private bool CheckSaveState()
            {
                return !enableSaveToFile;
            }

            private void EnableLogValueChanged()
            {
                // 启用或禁用日志系统，通过 符号 ENABLE_LOG 控制
                if (enableLog)
                {
                    AddScriptCompilationSymbol("WS_LOG_ENABLED");
                }
                else
                {
                    RemoveScriptCompilationSymbol("WS_LOG_ENABLED");
                }
            }

            private void RemoveScriptCompilationSymbol(string enableLOG)
            {
                // 获取当前的编译平台
                var currentPlatform = UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup;
                NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(currentPlatform);
                // 获取当前的脚本编译符号
#if UNITY_2022_2_OR_NEWER
                string symbols = UnityEditor.PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
#else
            string symbols = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(currentPlatform);
#endif
                // 移除指定的符号
                if (symbols.Contains(enableLOG))
                    UnityEditor.PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget,
                        symbols.Replace(";" + enableLOG, string.Empty));
            }

            private void AddScriptCompilationSymbol(string enableLOG)
            {
                // 获取当前的编译平台
                var currentPlatform = UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup;
                NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(currentPlatform);
                // 获取当前的脚本编译符号
#if UNITY_2022_2_OR_NEWER
                string symbols = UnityEditor.PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
#else
            string symbols = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(currentPlatform);
#endif
                // 添加指定的符号
                if (!symbols.Contains(enableLOG))
                    UnityEditor.PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, symbols + ";" + enableLOG);
            }
#endif
        }

        [Serializable]
        public class PoolingSetting
        {
            [LabelText("全局预热配置")]
            public PoolPrewarmConfig GlobalPrewarmConfig;

            public E_ResLoadType ResLoadType { get; private set; }

            public void SetResLoadType(E_ResLoadType loadType)
            {
                ResLoadType = loadType;
            }
        }

        [Serializable]
        public class AudioSystemSetting
        {
            public int audioSourceInitCount = 5;
            public string audioSourcePrefabPath;
        }

        [Serializable]
        public class UIManagerSetting
        {
            [Tooltip("UI 根节点预制体的资源加载路径。")]
            public string uiRootPath;

            [Tooltip("UI Camera 预制体的资源加载路径。")]
            public string uiCameraPrefabPath;

            [Tooltip("UI EventSystem 预制体的资源加载路径。")]
            public string uiEventSystemPrefabPath;

            [Tooltip("窗口配置表，记录窗口名称和窗口预制体加载路径。")]
            public WindowConfig windowConfig;

            [Tooltip("是否使用单遮罩模式。启用后 UIManager 会在当前顶层窗口上显示唯一遮罩。")]
            public bool isSingleMask;

            [Tooltip("组件绑定脚本生成路径。")]
            [WSFolderPath]
            public string BindComponentGeneratorPath = "";

            [Tooltip("组件绑定脚本生成时使用的命名空间。")]
            public string BindComponentNameSpace = "";

            [Tooltip("窗口交互脚本生成路径。")]
            [WSFolderPath]
            public string WindowGeneratorPath = "";

            [Tooltip("Item 脚本生成路径。")]
            [WSFolderPath]
            public string ItemScriptsGeneratorPath = "";

            [Tooltip("窗口预制体存放路径。框架会根据这些路径自动计算窗口加载路径，新增窗口无需手动配置。")]
            [WSFolderPath]
            public string[] WindowPrefabFolderPathArr;

            [Tooltip("自动生成脚本时需要额外引入的命名空间。")]
            [WSFolderPath]
            public string[] UsingNameSpaceArr;
        }
    }


    public enum E_ResLoadType
    {
        Addressable = 0,
        Resources = 1
    }
}
