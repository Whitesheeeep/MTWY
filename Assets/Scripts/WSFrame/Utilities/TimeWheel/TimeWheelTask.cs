using System;
using WS_Modules.Pooling;

namespace WS_Modules.Utilities
{
    internal enum TimeWheelTaskState
    {
        Scheduled,
        Paused,
        Cancelled,
        Executing,
        Completed
    }

    internal sealed class TimeWheelTask : IPoolable
    {
        internal long Id = IdGenerator.InvalidId;
        internal int Version;
        internal int ScheduleVersion;
        internal TimeWheelTaskState State = TimeWheelTaskState.Completed;

        internal long DueTick;
        internal long RemainingTicks;
        internal long IntervalTicks;
        internal int RemainingRepeatCount;
        internal bool IsRepeating;
        internal bool IsActiveCounted;
        internal Action Callback;

        public int MaxCount => 1000;
        public int InitCount => 0;

        public void OnSpawn()
        {
        }

        public void OnDespawn()
        {
            Recycle();
        }

        internal void Init(long delayTicks, long dueTick, Action callback)
        {
            Id = IdGenerator.Next();
            Version++;
            ScheduleVersion = 0;
            State = TimeWheelTaskState.Scheduled;
            DueTick = dueTick;
            RemainingTicks = delayTicks;
            IntervalTicks = 0;
            RemainingRepeatCount = 0;
            IsRepeating = false;
            IsActiveCounted = true;
            Callback = callback;
        }

        internal void SetRepeat(long intervalTicks, int repeatCount)
        {
            IsRepeating = true;
            IntervalTicks = intervalTicks;
            RemainingRepeatCount = repeatCount;
        }

        internal void Recycle()
        {
            Id = IdGenerator.InvalidId;
            Version++;
            ScheduleVersion++;
            State = TimeWheelTaskState.Completed;
            DueTick = 0;
            RemainingTicks = 0;
            IntervalTicks = 0;
            RemainingRepeatCount = 0;
            IsRepeating = false;
            IsActiveCounted = false;
            Callback = null;
        }
    }
}
