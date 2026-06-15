using UnityEditor;
using UnityEditor.Callbacks;

namespace GameData.Editor
{
    /// <summary>
    /// 处理 Project 窗口中双击对话图资源时打开自定义 Graph 编辑器。
    /// </summary>
    internal static class DialogueGraphAssetOpenHandler
    {
        #region 打开资源
        /// <summary>
        /// Unity 双击资源回调；如果目标是对话图资源，则打开对话图编辑器窗口。
        /// </summary>
        /// <param name="instanceId">被打开资源的实例 ID。</param>
        /// <param name="line">Unity 传入的行号参数，本编辑器不使用。</param>
        /// <returns>资源已由对话图编辑器处理时返回 true，否则返回 false。</returns>
        [OnOpenAsset]
        private static bool OpenDialogueGraph(int instanceId, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is not DialogueGraph_SO graph)
            {
                return false;
            }

            DialogueGraphEditorWindow.Open(graph);
            return true;
        }
        #endregion
    }
}
