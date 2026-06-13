using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using WS_Modules.CustomEventSystem;
using WS_Modules.Json;
using WS_Modules.SceneModule;
using WS_Modules.Singleton;
using WS_Modules.Utilities;
using EventSystem = WS_Modules.CustomEventSystem.EventSystem;

namespace Gameplay.TimeSystem
{
    /// <summary>
    /// 游戏内时间管理器。
    /// 使用 TimerManager 将真实时间转换为游戏分钟，并用独立 TimeWheelScheduler 调度游戏分钟任务。
    /// </summary>
    public sealed class GameTimeManager : SingletonMonoBase<GameTimeManager>
    {
        #region Constants

        // 第一版不使用 ScriptableObject 配置，默认日历参数集中写在代码中，避免 Editor 资产参与运行时存档。
        private const int StartYear = 2020;
        private const int StartMonth = 1;
        private const int StartDay = 1;
        private const int StartHour = 6;
        private const int StartMinute = 0;
        private const float RealSecondsPerGameMinute = 1f;
        private const int DaysPerMonth = 28;
        private const int MonthsPerYear = 12;
        private const int HoursPerDay = 24;
        private const int MinutesPerHour = 60;
        private const string SavePath = "GameTime/current_time";

        /// <summary>
        /// 允许使用的时间倍率。SetTimeScale 会吸附到最近的倍率值。
        /// </summary>
        private static readonly float[] AllowedTimeScales = { 0.5f, 1f, 2f, 5f, 10f };

        #endregion

        #region Fields

        /// <summary>
        /// 是否在启动时尝试读取 JsonMgr 保存的当前时间。关闭时始终使用代码默认时间。
        /// </summary>
        [SerializeField]
        private bool useJsonSavedTime;

        /// <summary>
        /// 当前游戏时间。内部可写，对外只暴露 IReadOnlyBindableProperty。
        /// </summary>
        private readonly BindableProperty<GameTimeData> currentTime = new BindableProperty<GameTimeData>();

        /// <summary>
        /// 游戏时间暂停状态。
        /// </summary>
        private readonly BindableProperty<bool> isPaused = new BindableProperty<bool>();

        /// <summary>
        /// 游戏时间倍率。
        /// </summary>
        private readonly BindableProperty<float> timeScale = new BindableProperty<float>(1f);

        /// <summary>
        /// 驱动每个游戏分钟推进的 TimerManager 句柄。
        /// </summary>
        private TimerHandle gameMinuteTimer;

        /// <summary>
        /// 游戏分钟时间轮，用于作物、机器、刷新等“经过 N 个游戏分钟后触发”的任务。
        /// </summary>
        private TimeWheelScheduler gameMinuteScheduler;

        /// <summary>
        /// 场景加载中暂停重建 Timer，避免 TimerManager.CancelAll 后立即重复创建。
        /// </summary>
        private bool isLoadingScene;

        #endregion

        #region Properties

        /// <summary>
        /// 当前游戏时间。UI 可以 RegisterWithInitValue 立即刷新显示。
        /// </summary>
        public IReadOnlyBindableProperty<GameTimeData> CurrentTime => currentTime;

        /// <summary>
        /// 当前游戏时间是否暂停。
        /// </summary>
        public IReadOnlyBindableProperty<bool> IsPaused => isPaused;

        /// <summary>
        /// 当前游戏时间倍率。
        /// </summary>
        public IReadOnlyBindableProperty<float> TimeScale => timeScale;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            gameMinuteScheduler = CreateGameMinuteScheduler();
            currentTime.Value = LoadInitialTime();
            isPaused.Value = false;
            timeScale.Value = 1f;

            SceneSystem.RegisterLoadStarted(OnSceneLoadStarted)
                .UnRegisterWhenGameObjectDestroyed(gameObject);
            SceneSystem.RegisterLoadSucceeded(OnSceneLoadSucceeded)
                .UnRegisterWhenGameObjectDestroyed(gameObject);
            SceneSystem.RegisterLoadFailed(OnSceneLoadFailed)
                .UnRegisterWhenGameObjectDestroyed(gameObject);
            SceneSystem.RegisterLoadCancelled(OnSceneLoadCancelled)
                .UnRegisterWhenGameObjectDestroyed(gameObject);

