using System.Collections.Generic;
using UnityEngine;
using WS_Modules.LogModule;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;
using WS_Modules.ResLoadModule;

namespace WS_Modules.Pooling
{
    /// <summary>
    /// 游戏对象池模块：提供基于预制体的对象池功能，支持同步和异步获取与回收，预热功能，以及池容量管理。
    /// - 不关心抽屉的实现细节，专注于对象池的核心功能和接口设计，方便后续替换抽屉实现或扩展其他类型的池数据结构。
    /// - 不关心资源加载的实现细节，通过 IResLoad 接口抽象资源加载逻辑，方便后续替换资源加载系统或支持不同类型的资源加载需求。
    /// - 需要自行传入对象的 key, initCount 和 maxCapacity 来预热池子，预热时会自动创建一个新的池子并加载指定数量的对象实例到池中，方便后续获取时直接复用。
    /// </summary>
    public class GameObjectPoolModule
    {
        // 整个对象池的根对象
        private Transform poolRootTransform;
        // 存储所有池子的数据结构
        private Dictionary<string, GameObjectPoolData> PoolDic = new();
        // 用于创建对象的工厂，避免直接依赖资源加载系统，方便后续替换和扩展
        private IResLoad<string> gameObjectResLoader;

        /// <summary>
        /// 构造函数，接受一个 Transform 作为对象池的根节点，以及一个 IResLoad 资源加载器来加载预制体资源，确保对象池模块的独立性和可替换性。
        /// </summary>
        /// <param name="poolRootTransform">该池子模块的根节点</param>
        /// <param name="gameObjectResLoader"></param>
        public GameObjectPoolModule(Transform poolRootTransform, IResLoad<string> gameObjectResLoader)
        {
            this.poolRootTransform = poolRootTransform ?? new GameObject("ObjectPoolRoot").transform;
            this.gameObjectResLoader = gameObjectResLoader;
        }

        public void Prewarm(GameObject prefab, int initCount, int maxCapacity)
        {
            if (prefab == null)
            {
                WSLog.LogWarning($"Prewarm: prefab is null.");
                return;
            }

            string key = prefab.name;
            if (!CheckPrewarmValid(key, initCount, maxCapacity)) return;

            var poolData = GetOrCreatePrewarmPool(key, maxCapacity);
            int needed = initCount - poolData.Count;
            if (needed <= 0) return;

            // 预热时直接使用传入的预制体实例作为第一个对象，避免重复加载资源。
            PrewarmObjects(poolData, key, prefab, needed, false);
        }

        /// <summary>
        /// 对象池预热：提前创建一定数量的对象实例并放入池中，减少后续获取时的性能开销，适用于需要在游戏开始时就准备好一定数量对象的情况，避免在游戏过程中频繁加载资源和实例化对象导致的性能问题。
        /// 预热时会自动创建一个新的池子并加载指定数量的对象实例到池中，方便后续获取时直接复用。要求预热的对象名称 key、初始数量 initCount 和最大容量 maxCapacity 参数必须有效，否则预热会失败并输出警告日志。
        /// 预热完成后，池子中会有 initCount 个对象实例可供获取，池子的最大容量为 maxCapacity，超过容量限制的对象在回收时会被丢弃而不是放入池中。
        /// </summary> <param name="key">对象名称 key</param>
        /// <param name="initCount">初始数量</param>
        /// <param name="maxCapacity">最大容量</param>
        public void Prewarm(string key, int initCount, int maxCapacity)
        {
            if (!CheckPrewarmValid(key, initCount, maxCapacity)) return;

            var poolData = GetOrCreatePrewarmPool(key, maxCapacity);
            int needed = initCount - poolData.Count;
            if (needed <= 0) return;

            var prefab = gameObjectResLoader.Load<GameObject>(key);
            if (prefab == null)
            {
                WSLog.LogWarning($"Prewarm: no prefab found for key '{key}'.");
                return;
            }

            PrewarmObjects(poolData, key, prefab, needed, false);
        }

