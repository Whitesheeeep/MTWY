#if UNITY_EDITOR
using Sirenix.OdinInspector;
using UnityEngine;
using WS_Modules.Utilities;

public class TimerTest : MonoBehaviour
{
    private TimerHandle _basicTimer;
    private TimerHandle _loopTimer;
    private TimerHandle _progressTimer;
    private TimerHandle _unscaledTimer;
    private TimerHandle _scaledTimer;
    private TimerHandle _tagTimerA;
    private TimerHandle _tagTimerB;

    [ShowInInspector, ReadOnly, BoxGroup("Handle State")]
    private bool BasicValid => _basicTimer.IsValid;

    [ShowInInspector, ReadOnly, BoxGroup("Handle State")]
    private bool LoopValid => _loopTimer.IsValid;

    [ShowInInspector, ReadOnly, BoxGroup("Handle State")]
    private bool ProgressValid => _progressTimer.IsValid;

    [ShowInInspector, ReadOnly, BoxGroup("Handle State")]
    private float Progress => _progressTimer.Progress;

    [ShowInInspector, ReadOnly, BoxGroup("Handle State")]
    private float ProgressRemaining => _progressTimer.TimeRemaining;

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    [Button("Run All Examples", ButtonSizes.Large)]
    [BoxGroup("Create")]
    private void RunAllExamples()
    {
        TimerManager.CancelAll();
        Time.timeScale = 1f;

        CreateBasicTimer();
        CreateLoopTimer();
        CreateProgressTimer();
        CreateUnscaledTimer();
        CreateScaledTimer();
        CreateTagTimers();

        Debug.Log("[TimerTest] All examples created.");
    }

    [Button("Basic Timer", ButtonSizes.Medium)]
    [BoxGroup("Create")]
    private void CreateBasicTimer()
    {
        _basicTimer = TimerManager.Register(2f, () =>
        {
            Debug.Log("[Basic] 2 seconds completed.");
        });
    }

    [Button("Loop Timer x3", ButtonSizes.Medium)]
    [BoxGroup("Create")]
    private void CreateLoopTimer()
    {
        int tick = 0;
        _loopTimer = TimerManager.Register(1f, () =>
        {
            tick++;
            Debug.Log($"[Loop] Tick {tick}/3");
        }).SetLoop(3);
    }

    [Button("Progress Timer", ButtonSizes.Medium)]
    [BoxGroup("Create")]
    private void CreateProgressTimer()
    {
        _progressTimer = TimerManager.Register(5f, () =>
        {
            Debug.Log("[Progress] Completed.");
        }).OnUpdate(progress =>
        {
            Debug.Log($"[Progress] {progress:P0}");
        });
    }

    [Button("Unscaled Timer", ButtonSizes.Medium)]
    [BoxGroup("Create")]
    private void CreateUnscaledTimer()
    {
        _unscaledTimer = TimerManager.Register(3f, () =>
        {
            Debug.Log("[Unscaled] Completed even when Time.timeScale is 0.");
        }).SetUnscaledTime(true);
    }

    [Button("Scaled Timer", ButtonSizes.Medium)]
    [BoxGroup("Create")]
    private void CreateScaledTimer()
    {
        _scaledTimer = TimerManager.Register(3f, () =>
        {
            Debug.Log("[Scaled] Completed only while Time.timeScale advances.");
        });
    }

    [Button("Create Test Tag Timers", ButtonSizes.Medium)]
    [BoxGroup("Tag")]
    private void CreateTagTimers()
    {
        _tagTimerA = TimerManager.Register(10f, () =>
        {
            Debug.Log("[Tag] Timer A completed.");
        }).SetTag(TimerManager.TimerTags.Test);

        _tagTimerB = TimerManager.Register(10f, () =>
        {
            Debug.Log("[Tag] Timer B completed.");
        }).SetTag(TimerManager.TimerTags.Test);
    }