            StartGameMinuteTimer();
        }
        #endregion

        #region Public Control API

        /// <summary>
        /// 设置启动时是否从 JsonMgr 读取保存的当前时间。
        /// </summary>
        public void SetUseJsonSavedTime(bool useSavedTime)
        {
            useJsonSavedTime = useSavedTime;
        }

        /// <summary>
        /// 暂停游戏时间推进。暂停后游戏分钟 Timer 和游戏分钟 TimeWheel 都不会继续推进。
        /// </summary>
        public void Pause()
        {
            if (isPaused.Value)
            {
                return;
            }

            isPaused.Value = true;
            if (gameMinuteTimer.IsValid)
            {
                gameMinuteTimer.Pause();
            }
        }

        /// <summary>
        /// 恢复游戏时间推进。
        /// </summary>
        public void Resume()
        {
            if (!isPaused.Value)
            {
                return;
            }

            isPaused.Value = false;
            if (gameMinuteTimer.IsValid)
            {
                gameMinuteTimer.Resume();
            }
            else if (!isLoadingScene)
            {
                StartGameMinuteTimer();
            }
        }

        /// <summary>
        /// 设置游戏时间倍率。输入值会吸附到 AllowedTimeScales 中最近的倍率。
        /// </summary>
        public void SetTimeScale(float scale)
        {
            float resolvedScale = ResolveAllowedTimeScale(scale);
            float previous = timeScale.Value;
            if (Mathf.Approximately(previous, resolvedScale))
            {
                return;
            }

            timeScale.Value = resolvedScale;
            if (gameMinuteTimer.IsValid)
            {
                gameMinuteTimer.SetTimeScale(resolvedScale);
            }

            EventSystem.EventTrigger_Int(
                (int)E_GameTimeEvent.TimeScaleChanged,
                new GameTimeScaleChangedEventArgs(previous, resolvedScale));
        }

