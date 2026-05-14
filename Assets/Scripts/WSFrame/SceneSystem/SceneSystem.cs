using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

namespace WS_Modules.SceneModule
{
    public static class SceneSystem
    {
        // OnLoadingSceneProgress 事件在每次加载进度更新时触发，参数为当前进度（0-1），与 LoadSceneAsync 的 callBack 参数功能类似，但提供了全局事件的方式，方便其他系统监听加载进度。
        public static event Action<float> OnLoadingSceneProgress;

        /// <summary>
        /// 当开始加载场景时触发，参数为目标场景名称（如果是索引加载，可能为空或索引字符串）
        /// </summary>
        public static event Action<string> OnLoadSceneStart;
        
        /// <summary>
        /// 当场景加载完成时触发
        /// </summary>
        public static event Action OnLoadSceneSucceed;

        /// <summary>
        /// 获取当前活动场景
        /// </summary>
        public static Scene CurrentScene => SceneManager.GetActiveScene();

        /// <summary>
        /// 获取当前场景名称
        /// </summary>
        public static string CurrentSceneName => CurrentScene.name;

        /// <summary>
        /// 获取当前场景索引
        /// </summary>
        public static int CurrentSceneIndex => CurrentScene.buildIndex;

        #region Sync Load
        // LoadSceneMode: Single（默认）会卸载当前场景并加载新场景；Additive 会在当前场景基础上加载新场景，不会卸载当前场景。
        public static void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            OnLoadSceneStart?.Invoke(sceneName);
            SceneManager.LoadScene(sceneName, mode);
            OnLoadSceneSucceed?.Invoke();
        }
        
        public static void LoadScene(int sceneBuildIndex, LoadSceneMode mode = LoadSceneMode.Single)
        {
            OnLoadSceneStart?.Invoke(sceneBuildIndex.ToString());
            SceneManager.LoadScene(sceneBuildIndex, mode);
            OnLoadSceneSucceed?.Invoke();
        }

        // LoadSceneParameters 允许更细粒度的控制加载行为，例如是否允许场景激活、加载时的本地化设置等。
        public static Scene LoadScene(string sceneName, LoadSceneParameters loadSceneParameters)
        {
            OnLoadSceneStart?.Invoke(sceneName);
            var scene = SceneManager.LoadScene(sceneName, loadSceneParameters);
            OnLoadSceneSucceed?.Invoke();
            return scene;
        }

        public static Scene LoadScene(int sceneBuildIndex, LoadSceneParameters loadSceneParameters)
        {
            OnLoadSceneStart?.Invoke(sceneBuildIndex.ToString());
            var scene = SceneManager.LoadScene(sceneBuildIndex, loadSceneParameters);
            OnLoadSceneSucceed?.Invoke();
            return scene;
        }
        #endregion

        #region Async Load (UniTask)

        /// <summary>
        /// 异步加载场景 (UniTask)
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <param name="callBack">回调函数,注意：每次进度更新都会调用一次,参数为0-1的进度</param>
        /// <param name="mode">加载模式</param>
        public static async UniTask LoadSceneAsync(string sceneName, Action<float> callBack = null, LoadSceneMode mode = LoadSceneMode.Single)
        {
            OnLoadSceneStart?.Invoke(sceneName);
            var ao = SceneManager.LoadSceneAsync(sceneName, mode);
            if (ao == null) return;
            
            await DoLoadSceneAsync(ao, callBack);
        }

        /// <summary>
        /// 异步加载场景 (UniTask)
        /// </summary>
        public static async UniTask LoadSceneAsync(int sceneBuildIndex, Action<float> callBack = null, LoadSceneMode mode = LoadSceneMode.Single)
        {
            OnLoadSceneStart?.Invoke(sceneBuildIndex.ToString());
            var ao = SceneManager.LoadSceneAsync(sceneBuildIndex, mode);
            if (ao == null) return;

            await DoLoadSceneAsync(ao, callBack);
        }