    [Button("Pause Progress Handle", ButtonSizes.Medium)]
    [BoxGroup("Handle Control")]
    private void PauseProgressTimer()
    {
        _progressTimer.Pause();
        Debug.Log("[Handle] Progress timer paused.");
    }

    [Button("Resume Progress Handle", ButtonSizes.Medium)]
    [BoxGroup("Handle Control")]
    private void ResumeProgressTimer()
    {
        _progressTimer.Resume();
        Debug.Log("[Handle] Progress timer resumed.");
    }

    [Button("Reset Progress Handle", ButtonSizes.Medium)]
    [BoxGroup("Handle Control")]
    private void ResetProgressTimer()
    {
        _progressTimer.ResetTime();
        Debug.Log("[Handle] Progress timer reset.");
    }

    [Button("Cancel Progress Handle", ButtonSizes.Medium)]
    [BoxGroup("Handle Control")]
    private void CancelProgressTimer()
    {
        _progressTimer.Cancel();
        Debug.Log("[Handle] Progress timer cancelled.");
    }

    [Button("Time Scale = 0", ButtonSizes.Medium)]
    [BoxGroup("Unity Time")]
    private void PauseUnityTime()
    {
        Time.timeScale = 0f;
        Debug.Log("[Unity Time] Time.timeScale = 0");
    }

    [Button("Time Scale = 1", ButtonSizes.Medium)]
    [BoxGroup("Unity Time")]
    private void ResumeUnityTime()
    {
        Time.timeScale = 1f;
        Debug.Log("[Unity Time] Time.timeScale = 1");
    }

    [Button("Progress Timer x2 Speed", ButtonSizes.Medium)]
    [BoxGroup("Timer Time Scale")]
    private void ProgressDoubleSpeed()
    {
        _progressTimer.SetTimeScale(2f);
        Debug.Log("[Timer TimeScale] Progress timer speed = 2");
    }

    [Button("Progress Timer 0.5 Speed", ButtonSizes.Medium)]
    [BoxGroup("Timer Time Scale")]
    private void ProgressHalfSpeed()
    {
        _progressTimer.SetTimeScale(0.5f);
        Debug.Log("[Timer TimeScale] Progress timer speed = 0.5");
    }

    [Button("Progress Timer Freeze", ButtonSizes.Medium)]
    [BoxGroup("Timer Time Scale")]
    private void ProgressFreeze()
    {
        _progressTimer.SetTimeScale(0f);
        Debug.Log("[Timer TimeScale] Progress timer speed = 0");
    }

    [Button("Pause Test Tag", ButtonSizes.Medium)]
    [BoxGroup("Tag")]
    private void PauseTestTag()
    {
        TimerManager.PauseByTag(TimerManager.TimerTags.Test);
        Debug.Log("[Tag] Paused Test timers.");
    }

    [Button("Resume Test Tag", ButtonSizes.Medium)]
    [BoxGroup("Tag")]
    private void ResumeTestTag()
    {
        TimerManager.ResumeByTag(TimerManager.TimerTags.Test);
        Debug.Log("[Tag] Resumed Test timers.");
    }

    [Button("Test Tag 0.5 Speed", ButtonSizes.Medium)]
    [BoxGroup("Tag")]
    private void SlowTestTag()
    {
        TimerManager.SetTimeScaleByTag(TimerManager.TimerTags.Test, 0.5f);
        Debug.Log("[Tag] Test timer speed = 0.5");
    }

    [Button("Cancel Test Tag", ButtonSizes.Medium)]
    [BoxGroup("Tag")]
    private void CancelTestTag()
    {
        TimerManager.CancelByTag(TimerManager.TimerTags.Test);
        Debug.Log("[Tag] Cancelled Test timers.");
    }

    [Button("Cancel All Timers", ButtonSizes.Large)]
    [BoxGroup("Cleanup")]
    private void CancelAllTimers()
    {
        TimerManager.CancelAll();
        Time.timeScale = 1f;
        Debug.Log("[Cleanup] All timers cancelled.");
    }
}
#endif
