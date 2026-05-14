using System;
using System.Collections.Generic;
using UnityEngine;

namespace WS_Modules.Utilities
{
    /// <summary>
    /// 单个时间轮调度器的配置。
    /// </summary>
    [Serializable]
    public sealed class TimeWheelConfig
    {
        private static readonly List<int> DefaultSlotCounts = new List<int> { 256, 64, 64 };

        [SerializeField]
        private float tickSeconds = 0.1f;

        [SerializeField]
        private List<int> slotCounts = new List<int> { 256, 64, 64 };

        [SerializeField]
        private int maxCatchUpTicksPerFrame = 100;

        public float TickSeconds => ValidateTickSeconds(tickSeconds);
        public List<int> SlotCounts => CopyAndValidateSlotCounts(slotCounts);
        public int MaxCatchUpTicksPerFrame => Math.Max(1, maxCatchUpTicksPerFrame);

        internal int LevelCount => SlotCounts.Count;

        public TimeWheelConfig(
            float tickSeconds = 0.1f,
            List<int> slotCounts = null,
            int maxCatchUpTicksPerFrame = 100)
        {
            this.tickSeconds = ValidateTickSeconds(tickSeconds);
            this.slotCounts = CopyAndValidateSlotCounts(slotCounts ?? DefaultSlotCounts);
            this.maxCatchUpTicksPerFrame = Math.Max(1, maxCatchUpTicksPerFrame);
        }

        public TimeWheelConfig CreateRuntimeCopy()
        {
            return new TimeWheelConfig(TickSeconds, SlotCounts, MaxCatchUpTicksPerFrame);
        }

        internal int GetSlotCount(int level)
        {
            return slotCounts[level];
        }

        private static float ValidateTickSeconds(float value)
        {
            if (value <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "tickSeconds 必须大于 0。");
            }

            return value;
        }

        private static List<int> CopyAndValidateSlotCounts(List<int> slotCounts)
        {
            if (slotCounts == null || slotCounts.Count == 0)
            {
                throw new ArgumentException("时间轮至少需要一层。", nameof(slotCounts));
            }

            var copy = new List<int>(slotCounts.Count);
            for (int i = 0; i < slotCounts.Count; i++)
            {
                if (slotCounts[i] <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(slotCounts), "每一层时间轮都至少需要一个 slot。");
                }

                copy.Add(slotCounts[i]);
            }

            return copy;
        }
    }
}
