using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using WS_Modules.ResLoadModule;
using WS_Modules.Singleton;

namespace WS_Modules.Pooling
{
    public class PoolManager : SingletonBase<PoolManager>
    {
        private PoolManager()
        {
        }
        
        private GameObjectPoolModule _gameObjectPoolModule;
        private ClassPoolModule _classPoolModule;
        private readonly GlobalPoolPrewarmProcessor _globalPrewarmProcessor = GlobalPoolPrewarmProcessor.Instance;

        public void Initialize(WSFrameSetting.PoolingSetting poolingSetting, IResLoad<string> resLoader = null)
        {
            Init(resLoader ?? GetResLoader(poolingSetting), poolingSetting);
        }

        private void Init(IResLoad<string> gameObjectResLoader, WSFrameSetting.PoolingSetting poolingSetting)
        {
            if (_gameObjectPoolModule != null) return;
            
            var poolRoot = new GameObject("PoolSystemRoot").transform;
            poolRoot.SetParent(WSFrameRoot.Instance.gameObject.transform);
            
            // 使用 ResourcesLoadMgr 作为资源加载器
            _gameObjectPoolModule = new GameObjectPoolModule(poolRoot, gameObjectResLoader);
            _classPoolModule = new ClassPoolModule();

            // 应用全局预热配置
            ApplyGlobalPrewarm(poolingSetting);
        }

        #region Prewarm
        public void Prewarm(string key, int initCount, int maxCapacity) => _gameObjectPoolModule.Prewarm(key, initCount, maxCapacity);

        public void PrewarmClass<T>(int count, int maxCapacity) where T : class, new() => _classPoolModule.Prewarm<T>(count, maxCapacity);

        public async UniTask PrewarmAsync(string key, int initCount, int maxCapacity, UnityAction<bool> onComplete = null) 
            => await _gameObjectPoolModule.PrewarmAsync(key, initCount, maxCapacity, onComplete);
        #endregion

        #region Get
        public GameObject Get<T>(Transform parent = null) where T : IPoolable => _gameObjectPoolModule.Get<T>(parent);
        public GameObject Get(string key, Transform parent = null) => _gameObjectPoolModule.Get(key, parent);
        public List<GameObject> GetSome(string key, int count, Transform parent = null) => _gameObjectPoolModule.GetSome(key, count, parent);

        public async UniTask<GameObject> GetAsync<T>(Transform parent = null) => await _gameObjectPoolModule.GetAsync<T>(parent);
        public async UniTask<GameObject> GetAsync(string key, Transform parent = null) => await _gameObjectPoolModule.GetAsync(key, parent);

        public void GetAsync<T>(Transform parent, UnityAction<GameObject> onComplete) => _gameObjectPoolModule.GetAsync<T>(parent, onComplete);
        public void GetAsync(string key, Transform parent, UnityAction<GameObject> onComplete) => _gameObjectPoolModule.GetAsync(key, parent, onComplete);
        
        /// <summary>
        /// 获取普通类对象
        /// </summary>
        public T GetClass<T>() where T : class, new() => _classPoolModule.Get<T>();
        #endregion

        #region Recycle
        public void Recycle(string key, GameObject go) => _gameObjectPoolModule.Recycle(key, go);
        public void Recycle(GameObject go) => _gameObjectPoolModule.Recycle(go);
        public void RecycleSome(List<GameObject> gos) => _gameObjectPoolModule.RecycleSome(gos);
        
        /// <summary>
        /// 回收普通类对象
        /// </summary>
        public void RecycleClass<T>(T obj) where T : class, new() => _classPoolModule.Recycle(obj);
        #endregion

        #region Clear
        public void ClearPool(string key) => _gameObjectPoolModule.ClearPool(key);
        public void ClearClassPool<T>() => _classPoolModule.Clear<T>();
        
        public void ClearAll()
        {
            _gameObjectPoolModule.ClearAll();
            _classPoolModule.ClearAll();
        }
        #endregion
        
        private IResLoad<string> GetResLoader(WSFrameSetting.PoolingSetting poolingSetting)
        {
            IResLoad<string> resLoader;
            switch (poolingSetting?.ResLoadType ?? E_ResLoadType.Resources)
            {
                case E_ResLoadType.Resources:
                    resLoader = new ResourcesLoadMgrModule();
                    break;
                case E_ResLoadType.Addressable:
                    resLoader = new AddressablesLoadMgrModule();
                    break;
                default:
                    resLoader = new ResourcesLoadMgrModule();
                    break;
            }

            return resLoader;
        }

        private void ApplyGlobalPrewarm(WSFrameSetting.PoolingSetting poolingSetting)
        {
            _globalPrewarmProcessor.SetConfig(poolingSetting?.GlobalPrewarmConfig);
            _globalPrewarmProcessor.Apply(_gameObjectPoolModule, _classPoolModule);
        }
    }
}
