using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;
using WS_Modules.ResLoadModule;

namespace GameData
{
    /// <summary>
    /// 对话头像加载器，通过 Speaker 的头像图集和头像 Id 解析 UI 可用的 Sprite。
    /// </summary>
    public sealed class DialoguePortraitLoader
    {
        #region 字段
        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        private readonly HashSet<string> loadedAtlasAddresses = new HashSet<string>();
        #endregion

        #region 加载
        /// <summary>
        /// 根据 Speaker 数据和头像 Id 异步加载头像 Sprite。
        /// </summary>
        /// <param name="speaker">说话人数据。</param>
        /// <param name="portraitId">头像 Id，同时也是图集中的 Sprite 名称。</param>
        /// <returns>加载成功时返回头像 Sprite；失败或未配置时返回 null。</returns>
        public async UniTask<Sprite> LoadPortraitAsync(DialogueSpeakerData speaker, string portraitId)
        {
            if (speaker == null || string.IsNullOrWhiteSpace(speaker.portraitAtlasAddress))
            {
                return null;
            }

            string resolvedPortraitId = ResolvePortraitId(speaker, portraitId);

            string cacheKey = CreateCacheKey(speaker, resolvedPortraitId);
            if (spriteCache.TryGetValue(cacheKey, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            Sprite sprite = await LoadSpriteFromAtlasAsync(speaker.portraitAtlasAddress, resolvedPortraitId);
            spriteCache[cacheKey] = sprite;
            return sprite;
        }
        #endregion

        #region 生命周期
        /// <summary>
        /// 释放当前加载器通过资源系统持有的头像图集引用。
        /// </summary>
        public void ReleaseAll()
        {
            foreach (string address in loadedAtlasAddresses)
            {
                ResSystem.Instance.UnLoad<SpriteAtlas>(address);
            }

            spriteCache.Clear();
            loadedAtlasAddresses.Clear();
        }
        #endregion

        #region 内部加载
        private async UniTask<Sprite> LoadSpriteFromAtlasAsync(string atlasAddress, string spriteName)
        {
            SpriteAtlas atlas = await ResSystem.Instance.LoadAsync<SpriteAtlas>(atlasAddress);
            if (atlas == null)
            {
                Debug.LogWarning($"[DialoguePortraitLoader] Failed to load portrait atlas: {atlasAddress}");
                return null;
            }

            loadedAtlasAddresses.Add(atlasAddress);

            Sprite sprite = atlas.GetSprite(spriteName);
            if (sprite == null)
            {
                Debug.LogWarning($"[DialoguePortraitLoader] Sprite '{spriteName}' not found in atlas: {atlasAddress}");
            }

            return sprite;
        }
        #endregion

        #region 工具方法
        private static string ResolvePortraitId(DialogueSpeakerData speaker, string portraitId)
        {
            if (speaker.portraitIds == null || speaker.portraitIds.Count == 0)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(portraitId) && speaker.portraitIds.Contains(portraitId))
            {
                return portraitId;
            }

            if (!string.IsNullOrWhiteSpace(speaker.defaultPortraitId) && speaker.portraitIds.Contains(speaker.defaultPortraitId))
            {
                return speaker.defaultPortraitId;
            }

            return speaker.portraitIds.Find(item => !string.IsNullOrWhiteSpace(item)) ?? string.Empty;
        }

        private static string CreateCacheKey(DialogueSpeakerData speaker, string portraitId)
        {
            return $"{speaker.portraitAtlasAddress}#{portraitId}";
        }
        #endregion
    }
}
