using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 标记字符串字段应通过 Speaker 数据表绘制为 SpeakerId 下拉框。
    /// </summary>
    public sealed class DialogueSpeakerIdAttribute : PropertyAttribute
    {
    }

    /// <summary>
    /// 标记字符串字段应根据同一对象上的 SpeakerId 绘制为头像 Id 下拉框。
    /// </summary>
    public sealed class DialogueSpeakerPortraitIdAttribute : PropertyAttribute
    {
    }
}
