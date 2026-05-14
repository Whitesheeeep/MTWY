using System;
using UnityEngine;
using UnityEngine.Events;
using WS_Modules.ResLoadModule;
using WS_Modules.Singleton;

namespace WS_Modules.AudioSystem
{
    public class AudioManager : SingletonBase<AudioManager>, IDisposable
    {
        private AudioModule _audioModule;

        // SingletonBase requires private constructor
        private AudioManager()
        {
            _audioModule = new AudioModule();
        }
        
        public void Initialize(WSFrameSetting.AudioSystemSetting audioSystemSetting, Transform root, IResLoad<string> resLoader)
        {
            Initialize(audioSystemSetting.audioSourcePrefabPath, audioSystemSetting.audioSourceInitCount, root, resLoader);
        }
        
        /// <summary>
        /// 初始化音频系统
        /// </summary>
        /// <param name="audioUnitKey">音频单元对象池Key</param>
        /// <param name="root">音频系统根节点</param>
        /// <param name="resLoader">资源加载器</param>
        public void Initialize(string audioUnitKey, int initCount, Transform root, IResLoad<string> resLoader)
        {
            GameObject audioSystem = new  GameObject("AudioSystem");
            audioSystem.transform.SetParent(root);
            _audioModule.Init(audioUnitKey, initCount, audioSystem.transform, resLoader);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _audioModule.Dispose();
        }

        #region Volume Control
        
        public float GlobalVolume
        {
            get => _audioModule.GlobalVolume;
            set => _audioModule.GlobalVolume = value;
        }

        public float BGVolume
        {
            get => _audioModule.BGVolume;
            set => _audioModule.BGVolume = value;
        }

        public float EffectVolume
        {
            get => _audioModule.EffectVolume;
            set => _audioModule.EffectVolume = value;
        }

        public bool IsMute
        {
            get => _audioModule.IsMute;
            set => _audioModule.IsMute = value;
        }
        
        public bool IsLoop
        {
            get => _audioModule.IsLoop;
            set => _audioModule.IsLoop = value;
        }

        public bool IsPause
        {
            get => _audioModule.IsPause;
            set => _audioModule.IsPause = value;
        }
        
        #endregion

        #region BGM Operations

        public void PlayBGM(AudioClip clip, float volume = 1f, bool loop = true)
        {
            _audioModule.PlayBGM(clip, volume, loop);
        }

        public void StopBGM()
        {
            _audioModule.StopBGM();
        }

        public void PauseBGM()
        {
            _audioModule.PauseBGM();
        }

        public void ResumeBGM()
        {
            _audioModule.ResumeBGM();
        }

        #endregion

        #region SFX Operations

        /// <summary>
        /// 播放音效（跟随组件）
        /// </summary>
        public void PlaySFX(string name, Component component, bool is3D = false, float scale = 1f, bool loop = false,
            bool autoRelease = false, UnityAction<AudioSource> onPlay = null,
            UnityAction<AudioSource> onComplete = null)
        {
            _audioModule.PlaySFX(name, component, is3D, scale, loop, autoRelease, onPlay, onComplete);
        }

        /// <summary>
        /// 播放音效（指定位置）
        /// </summary>
        public void PlaySFX(string name, Vector3 position, bool is3D = false, float scale = 1f, bool loop = false,
            bool autoRelease = false, UnityAction<AudioSource> onPlay = null,
            UnityAction<AudioSource> onComplete = null)
        {
            _audioModule.PlaySFX(name, position, is3D, scale, loop, autoRelease, onPlay, onComplete);
        }

        public void StopAllSFX()
        {
            _audioModule.StopAllSFX();
        }

        #endregion
    }
}