        /// <summary>
        /// 直接设置当前游戏时间，不自动补发分钟、小时、天等跨越事件。
        /// </summary>
        public void SetTime(GameTimeData time)
        {
            currentTime.Value = NormalizeTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.TotalMinutes);
        }

        /// <summary>
        /// 直接设置当前游戏时间，并根据输入时间重新计算累计游戏分钟。
        /// </summary>
        public void SetTime(int year, int month, int day, int hour, int minute)
        {
            long totalMinutes = CalculateTotalMinutes(year, month, day, hour, minute);
            currentTime.Value = NormalizeTime(year, month, day, hour, minute, totalMinutes);
        }

        /// <summary>
        /// 手动推进若干游戏分钟。会逐分钟派发时间事件并推进游戏分钟 TimeWheel。
        /// </summary>
        public void AdvanceMinutes(int minutes)
        {
            if (minutes <= 0)
            {
                return;
            }

            for (int i = 0; i < minutes; i++)
            {
                AdvanceOneMinute();
            }
        }

        #endregion

        #region Save Load API

        /// <summary>
        /// 将当前游戏时间保存到 JsonMgr 默认路径下。
        /// </summary>
        public bool SaveCurrentTimeToJson()
        {
            return JsonMgr.Save(ToSaveData(currentTime.Value), SavePath, true);
        }

        /// <summary>
        /// 尝试从 JsonMgr 读取当前时间，并覆盖当前运行时状态。
        /// </summary>
        public bool TryLoadCurrentTimeFromJson()
        {
            if (!TryReadSavedTime(out GameTimeData loadedTime))
            {
                return false;
            }

            currentTime.Value = loadedTime;
            return true;
        }

        #endregion

        #region Event Register API

        /// <summary>
        /// 注册游戏分钟变化事件。
        /// </summary>
        public IUnRegister RegisterMinuteChanged(Action<GameTimeChangedEventArgs> handler)
        {
            return EventSystem.Register_Int((int)E_GameTimeEvent.MinuteChanged, handler);
        }

        /// <summary>
        /// 注册游戏小时变化事件。
        /// </summary>
        public IUnRegister RegisterHourChanged(Action<GameTimeChangedEventArgs> handler)
        {
            return EventSystem.Register_Int((int)E_GameTimeEvent.HourChanged, handler);
        }

        /// <summary>
        /// 注册新一天开始事件。
        /// </summary>
        public IUnRegister RegisterDayStarted(Action<GameTimeChangedEventArgs> handler)
        {
            return EventSystem.Register_Int((int)E_GameTimeEvent.DayStarted, handler);
        }

        /// <summary>
        /// 注册月份变化事件。
        /// </summary>
        public IUnRegister RegisterMonthChanged(Action<GameTimeChangedEventArgs> handler)
        {
            return EventSystem.Register_Int((int)E_GameTimeEvent.MonthChanged, handler);
        }

        /// <summary>
        /// 注册季节变化事件。
        /// </summary>
        public IUnRegister RegisterSeasonChanged(Action<GameTimeChangedEventArgs> handler)
        {
            return EventSystem.Register_Int((int)E_GameTimeEvent.SeasonChanged, handler);
        }

        /// <summary>
        /// 注册年份变化事件。
        /// </summary>
        public IUnRegister RegisterYearChanged(Action<GameTimeChangedEventArgs> handler)
        {
            return EventSystem.Register_Int((int)E_GameTimeEvent.YearChanged, handler);
        }

        /// <summary>
        /// 注册时间倍率变化事件。
        /// </summary>
        public IUnRegister RegisterTimeScaleChanged(Action<GameTimeScaleChangedEventArgs> handler)
        {
            return EventSystem.Register_Int((int)E_GameTimeEvent.TimeScaleChanged, handler);
        }

        #endregion

        #region Schedule API

        /// <summary>
        /// 在指定游戏分钟后执行一次回调。
        /// 适合用于作物生长、机器加工、刷新等待等游戏时间任务。
        /// </summary>
        public TimeWheelHandle ScheduleAfterMinutes(int minutes, Action callback)
        {
            if (minutes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minutes), "Delay minutes must be greater than 0.");
            }

            EnsureGameMinuteScheduler();
            return gameMinuteScheduler.Schedule(minutes, callback);
        }

        /// <summary>
        /// 按游戏分钟间隔重复执行回调。
        /// repeatCount 为 -1 时无限重复。
        /// </summary>
        public TimeWheelHandle ScheduleRepeatMinutes(int intervalMinutes, Action callback, int repeatCount = -1)
        {
            if (intervalMinutes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(intervalMinutes), "Interval minutes must be greater than 0.");
            }

            EnsureGameMinuteScheduler();
            return gameMinuteScheduler.ScheduleRepeat(intervalMinutes, callback, repeatCount);
        }

        /// <summary>
        /// 取消通过游戏分钟 TimeWheel 注册的任务。
        /// </summary>
        public bool CancelScheduledTask(TimeWheelHandle handle)
        {
            EnsureGameMinuteScheduler();
            return gameMinuteScheduler.Cancel(handle);
        }

        #endregion

        #region Public Cleanup API

        /// <summary>
        /// 清理时间系统运行时状态。会取消分钟 Timer、清空游戏分钟 TimeWheel，并恢复默认时间。
        /// </summary>
        public void Clear()
        {
            StopGameMinuteTimer();
            gameMinuteScheduler?.Clear();
            currentTime.Value = CreateDefaultTime();
            isPaused.Value = false;
            timeScale.Value = 1f;
        }

        private void OnDestroy()
        {
            Clear();
        }

        #endregion

        #region Timer Lifecycle

        /// <summary>
        /// 创建真实时间 Timer。每次 Timer 到期推进 1 个游戏分钟。
        /// </summary>
        private void StartGameMinuteTimer()
        {
            if (gameMinuteTimer.IsValid || isPaused.Value || isLoadingScene)
            {
                return;
            }

            gameMinuteTimer = TimerManager.Register(RealSecondsPerGameMinute, AdvanceOneMinute)
                .SetLoop(-1)
                .SetTimeScale(timeScale.Value);
        }

        /// <summary>
        /// 停止真实时间 Timer。
        /// </summary>
        private void StopGameMinuteTimer()
        {
            if (!gameMinuteTimer.IsValid)
            {
                return;
            }

            gameMinuteTimer.Cancel();
            gameMinuteTimer = default;
        }

        #endregion

        #region Time Progression

        /// <summary>
        /// 推进 1 个游戏分钟，并依次派发分钟、小时、天、月、季节、年份事件。
        /// </summary>
        private void AdvanceOneMinute()
        {
            GameTimeData previous = currentTime.Value;
            GameTimeData next = AddOneMinute(previous);
            currentTime.Value = next;

            TriggerTimeChanged(E_GameTimeEvent.MinuteChanged, previous, next);

            if (previous.Hour != next.Hour)
            {
                TriggerTimeChanged(E_GameTimeEvent.HourChanged, previous, next);
            }

            if (previous.Day != next.Day || previous.Month != next.Month || previous.Year != next.Year)
            {
                TriggerTimeChanged(E_GameTimeEvent.DayStarted, previous, next);
            }

            if (previous.Month != next.Month || previous.Year != next.Year)
            {
                TriggerTimeChanged(E_GameTimeEvent.MonthChanged, previous, next);
            }

            if (previous.Season != next.Season)
            {
                TriggerTimeChanged(E_GameTimeEvent.SeasonChanged, previous, next);
            }

            if (previous.Year != next.Year)
            {
                TriggerTimeChanged(E_GameTimeEvent.YearChanged, previous, next);
            }

            // 游戏分钟调度器的输入单位是“游戏分钟”，所以每推进 1 分钟就传入 1 个单位。
            gameMinuteScheduler?.Tick(1f);
            // Debug.Log(currentTime.Value.ToString());
        }

        /// <summary>
        /// 触发 int enum 时间事件。
        /// </summary>
        private void TriggerTimeChanged(E_GameTimeEvent eventType, GameTimeData previous, GameTimeData next)
        {
            EventSystem.EventTrigger_Int(
                (int)eventType,
                new GameTimeChangedEventArgs(previous, next));
        }

        #endregion

        #region Scene Events

        /// <summary>
        /// SceneSystem 开始加载时，TimerManager 会 CancelAll，因此这里标记加载中并丢弃旧句柄。
        /// </summary>
        private void OnSceneLoadStarted(SceneLoadStartedEventArgs _)
        {
            isLoadingScene = true;
            gameMinuteTimer = default;
        }

        /// <summary>
        /// 场景加载成功后尝试恢复游戏分钟 Timer。
        /// </summary>
        private void OnSceneLoadSucceeded(SceneLoadSucceededEventArgs _)
        {
            RestartAfterSceneLoading();
        }

        /// <summary>
        /// 场景加载失败后也需要恢复游戏分钟 Timer，避免时间系统停住。
        /// </summary>
        private void OnSceneLoadFailed(SceneLoadFailedEventArgs _)
        {
            RestartAfterSceneLoading();
        }

        /// <summary>
        /// 场景加载取消后也需要恢复游戏分钟 Timer。
        /// </summary>
        private void OnSceneLoadCancelled(SceneLoadCancelledEventArgs _)
        {
            RestartAfterSceneLoading();
        }

        /// <summary>
        /// 场景加载结束后的统一恢复入口。
        /// </summary>
        private void RestartAfterSceneLoading()
        {
            isLoadingScene = false;
            if (!isPaused.Value)
            {
                StartGameMinuteTimer();
            }
        }

        #endregion

        #region Save Load Internals

        /// <summary>
        /// 获取启动时使用的时间。开启 Json 读档且读取成功时使用存档，否则使用默认时间。
        /// </summary>
        private GameTimeData LoadInitialTime()
        {
            if (useJsonSavedTime && TryReadSavedTime(out GameTimeData loadedTime))
            {
                return loadedTime;
            }

            return CreateDefaultTime();
        }

        /// <summary>
        /// 从 JsonMgr 读取并校验存档时间。
        /// </summary>
        private bool TryReadSavedTime(out GameTimeData loadedTime)
        {
            loadedTime = default;
            if (!JsonMgr.TryLoad(SavePath, out GameTimeSaveData saveData) || saveData == null)
            {
                return false;
            }

            if (!IsValidDateTime(saveData.year, saveData.month, saveData.day, saveData.hour, saveData.minute))
            {
                Debug.LogWarning("[GameTimeManager] Saved game time is invalid. Falling back to default time.");
                return false;
            }

            long totalMinutes = saveData.totalMinutes >= 0
                ? saveData.totalMinutes
                : CalculateTotalMinutes(saveData.year, saveData.month, saveData.day, saveData.hour, saveData.minute);

            loadedTime = NormalizeTime(saveData.year, saveData.month, saveData.day, saveData.hour, saveData.minute, totalMinutes);
            return true;
        }

        /// <summary>
        /// 将运行时不可变时间快照转换为 Json 可序列化数据。
        /// </summary>
        private static GameTimeSaveData ToSaveData(GameTimeData time)
        {
            return new GameTimeSaveData
            {
                year = time.Year,
                month = time.Month,
                day = time.Day,
                hour = time.Hour,
                minute = time.Minute,
                totalMinutes = time.TotalMinutes,
            };
        }

        #endregion

        #region Time Calculation

        /// <summary>
        /// 创建代码默认时间。
        /// </summary>
        private static GameTimeData CreateDefaultTime()
        {
            long totalMinutes = CalculateTotalMinutes(StartYear, StartMonth, StartDay, StartHour, StartMinute);
            return NormalizeTime(StartYear, StartMonth, StartDay, StartHour, StartMinute, totalMinutes);
        }

        /// <summary>
        /// 在时间快照上增加 1 游戏分钟，并处理小时、日期、月份、年份回绕。
        /// </summary>
        private static GameTimeData AddOneMinute(GameTimeData time)
        {
            int year = time.Year;
            int month = time.Month;
            int day = time.Day;
            int hour = time.Hour;
            int minute = time.Minute + 1;

            if (minute >= MinutesPerHour)
            {
                minute = 0;
                hour++;
            }

            if (hour >= HoursPerDay)
            {
                hour = 0;
                day++;
            }

            if (day > DaysPerMonth)
            {
                day = 1;
                month++;
            }

            if (month > MonthsPerYear)
            {
                month = 1;
                year++;
            }

            return NormalizeTime(year, month, day, hour, minute, time.TotalMinutes + 1);
        }

        /// <summary>
        /// 归一化输入时间并补齐季节信息。
        /// </summary>
        private static GameTimeData NormalizeTime(int year, int month, int day, int hour, int minute, long totalMinutes)
        {
            year = Mathf.Max(1, year);
            month = Mathf.Clamp(month, 1, MonthsPerYear);
            day = Mathf.Clamp(day, 1, DaysPerMonth);
            hour = Mathf.Clamp(hour, 0, HoursPerDay - 1);
            minute = Mathf.Clamp(minute, 0, MinutesPerHour - 1);
            totalMinutes = Math.Max(0, totalMinutes);

            return new GameTimeData(year, month, day, hour, minute, GetSeasonByMonth(month), totalMinutes);
        }

        /// <summary>
        /// 校验年月日时分是否在当前日历规则内。
        /// </summary>
        private static bool IsValidDateTime(int year, int month, int day, int hour, int minute)
        {
            return year >= 1 &&
                   month >= 1 &&
                   month <= MonthsPerYear &&
                   day >= 1 &&
                   day <= DaysPerMonth &&
                   hour >= 0 &&
                   hour < HoursPerDay &&
                   minute >= 0 &&
                   minute < MinutesPerHour;
        }

        /// <summary>
        /// 根据年月日时分计算累计游戏分钟。
        /// </summary>
        private static long CalculateTotalMinutes(int year, int month, int day, int hour, int minute)
        {
            year = Math.Max(1, year);
            month = Math.Max(1, Math.Min(MonthsPerYear, month));
            day = Math.Max(1, Math.Min(DaysPerMonth, day));
            hour = Math.Max(0, Math.Min(HoursPerDay - 1, hour));
            minute = Math.Max(0, Math.Min(MinutesPerHour - 1, minute));

            long completedYears = Math.Max(0, year - 1);
            long completedMonths = Math.Max(0, month - 1);
            long completedDays = Math.Max(0, day - 1);

            return completedYears * MonthsPerYear * DaysPerMonth * HoursPerDay * MinutesPerHour +
                   completedMonths * DaysPerMonth * HoursPerDay * MinutesPerHour +
                   completedDays * HoursPerDay * MinutesPerHour +
                   hour * MinutesPerHour +
                   minute;
        }

        /// <summary>
        /// 根据月份推导季节。
        /// </summary>
        private static GameSeason GetSeasonByMonth(int month)
        {
            if (month >= 3 && month <= 5)
            {
                return GameSeason.Spring;
            }

            if (month >= 6 && month <= 8)
            {
                return GameSeason.Summer;
            }

            if (month >= 9 && month <= 11)
            {
                return GameSeason.Autumn;
            }

            return GameSeason.Winter;
        }

        /// <summary>
        /// 将输入倍率吸附到允许倍率中最近的值。
        /// </summary>
        private static float ResolveAllowedTimeScale(float scale)
        {
            float best = AllowedTimeScales[0];
            float bestDelta = Mathf.Abs(scale - best);
            for (int i = 1; i < AllowedTimeScales.Length; i++)
            {
                float delta = Mathf.Abs(scale - AllowedTimeScales[i]);
                if (delta < bestDelta)
                {
                    best = AllowedTimeScales[i];
                    bestDelta = delta;
                }
            }

            return best;
        }

        #endregion

        #region Scheduler Internals

        /// <summary>
        /// 确保游戏分钟 TimeWheel 已创建。
        /// </summary>
        private void EnsureGameMinuteScheduler()
        {
            if (gameMinuteScheduler == null)
            {
                gameMinuteScheduler = CreateGameMinuteScheduler();
            }
        }

        /// <summary>
        /// 创建以“游戏分钟”为输入单位的时间轮。
        /// tickUnit = 1 表示累计 1 个游戏分钟推进 1 个 TimeWheel tick。
        /// slotCounts = { 1440, 28, 12 } 对齐当前日历：
        /// level 0 覆盖 1440 分钟，即 1 天；
        /// level 1 覆盖 1440 * 28 分钟，即 1 个月；
        /// level 2 覆盖 1440 * 28 * 12 分钟，即 1 年。
        /// </summary>
        private static TimeWheelScheduler CreateGameMinuteScheduler()
        {
            return new TimeWheelScheduler(new TimeWheelConfig(
                tickUnit: 1f,
                slotCounts: new List<int> { 1440, 28, 12 },
                maxCatchUpTicksPerFrame: 1440));
        }

        #endregion