        /// <summary>
        /// 对象池预热的异步版本，适用于需要从远程或异步资源系统加载预制体的情况，避免在主线程等待资源加载完成。
        /// 可以直接使用 async/await 来调用这个方法，或者使用回调接口来获取预热完成的通知。
        /// 也可以直接用 Forget() 实现一发即弃的预热调用，适用于不关心预热完成时机的情况。
        /// </summary>
        /// <param name="key">对象名称 key</param>
        /// <param name="initCount">初始数量</param>
        /// <param name="maxCapacity">最大容量</param>
        /// <param name="onComplete">异步版本预热完成后的回调，返回一个bool表示预热是否成功</param>
        public async UniTask PrewarmAsync(string key, int initCount, int maxCapacity,
            UnityAction<bool> onComplete = null)
        {
            if (!CheckPrewarmValid(key, initCount, maxCapacity))
            {
                onComplete?.Invoke(false);
                return;
            }

            var data = GetOrCreatePrewarmPool(key, maxCapacity);
            int needed = initCount - data.Count;
            if (needed <= 0)
            {
                onComplete?.Invoke(true);
                return;
            }

            var prefab = await gameObjectResLoader.LoadAsync<GameObject>(key);
            if (prefab == null)
            {
                WSLog.LogWarning($"PrewarmAsync: no prefab found for key '{key}'.");
                onComplete?.Invoke(false);
                return;
            }

            PrewarmObjects(data, key, prefab, needed, false);

            onComplete?.Invoke(true);
        }


        /// <summary>
        /// 该方法提供了一个简化的接口，允许调用方直接通过类型参数来获取对象实例，内部会使用类型名称作为 key 来管理池子，适用于每个类型对应一个预制体的常见情况。
        /// 要求：<c>必须是资源与对应的类一致名称</c>
        /// </summary>
        public GameObject Get<T>(Transform parent = null) where T : IPoolable
        {
            return Get(typeof(T).Name, parent);
        }

        /// <summary>
        /// 同步加载对象，如果池中没有可用对象，则尝试从项目资源系统加载预制体并实例化，返回实例对象。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public GameObject Get(string key, Transform parent = null)
        {
            if (!CheckKeyAndResLoadValid(key)) return null;

            if (!PoolDic.TryGetValue(key, out var data))
            {
                // 自动创建一个无限容量的池
                WSLog.Log("创建新的对象池: " + key + ", 默认无限容量，如果需要容量限制请预先调用 Prewarm 方法设置容量，同时建议预热池子以避免后续获取时的性能问题");
                data = new GameObjectPoolData(poolRootTransform, -1, $"Pool_{key}");
                PoolDic[key] = data;
            }

            if (data.TryGet(out var go, parent))
            {
                // 复用池中对象，设置 parent 并激活
                PrepareForGet(go);
                return go;
            }

            // 如果池中没有对象了，尝试加载预制体并实例化返回
            var prefab = gameObjectResLoader.Load<GameObject>(key);
            if (prefab == null)
            {
                WSLog.LogWarning($"Get: no prefab found for key '{key}' and pool is empty.");
                return null;
            }

            var inst = GameObject.Instantiate(prefab, parent, false);
            MarkObjectIdentity(inst, key);
            PrepareForGet(inst);
            inst.name = prefab.name;
            return inst;
        }

        public List<GameObject> GetSome(string key, int count, Transform parent = null)
        {
            if (!CheckKeyAndResLoadValid(key)) return null;

            if (!PoolDic.TryGetValue(key, out var data))
            {
                WSLog.Log("创建新的对象池: " + key + ", 默认无限容量，如果需要容量限制请预先调用 Prewarm 方法设置容量，同时建议预热池子以避免后续获取时的性能问题");
                data = new GameObjectPoolData(poolRootTransform, -1, $"Pool_{key}");
                PoolDic[key] = data;
            }

            if (data.TryGetSome(count, out var gos, parent))
            {
                PrepareForGet(gos);
                return gos;
            }

            var prefab = gameObjectResLoader.Load<GameObject>(key);
            if (prefab == null)
            {
                WSLog.LogWarning($"Get(count): no prefab found for key '{key}' and pool is empty.");
                return null;
            }

            var instList = new List<GameObject>(count);
            for (int i = 0; i < count; i++)
            {
                var inst = GameObject.Instantiate(prefab, parent, false);
                MarkObjectIdentity(inst, key);
                PrepareForGet(inst);
                inst.name = prefab.name;
                instList.Add(inst);
            }

            return instList;
        }
        
        /// <summary>
        /// 返回 UniTask&lt;GameObject&gt;版本的 Get 方法，适用于需要从远程或异步资源系统加载预制体的情况，避免在主线程等待资源加载完成。
        /// </summary>
        /// <param name="parent"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async UniTask<GameObject> GetAsync<T>(Transform parent = null)
        {
            return await GetAsync(typeof(T).Name, parent);
        }

