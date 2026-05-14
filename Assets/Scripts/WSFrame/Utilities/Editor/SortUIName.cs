using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using WS_Modules.LogModule;

namespace WS_Modules.UIModule
{
    public static class SortUIName
    {
        [MenuItem("GameObject/UI自动绑定工具/整理 UI 名称为 []", false, 0)]
        static void SortUINameByComponent()
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogWarning("请先选择一个 GameObject");
                return;
            }

            var uiContent = Selection.activeGameObject.transform.Find("UIContent");
            if (uiContent == null)
            {
                Debug.LogWarning("未找到 UIContent 子对象，请确保选中的 GameObject 下有一个名为 UIContent 的子对象");
                return;
            }

            SortChildrenName(uiContent);
        }

        private static void SortChildrenName(Transform uiContent)
        {
            foreach (Transform child in uiContent)
            {
                if (child == uiContent) continue; // 跳过 UIContent 本身
                
                Debug.Log("正在处理: " + child.name);
                if (child.name.Contains("[") && child.name.Contains("]"))
                {
                    continue; // 已经包含组件类型标识，跳过
                }
                if (child.GetComponent<Button>() != null)
                {
                    child.name = $"[Button]{child.name}";
                    Debug.Log("重命名为: " + child.name);
                }
                else if (child.GetComponent<InputField>() != null)
                {
                    child.name = $"[InputField]{child.name}";
                    Debug.Log("重命名为: " + child.name);
                }
                else if (child.GetComponent<Dropdown>() != null)
                {
                    child.name = $"[Dropdown]{child.name}";
                    Debug.Log("重命名为: " + child.name);
                }
                else if (child.GetComponent<Toggle>() != null)
                {
                    child.name = $"[Toggle]{child.name}";
                    Debug.Log("重命名为: " + child.name);
                }
                else if (child.GetComponent<Slider>() != null)
                {
                    child.name = $"[Slider]{child.name}";
                    Debug.Log("重命名为: " + child.name);
                }
                else if (child.GetComponent<ScrollRect>() != null)
                {
                    child.name = $"[ScrollRect]{child.name}";
                    Debug.Log("重命名为: " + child.name);
                }
                else if (child.GetComponent<Image>() != null)
                {
                    child.name = $"[Image]{child.name}";
                    Debug.Log("重命名为: " + child.name);
                }
                else if (child.GetComponent<Text>() != null)
                {
                    child.name = $"[Text]{child.name}";
                    Debug.Log("重命名为: " + child.name);
                }
                Debug.Log("完成处理: " + child.name);
            }
        }
    }
}