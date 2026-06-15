using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;
using WS_Modules;
using WS_Modules.ResLoadModule;

namespace GameData
{
    /// <summary>
    /// 场景侧对话配置组件，负责加载对话图并把启动配置交给 DialogueManager。
    /// </summary>
    public sealed class DialogueConfig : MonoBehaviour
    {
        #region 字段
        [SerializeField, WSAddressableKey]
        private string dialogueGraphAddress;
        [SerializeField]
        private DialogueStartOptions startOptions = new DialogueStartOptions();
        [SerializeField]
        private List<DialogueServiceFactory> serviceFactories = new List<DialogueServiceFactory>();

        private bool isLoading;
        private string loadingGraphAddress;
        private DialogueGraph_SO loadedGraph;
        private readonly HashSet<string> prewarmedPortraitAtlasAddresses = new HashSet<string>();
        #endregion

        #region 属性
        /// <summary>
        /// 当前配置的对话图 Addressables Key。
        /// </summary>
        public string DialogueGraphAddress => dialogueGraphAddress;

        /// <summary>
        /// 当前配置的对话启动选项。
        /// </summary>
        public DialogueStartOptions StartOptions => startOptions;

        /// <summary>
        /// 当前是否正在加载对话图。
        /// </summary>
        public bool IsLoading => isLoading;

        /// <summary>
        /// 当前配置缓存的对话图资源。
        /// </summary>
        public DialogueGraph_SO LoadedGraph => loadedGraph;
        #endregion

        #region Unity 生命周期
        private void OnDestroy()
        {
            UnloadLoadedGraph();
        }
        #endregion

        #region 对话启动
        /// <summary>
        /// 使用当前配置加载对话图，并在加载完成后交给 DialogueManager 启动。
        /// </summary>
        public void StartDialogue()
        {
            if (string.IsNullOrWhiteSpace(dialogueGraphAddress))
            {
                Debug.LogWarning("[DialogueConfig] Dialogue graph address is empty.");
                return;
            }

            if (isLoading)
            {
                Debug.LogWarning($"[DialogueConfig] Dialogue graph is already loading: {loadingGraphAddress}");
                return;
            }

            if (loadedGraph != null)
            {
                isLoading = true;
                loadingGraphAddress = dialogueGraphAddress;
                StartLoadedGraphAfterPrewarmAsync(loadedGraph).Forget();
                return;
            }

            isLoading = true;
            loadingGraphAddress = dialogueGraphAddress;
            ResSystem.Instance.LoadAsync<DialogueGraph_SO>(dialogueGraphAddress, OnGraphLoaded);
        }

        /// <summary>
        /// 创建本次对话运行时服务表。
        /// </summary>
        /// <returns>安装完所有服务工厂后的服务表。</returns>
        private DialogueServices CreateServices()
        {
            DialogueServices services = new DialogueServices();
            if (serviceFactories == null)
            {
                return services;
            }

            for (int i = 0; i < serviceFactories.Count; i++)
            {
                DialogueServiceFactory factory = serviceFactories[i];
                if (factory == null)
                {
                    Debug.LogWarning($"[DialogueConfig] Service factory at index {i} is null.");
                    continue;
                }

                factory.Install(services);
            }

            return services;
        }

        /// <summary>
        /// 释放当前配置加载过的对话图资源。
        /// </summary>
        private void UnloadLoadedGraph()
        {
            if (string.IsNullOrWhiteSpace(dialogueGraphAddress))
            {
                loadedGraph = null;
                UnloadPrewarmedPortraitAtlases();
                return;
            }

            ResSystem.Instance.UnLoad<DialogueGraph_SO>(dialogueGraphAddress);
            loadedGraph = null;
            UnloadPrewarmedPortraitAtlases();
        }
        #endregion

        #region 回调
        private void OnGraphLoaded(DialogueGraph_SO graph)
        {
            string completedAddress = loadingGraphAddress;

            if (graph == null)
            {
                isLoading = false;
                loadingGraphAddress = null;
                Debug.LogWarning($"[DialogueConfig] Failed to load dialogue graph: {completedAddress}");
                return;
            }

            loadedGraph = graph;
            StartLoadedGraphAfterPrewarmAsync(graph).Forget();
        }
        #endregion

        #region 内部工具
        private async UniTaskVoid StartLoadedGraphAfterPrewarmAsync(DialogueGraph_SO graph)
        {
            try
            {
                await PrewarmFirstPortraitAtlasAsync(graph);
            }
            finally
            {
                isLoading = false;
                loadingGraphAddress = null;
            }

            StartLoadedGraph(graph);
        }

        private async UniTask PrewarmFirstPortraitAtlasAsync(DialogueGraph_SO graph)
        {
            if (graph?.StartNode?.NextNode is not DialogueSpeechNode firstSpeech)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(firstSpeech.SpeakerId))
            {
                Debug.LogWarning("[DialogueConfig] First speech has empty speakerId. Portrait atlas prewarm skipped.");
                return;
            }

            if (!GameDatabase.TryGet(out IDialogueSpeakerDatabase speakerDatabase))
            {
                Debug.LogWarning("[DialogueConfig] IDialogueSpeakerDatabase is not registered. Portrait atlas prewarm skipped.");
                return;
            }

            if (!speakerDatabase.TryGet(firstSpeech.SpeakerId, out DialogueSpeakerData speaker))
            {
                Debug.LogWarning($"[DialogueConfig] Speaker '{firstSpeech.SpeakerId}' not found. Portrait atlas prewarm skipped.");
                return;
            }

            string atlasAddress = speaker.portraitAtlasAddress;
            if (string.IsNullOrWhiteSpace(atlasAddress) || prewarmedPortraitAtlasAddresses.Contains(atlasAddress))
            {
                return;
            }

            SpriteAtlas atlas = await ResSystem.Instance.LoadAsync<SpriteAtlas>(atlasAddress);
            if (atlas == null)
            {
                Debug.LogWarning($"[DialogueConfig] Failed to prewarm portrait atlas: {atlasAddress}");
                return;
            }

            prewarmedPortraitAtlasAddresses.Add(atlasAddress);
        }

        private void StartLoadedGraph(DialogueGraph_SO graph)
        {
            DialogueServices services = CreateServices();
            DialogueManager.Instance.StartDialogue(graph, services, startOptions);
        }

        private void UnloadPrewarmedPortraitAtlases()
        {
            foreach (string atlasAddress in prewarmedPortraitAtlasAddresses)
            {
                ResSystem.Instance.UnLoad<SpriteAtlas>(atlasAddress);
            }

            prewarmedPortraitAtlasAddresses.Clear();
        }
        #endregion
    }
}