#if UNITY_EDITOR
        #region Editor 测试
        [Header("Editor 测试按钮")]
        [SerializeField] private Key hourSkipButton = Key.F1;
        [SerializeField] private Key daySkipButton = Key.F2;
        [SerializeField] private Key monthSkipButton  = Key.F3;
        [SerializeField] private Key yearSkipButton  = Key.F4;
        [SerializeField] private Key timeScaleButton  = Key.F5;

        private void Update()
        {
            if (Keyboard.current[hourSkipButton].wasPressedThisFrame)
            {
                AdvanceMinutes(60);
            }

            if (Keyboard.current[daySkipButton].wasPressedThisFrame)
            {
                AdvanceMinutes(1440);
            }

            if (Keyboard.current[monthSkipButton].wasPressedThisFrame)
            {
                AdvanceMinutes(1440 * 28);
            }

            if (Keyboard.current[yearSkipButton].wasPressedThisFrame)
            {
                AdvanceMinutes(1440 * 28 * 12);
            }

            if (Keyboard.current[timeScaleButton].wasPressedThisFrame)
            {
                float newScale = timeScale.Value >= 10f ? 0.5f : ResolveAllowedTimeScale(timeScale.Value * 2f);
                SetTimeScale(newScale);
                Debug.Log($"Time scale set to {newScale}x");
            }
        }
        #endregion
#endif
    }
}
