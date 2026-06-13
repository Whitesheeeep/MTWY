/*---------------------------------
 *Title:UI自动化组件生成代码生成工具
 *Date:2026/5/30 17:05:56
 *Description:变量需要以[Text]括号加组件类型的格式进行声明，然后右键窗口物体—— 一键生成UI数据组件脚本即可
 *注意:以下文件是自动生成的，再次生成后会以代码追加的形式新增,若手动修改后,尽量避免自动生成
---------------------------------*/
using Gameplay.TimeSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WS_Modules.CustomEventSystem;

namespace WS_Modules.UIModule
{
    public class TimeUIView : MonoBehaviour
    {
        #region 自定义字段
        public Image TimeImageUIImage;

        public Image[] TimeDurationImagesImageArray;

        public TMP_Text TimeTMP_Text;

        public TMP_Text DateTMP_Text;
        #endregion

        #region 事件订阅
        private GameTimeManager gameTimeManager;
        private IUnRegister currentTimeUnRegister;
        private IUnRegister minuteChangedUnRegister;
        #endregion

        #region 生命周期
        //脚本初始化接口 (为保证生命周期的执行顺序，请在View层调用该接口确保需要初始化的数据正常执行)
        public void OnInitialize()
        {
            //按钮事件自动注册绑定
        }

        //物体设置数据接口 (请自定以你的参数，方便外部调用传参)
        public void SetItemData()
        {
        }

        //物体销毁时执行 (为保证生命周期的执行顺序，请在View层调用该接口确保需要释放时的接口正常调用)
        public void OnDispose()
        {
            Unbind();
        }

        private void OnDestroy()
        {
            Unbind();
        }
        #endregion

        #region Binding
        public void Bind(GameTimeManager manager)
        {
            Unbind();

            if (manager == null)
            {
                return;
            }

            gameTimeManager = manager;
            currentTimeUnRegister = gameTimeManager.CurrentTime.RegisterWithInitValue(Refresh);
            minuteChangedUnRegister = gameTimeManager.RegisterMinuteChanged(OnMinuteChanged);
        }

        public void Unbind()
        {
            currentTimeUnRegister?.UnRegister();
            currentTimeUnRegister = null;
            minuteChangedUnRegister?.UnRegister();
            minuteChangedUnRegister = null;
            gameTimeManager = null;
        }
        #endregion

        #region Refresh
        private void OnMinuteChanged(GameTimeChangedEventArgs eventArgs)
        {
            Refresh(eventArgs.Current);
        }

        private void Refresh(GameTimeData time)
        {
            if (TimeTMP_Text != null)
            {
                TimeTMP_Text.text = $"{time.Hour:D2}:{time.Minute:D2}";
            }

            if (DateTMP_Text != null)
            {
                DateTMP_Text.text = $"{time.Year}年 {time.Month:D2}月 {time.Day:D2}日";
            }

            RefreshTimeDuration(time.Hour);
            RefreshTimeImageRotation(time);
        }

        private void RefreshTimeDuration(int hour)
        {
            if (TimeDurationImagesImageArray == null || TimeDurationImagesImageArray.Length == 0)
            {
                return;
            }

            int activeIndex = Mathf.Clamp(hour / 4, 0, TimeDurationImagesImageArray.Length - 1);
            for (int i = 0; i < TimeDurationImagesImageArray.Length; i++)
            {
                Image image = TimeDurationImagesImageArray[i];
                if (image == null)
                {
                    continue;
                }

                image.enabled = i <= activeIndex;
            }
        }

        private void RefreshTimeImageRotation(GameTimeData time)
        {
            if (TimeImageUIImage == null)
            {
                return;
            }

            int minutesOfDay = (time.Hour - 8) * 60 + time.Minute;
            minutesOfDay = (minutesOfDay + 1440) % 1440;
            float progress = minutesOfDay / 1440f;
            float angle = progress * 360f;
            TimeImageUIImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
        #endregion

        #region UI组件事件
        #endregion
    }
}