        public async UniTask<GameObject> GetAsync(string key, Transform parent = null)
        {
            if (!CheckKeyAndResLoadValid(key))
            {
                return null;
            }

            if (!PoolDic.TryGetValue(key, out var data))
            {
                WSLog.Log("创建新的对象池: " + key + ", 默认无限容量，如果需要容量限制请预先调用 Prewarm 方法设置容量，同时建议预热池子以避免后续获取时的性能问题");
                data = new GameObjectPoolData(poolRootTransform, -1, $"Pool_{key}");
                PoolDic[key] = data;
            }

            if (data.TryGet(out var go, parent))
            {
                PrepareForGet(go);
                return go;
            }

            // 使用 IResLoad 的 LoadAsync 方法来加载资源，加载完成后实例化并返回
            var prefab = await gameObjectResLoader.LoadAsync<GameObject>(key);

            if (prefab is null)
            {
                WSLog.LogWarning($"GetAsync: no prefab found for key '{key}' and pool is empty.");
                return null;
            }

            var inst = GameObject.Instantiate(prefab, parent, false);
            MarkObjectIdentity(inst, key);
            PrepareForGet(inst);
            inst.name = prefab.name;
            return inst;
        }

        // 回调式的异步获取：立即返回，通过回调在资源加载完成时返回实例，避免调用方在主线程等待
        public void GetAsync<T>(Transform parent, UnityAction<GameObject> onComplete)
        {
            GetAsync(typeof(T).Name, parent, onComplete);
        }

        public void GetAsync(string key, Transform parent, UnityAction<GameObject> onComplete)
        {
            if (!CheckKeyAndResLoadValid(key))
            {
                onComplete?.Invoke(null);
                return;
            }

            if (!PoolDic.TryGetValue(key, out var data))
            {
                WSLog.Log("创建新的对象池: " + key + ", 默认无限容量，如果需要容量限制请预先调用 Prewarm 方法设置容量，同时建议预热池子以避免后续获取时的性能问题");
                data = new GameObjectPoolData(poolRootTransform, -1, $"Pool_{key}");
                PoolDic[key] = data;
            }

            if (data.TryGet(out var go, parent))
            {
                PrepareForGet(go);
                onComplete?.Invoke(go);
                return;
            }

            // 使用 IResLoad 的 LoadAsync 回调接口来加载资源，加载完成后在回调中实例化并返回
            gameObjectResLoader.LoadAsync<GameObject>(key, prefab =>
            {
                if (prefab == null)
                {
                    WSLog.LogWarning($"GetAsync(callback): no prefab found for key '{key}' and pool is empty.");
                    onComplete?.Invoke(null);
                    return;
                }

                var inst = GameObject.Instantiate(prefab, parent, false);
                MarkObjectIdentity(inst, key);
                PrepareForGet(inst);
                inst.name = prefab.name;
                onComplete?.Invoke(inst);
            });
        }

        /// <summary>
        /// 回收对象到对应的池中，如果池不存在则会创建一个新的池，如果池不存在就直接丢弃对象，适用于需要手动指定回收对象所属池的情况。
        /// </summary>
        public void Recycle(string key, GameObject go)
        {
            if (string.IsNullOrEmpty(key) || go == null) return;

            if (!PoolDic.TryGetValue(key, out var data))
            {
                GameObject.Destroy(go);
                return;
            }

            PrepareForRecycle(go, key);
            data.PushObj(go);
        }

        /// <summary>
        /// 该方法只能用于 GameObject 的名字与池子的 key 一致的情况，方便调用方直接传入对象实例进行回收，而不需要额外传入 key 参数。
        /// </summary>
        /// <param name="go"></param>
        public void Recycle(GameObject go)
        {
            if (go == null) return;
            var key = go.TryGetComponent<PoolObjectIdentity>(out var identity)
                ? identity.PoolKey
                :
                // 去除可能存在的 (Clone) 后缀，确保能正确找到对应的池
                go.name.Replace("(Clone)", "");

            Recycle(key, go);
        }

        public void RecycleSome(List<GameObject> gos)
        {
            if (gos is not { Count: > 0 }) return;

            string key;
            if (gos[0].TryGetComponent<PoolObjectIdentity>(out var identity))
            {
                key = identity.PoolKey;
            }
            else
            {
                // 去除可能存在的 (Clone) 后缀
                key = gos[0].name.Replace("(Clone)", "");
            }

            if (!PoolDic.TryGetValue(key, out var data))
            {
                foreach (var go in gos)
                {
                    GameObject.Destroy(go);
                }

                return;
            }
            
            PrepareForRecycle(gos, key);
            data.PushObjs(gos);
        }

