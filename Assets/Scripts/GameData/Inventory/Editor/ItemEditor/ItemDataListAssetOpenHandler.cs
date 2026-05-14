using UnityEditor;
using UnityEditor.Callbacks;

namespace GameData.Editor
{
    internal static class ItemDataListAssetOpenHandler
    {
        [OnOpenAsset]
        private static bool OnOpenAsset(int instanceId, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is not ItemDataList_SO dataList)
            {
                return false;
            }

            // 双击 ItemDataList_SO 资产时，打开专用编辑窗口并绑定当前 SO。
            ItemEditorWindow.Open(dataList);
            return true;
        }
    }
}
