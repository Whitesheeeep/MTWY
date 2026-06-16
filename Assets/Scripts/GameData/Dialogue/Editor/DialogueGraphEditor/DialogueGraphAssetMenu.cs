using UnityEditor;

namespace GameData.Editor
{
    /// <summary>
    /// DialogueGraph 资源在 Project 视图中的快捷菜单。
    /// </summary>
    internal static class DialogueGraphAssetMenu
    {
        #region 常量
        private const string DuplicateMenuPath = "Assets/GameData/Dialogue/Duplicate Dialogue Graph";
        #endregion

        #region 菜单命令
        /// <summary>
        /// 复制当前选中的 DialogueGraph 资源。
        /// </summary>
        [MenuItem(DuplicateMenuPath, false, 1200)]
        private static void DuplicateSelectedDialogueGraph()
        {
            if (Selection.activeObject is not DialogueGraph_SO source)
            {
                return;
            }

            DialogueGraphEditorGraphCommands commands = new();
            DialogueGraph_SO duplicate = commands.DuplicateGraph(source);
            if (duplicate == null)
            {
                return;
            }

            Selection.activeObject = duplicate;
            EditorGUIUtility.PingObject(duplicate);
        }

        /// <summary>
        /// 仅在选中 DialogueGraph 资源时启用复制菜单。
        /// </summary>
        [MenuItem(DuplicateMenuPath, true)]
        private static bool ValidateDuplicateSelectedDialogueGraph()
        {
            return Selection.activeObject is DialogueGraph_SO;
        }
        #endregion
    }
}