        private static async UniTask DoLoadSceneAsync(AsyncOperation ao, Action<float> callBack)
        {
            ao.allowSceneActivation = true;
            while (!ao.isDone)
            {
                // 进度可能会停在 0.9，直到激活场景
                // 这里手动模拟平滑进度直到 1 (如果 ao.isDone 为 true)
                // SceneManager.LoadSceneAsync 如果 allowSceneActivation=true，进度会在加载完变成 1 并 isDone=true
                
                float progress = ao.progress < 0.9f ? ao.progress : 1.0f;

                callBack?.Invoke(progress);
                OnLoadingSceneProgress?.Invoke(progress);

                if (ao.progress >= 0.9f)
                {
                    // 加载完成
                    break;
                }

                await UniTask.Yield();
            }
            
            callBack?.Invoke(1.0f);
            OnLoadingSceneProgress?.Invoke(1.0f);
            OnLoadSceneSucceed?.Invoke();
        }

        /// <summary>
        /// 异步加载场景，加载完成后不立刻切换 (UniTask)
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <param name="activeCallBack">手动激活场景的回调，当场景准备好时调用此回调，传入的 Action 用于执行激活操作</param>
        /// <param name="loadingCallBack">加载时进度回调</param>
        /// <param name="mode">加载模式</param>
        public static async UniTask LoadSceneAsyncWithoutActive(string sceneName, Action<Action> activeCallBack,
            Action<float> loadingCallBack = null, LoadSceneMode mode = LoadSceneMode.Single)
        {
            OnLoadSceneStart?.Invoke(sceneName);
            var ao = SceneManager.LoadSceneAsync(sceneName, mode);
            if (ao == null) return;

            await DoLoadSceneAsyncWithoutActive(ao, activeCallBack, loadingCallBack);
        }

        /// <summary>
        /// 异步加载场景，加载完成后不立刻切换 (UniTask)
        /// </summary>
        public static async UniTask LoadSceneAsyncWithoutActive(int sceneIndex, Action<Action> activeCallBack,
            Action<float> loadingCallBack = null, LoadSceneMode mode = LoadSceneMode.Single)
        {
            OnLoadSceneStart?.Invoke(sceneIndex.ToString());
            var ao = SceneManager.LoadSceneAsync(sceneIndex, mode);
            if (ao == null) return;

            await DoLoadSceneAsyncWithoutActive(ao, activeCallBack, loadingCallBack);
        }

        
        private static async UniTask DoLoadSceneAsyncWithoutActive(AsyncOperation ao,
            Action<Action> activeCallBack, Action<float> loadingCallBack = null)
        {
            ao.allowSceneActivation = false;
            float progress;

            // 当 allowSceneActivation = false 时，progress 最多到 0.9
            while (ao.progress < 0.9f)
            {
                progress = ao.progress;
                loadingCallBack?.Invoke(progress);
                OnLoadingSceneProgress?.Invoke(progress);
                
                await UniTask.Yield();
            }

            // 加载到了 0.9，即使已经 ready，也需要给用户回调去激活
            loadingCallBack?.Invoke(0.9f);
            OnLoadingSceneProgress?.Invoke(0.9f);

            // 等待直到 isDone (其实这里不会 isDone 直到 allowSceneActivation = true)
            // 我们告知用户可以激活了
            bool activated = false;
            
            Action activateScene = () =>
            {
                if (activated) return;
                activated = true;
                ao.allowSceneActivation = true;
            };

            // 触发回调，告诉用户“准备好了，调用传入的 Action 来激活”
            activeCallBack?.Invoke(activateScene);


            // 等待激活完成
            await ao;
            /*hile (!ao.isDone)
            {
                // 激活中
                await UniTask.Yield();
            }*/
            
            // 完成
            loadingCallBack?.Invoke(1.0f);
            OnLoadingSceneProgress?.Invoke(1.0f);
            OnLoadSceneSucceed?.Invoke();
        }

        #endregion
    }
}






