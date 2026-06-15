/*---------------------------------
 *Title:UI自动化组件生成代码生成工具
 *Date:2026/6/14 14:32:42
 *Description:变量需要以[Text]括号加组件类型的格式进行声明，然后右键窗口物体—— 一键生成UI数据组件脚本即可
 *注意:以下文件是自动生成的，再次生成后会以代码追加的形式新增,若手动修改后,尽量避免自动生成
---------------------------------*/
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 对话选项 UI 槽位，负责显示选项文本并把按钮点击回传给窗口。
    /// </summary>
    public class Choice : MonoBehaviour
    {
        #region 自定义字段
        public Button ChoiceButtonButton;

        public TMP_Text ChoiceContentTMP_Text;
        #endregion

        #region 字段
        private int choiceIndex = -1;
        #endregion

        #region 生命周期
        /// <summary>
        /// 初始化按钮监听状态，确保外部注册前没有旧监听残留。
        /// </summary>
        public void OnInitialize()
        {
            ChoiceButtonButton?.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// 设置当前选项槽位显示的数据。
        /// </summary>
        /// <param name="index">当前选项在 ViewData 中的索引。</param>
        /// <param name="choiceText">当前选项显示文本。</param>
        public void SetItemData(int index, string choiceText, bool isInteractable, string disabledReason)
        {
            choiceIndex = index;

            if (ChoiceContentTMP_Text != null)
            {
                ChoiceContentTMP_Text.text = BuildChoiceText(choiceText, isInteractable, disabledReason);
            }

            if (ChoiceButtonButton != null)
            {
                ChoiceButtonButton.interactable = isInteractable;
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// 注册选项按钮点击回调。重复注册时会先清理旧监听。
        /// </summary>
        /// <param name="callback">点击选项时触发的回调，参数为当前选项索引。</param>
        public void RegisterButtonClicked(Action<int> callback)
        {
            if (ChoiceButtonButton == null)
            {
                return;
            }

            ChoiceButtonButton.onClick.RemoveAllListeners();
            ChoiceButtonButton.onClick.AddListener(() => callback?.Invoke(choiceIndex));
        }

        /// <summary>
        /// 清空当前槽位数据并隐藏自身。
        /// </summary>
        public void ClearItemData()
        {
            choiceIndex = -1;

            if (ChoiceContentTMP_Text != null)
            {
                ChoiceContentTMP_Text.text = string.Empty;
            }

            if (ChoiceButtonButton != null)
            {
                ChoiceButtonButton.interactable = true;
                ChoiceButtonButton.onClick.RemoveAllListeners();
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// 释放按钮监听和显示数据。
        /// </summary>
        public void OnDispose()
        {
            ClearItemData();
        }

        private static string BuildChoiceText(string choiceText, bool isInteractable, string disabledReason)
        {
            string text = choiceText ?? string.Empty;
            if (isInteractable || string.IsNullOrWhiteSpace(disabledReason))
            {
                return text;
            }

            return $"{text}\n{disabledReason}";
        }
        #endregion
    }
}
