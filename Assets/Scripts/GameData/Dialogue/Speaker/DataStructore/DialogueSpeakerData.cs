using System;
using System.Collections.Generic;
using UnityEngine;
using WS_Modules;

namespace GameData
{
    /// <summary>
    /// 对话说话人配置数据，保存身份信息以及该角色头像图集中的可用头像 Id。
    /// </summary>
    [Serializable]
    public sealed class DialogueSpeakerData
    {
        #region 字段
        public string speakerId;
        public string speakerName;
        public Color nameColor = Color.white;
        [WSAddressableKey("Portrait")]
        public string portraitAtlasAddress;
        public string defaultPortraitId;
        public List<string> portraitIds = new List<string>();
        #endregion
    }
}
