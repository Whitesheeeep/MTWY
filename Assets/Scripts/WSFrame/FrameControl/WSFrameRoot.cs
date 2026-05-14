using Sirenix.OdinInspector;
using System.Reflection;
using UnityEngine;
using WS_Modules.LogModule;
using WS_Modules.Pooling;
using WS_Modules.ResLoadModule;
using WS_Modules.Singleton;
using WS_Modules.AudioSystem;
using WS_Modules.UIModule;

namespace WS_Modules
{
    /// <summary>
    /// 框架根类，用于初始化框架的核心组件和设置，例如日志系统、资源管理器、事件中心等
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)] // 确保这个组件在其他组件之前执行
    public class WSFrameRoot : SingletonMonoBase<WSFrameRoot>
    {
        [SerializeField]
#if UNITY_EDITOR
        [OnInspectorInit(nameof(RegisterEditorInstance))]
#endif
        private WSFrameSetting frameSetting;
        public WSFrameSetting FrameSetting => frameSetting;

        #region 音频系统管理
        [BoxGroup("AudioSystem"), LabelText("全局音量"), PropertyRange(0, 1)]
        [ShowInInspector]
        public float GlobalVolume
        {
            get => AudioManager.Instance.GlobalVolume;
            set => AudioManager.Instance.GlobalVolume = value;
        }

        [BoxGroup("AudioSystem"), LabelText("背景音量"), PropertyRange(0, 1)]
        [ShowInInspector]
        public float BGVolume
        {
            get => AudioManager.Instance.BGVolume;
            set => AudioManager.Instance.BGVolume = value;
        }

        [BoxGroup("AudioSystem"), LabelText("特效音量"), PropertyRange(0, 1)]
        [ShowInInspector]
        public float EffectVolume
        {
            get => AudioManager.Instance.EffectVolume;
            set => AudioManager.Instance.EffectVolume = value;
        }

        [BoxGroup("AudioSystem"), LabelText("是否静音")]
        [ShowInInspector]
        public bool IsMute
        {
            get => AudioManager.Instance.IsMute;
            set => AudioManager.Instance.IsMute = value;
        }

        [BoxGroup("AudioSystem"), LabelText("是否循环(仅背景音乐)")]
        [ShowInInspector]
        public bool IsLoop
        {
            get => AudioManager.Instance.IsLoop;
            set => AudioManager.Instance.IsLoop = value;
        }

        [BoxGroup("AudioSystem"), LabelText("是否暂停")]
        [ShowInInspector]
        public bool IsPause
        {
            get => AudioManager.Instance.IsPause;
            set => AudioManager.Instance.IsPause = value;
        }
        #endregion

        private IResLoad<string> _resLoader;

        protected override void Awake()
        {
            base.Awake();

            InitWSFrameRoot();
        }

#if UNITY_EDITOR
        private void RegisterEditorInstance()
        {
            if (Application.isPlaying)
            {
                return;
            }

            var instanceField = typeof(SingletonMonoBase<WSFrameRoot>).GetField(
                "_instance",
                BindingFlags.Static | BindingFlags.NonPublic);

            if (instanceField == null)
            {
                return;
            }

            var current = instanceField.GetValue(null) as WSFrameRoot;
            if (current == null || current == this)
            {
                instanceField.SetValue(null, this);
            }
        }
#endif

        private void InitWSFrameRoot()
        {
            GetResLoader();

            WSLog.Init(frameSetting.logSetting);
            ResSystem.Instance.Initialize(_resLoader);
            PoolManager.Instance.Initialize(frameSetting.PoolingSettings, _resLoader);
            AudioManager.Instance.Initialize(frameSetting.audioSystemSetting, this.transform, _resLoader);
            UIManager.Instance.Initialize(frameSetting.uiManagerSetting);
        }

        private void GetResLoader()
        {
            switch (frameSetting.resLoadType)
            {
                case E_ResLoadType.Resources:
                    _resLoader = new ResourcesLoadMgrModule();
                    break;
                case E_ResLoadType.Addressable:
                    _resLoader = new AddressablesLoadMgrModule();
                    break;
            }
        }
    }
}
