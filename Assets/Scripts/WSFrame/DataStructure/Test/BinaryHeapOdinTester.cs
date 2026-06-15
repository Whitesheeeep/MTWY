#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace WS_Modules.DataStructure
{
    /// <summary>
    /// 基于 Odin Inspector 的二叉堆手动测试组件，用于验证 int 最小堆和最大堆弹出顺序。
    /// </summary>
    public sealed class BinaryHeapOdinTester : MonoBehaviour
    {
        [Title("测试参数")]
        [SerializeField] private List<int> heapTestValues = new List<int> { 5, 1, 9, 3, 7, 3 };

        [Title("最近结果")]
        [ShowInInspector, ReadOnly] private string lastHeapTestResult = "Ready.";

        /// <summary>
        /// 使用默认 int 比较器执行最小堆测试，期望按升序弹出。
        /// </summary>
        [Button("测试 int 最小堆")]
        public void TestIntMinHeap()
        {
            List<int> output = RunHeapTest(Comparer<int>.Default);
            List<int> expected = GetInputValues().OrderBy(value => value).ToList();
            ReportResult("int 最小堆", output, expected);
        }

        /// <summary>
        /// 使用反向 int 比较器执行最大堆测试，期望按降序弹出。
        /// </summary>
        [Button("测试 int 最大堆")]
        public void TestIntMaxHeap()
        {
            List<int> output = RunHeapTest(Comparer<int>.Create((a, b) => b.CompareTo(a)));
            List<int> expected = GetInputValues().OrderByDescending(value => value).ToList();
            ReportResult("int 最大堆", output, expected);
        }

        private List<int> RunHeapTest(IComparer<int> comparer)
        {
            BinaryHeap<int> heap = new BinaryHeap<int>(comparer);
            foreach (int value in GetInputValues())
            {
                heap.Push(value);
            }

            List<int> output = new List<int>();
            while (heap.Count > 0)
            {
                output.Add(heap.Pop());
            }

            return output;
        }

        private List<int> GetInputValues()
        {
            return heapTestValues != null ? new List<int>(heapTestValues) : new List<int>();
        }

        private void ReportResult(string testName, List<int> output, List<int> expected)
        {
            bool success = output.SequenceEqual(expected);
            string inputText = FormatValues(GetInputValues());
            string outputText = FormatValues(output);
            string expectedText = FormatValues(expected);
            lastHeapTestResult = $"{testName} {(success ? "通过" : "失败")} | input=[{inputText}] output=[{outputText}] expected=[{expectedText}]";

            if (success)
            {
                Debug.Log($"[BinaryHeapOdinTester] {lastHeapTestResult}");
                return;
            }

            Debug.LogError($"[BinaryHeapOdinTester] {lastHeapTestResult}");
        }

        private static string FormatValues(IReadOnlyList<int> values)
        {
            return values == null || values.Count == 0 ? string.Empty : string.Join(", ", values);
        }
    }
}
#endif
