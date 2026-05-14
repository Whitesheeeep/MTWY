using System.Diagnostics;
using UnityEngine;

namespace WS_Modules.LogModule
{
    /// <summary>
    /// 使用外观模式简化日志记录操作的自定义日志类
    /// </summary>
    public static class WSLog
    {
        private static bool isInitialized = false;
        static WSLog()
        {
            Init();
        }

        private static void Init()
        {
            if (WSFrameRoot.Instance.FrameSetting is not null)
                Init(WSFrameRoot.Instance.FrameSetting.logSetting);
            else
            {
                LogManager.Initialize();
            }
            isInitialized = true;
        }

        public static void Init(WSFrameSetting.LogSetting logSetting)
        {
            LogManager.Initialize(
                "#"+ColorUtility.ToHtmlStringRGB(logSetting.infoColor),
                "#"+ColorUtility.ToHtmlStringRGB(logSetting.succeedColor),
                "#"+ColorUtility.ToHtmlStringRGB(logSetting.warningColor),
                "#"+ColorUtility.ToHtmlStringRGB(logSetting.errorColor),
                logSetting.enableWriteTime,
                logSetting.enableWriteThreadID, 
                logSetting.enableWriteTrace, 
                logSetting.enableSaveToFile, 
                logSetting.saveLogTypes, 
                logSetting.customSaveFileName,
                Application.persistentDataPath + logSetting.savePath,
                LoggerType.Unity, 5);
        }

        [Conditional("WS_LOG_ENABLED")]
        public static void Log(string message)
        {
            if (!isInitialized)
                Init();
            LogManager.Log(message);
        }

        [Conditional("WS_LOG_ENABLED")]
        public static void LogSuccess(string message)
        {
            if (!isInitialized)
                Init();
            LogManager.Succeed(message);
        }

        [Conditional("WS_LOG_ENABLED")]
        public static void LogWarning(string message)
        {
            if (!isInitialized)
                Init();
            LogManager.Warning(message);
        }


        [Conditional("WS_LOG_ENABLED")]
        public static void LogError(string message)
        {
            if (!isInitialized)
                Init();
            LogManager.Error(message);
        }
    }
}