        public void ClearPool(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (PoolDic.TryGetValue(key, out var data))
            {
                data.ClearPool();
                PoolDic.Remove(key);
            }
        }

        public void ClearAll()
        {
            foreach (var p in PoolDic.Values)
            {
                p.ClearPool();
            }

            PoolDic.Clear();
        }

        private GameObjectPoolData GetOrCreatePrewarmPool(string key, int maxCapacity)
        {
            if (!PoolDic.TryGetValue(key, out var poolData))
            {
                poolData = new GameObjectPoolData(poolRootTransform, maxCapacity, $"Pool_{key}");
                PoolDic[key] = poolData;
                return poolData;
            }

            poolData.EnsureMaxCapacity(maxCapacity);
            return poolData;
        }

        private void PrewarmObjects(
            GameObjectPoolData poolData,
            string key,
            GameObject prefab,
            int count,
            bool usePrefabAsFirst)
        {
            if (poolData == null || prefab == null || count <= 0) return;

            int startIndex = 0;
            if (usePrefabAsFirst)
            {
                MarkObjectIdentity(prefab, key);
                PrepareForRecycle(prefab);
                poolData.PushObj(prefab);
                startIndex = 1;
            }

            for (int i = startIndex; i < count; i++)
            {
                var inst = GameObject.Instantiate(prefab, poolRootTransform, false);
                inst.name = prefab.name;
                MarkObjectIdentity(inst, key);
                PrepareForRecycle(inst);
                poolData.PushObj(inst);
            }
        }

        #region 该类的合理性检验
        private bool CheckPrewarmValid(string key, int initCount, int maxCapacity)
        {
            if (!CheckKeyAndResLoadValid(key)) return false;

            if (initCount <= 0 || (initCount > maxCapacity && maxCapacity != -1))
            {
                WSLog.LogError(
                    $"InitCount is inValid: {initCount} or Prewarm: initCount {initCount} exceeds maxCapacity {maxCapacity} for key '{key}'.");
                return false;
            }

            return true;
        }

        private bool CheckKeyAndResLoadValid(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                WSLog.LogError($"Prewarm: invalid parameters for key '{key}'.");
                return false;
            }

            if (gameObjectResLoader == null)
            {
                WSLog.LogError($"Prewarm: gameObjectResLoader is null.");
                return false;
            }

            return true;
        }
        #endregion

        #region 辅助函数
        private void MarkObjectIdentity(GameObject go, string key)
        {
            if (go == null) return;
            if (!go.TryGetComponent<PoolObjectIdentity>(out var identity))
            {
                identity = go.AddComponent<PoolObjectIdentity>();
            }

            identity.PoolKey = key;
        }

        // 将对象准备为可用状态：激活、重置 transform、parent 到指定节点
        private void PrepareForGet(GameObject go)
        {
            if (go == null) return;

            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
        }
        
        private void PrepareForGet(List<GameObject> gos)
        {
            if (gos is not { Count: > 0 }) return;
            foreach (var go in gos)
            {
                PrepareForGet(go);
            }
        }
        
        private void PrepareForGet(GameObject[] gos)
        {
            if (gos is not { Length: > 0 }) return;
            foreach (var go in gos)
            {
                PrepareForGet(go);
            }
        }
        
        /// <summary>
        /// 将对象准备为可回收状态：停用、重置 transform、parent 到 poolRoot 
        /// </summary>
        /// <param name="go">将要回收的对象</param>
        /// <param name="key">如果填入内容，则会添加 ObjectIdentity 对象，标记属于哪个池子</param>
        private void PrepareForRecycle(GameObject go, string key = null)
        {
            if (go == null) return;
            // 可根据需要在这里清除组件状态（如停止协程、重置动画、关闭特效等）

            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            
            if (key is not null) MarkObjectIdentity(go, key);
        }

        private void PrepareForRecycle(List<GameObject> gos, string key = null)
        {
            if (gos is not { Count: > 0 }) return;
            foreach (var go in gos)
            {
                PrepareForRecycle(go, key);
            }
        }

        private void PrepareForRecycle(GameObject[] gos, string key = null)
        {
            if (gos is not { Length: > 0 }) return;
            foreach (var go in gos)
            {
                PrepareForRecycle(go, key);
            }
        }
        #endregion
    }
}
