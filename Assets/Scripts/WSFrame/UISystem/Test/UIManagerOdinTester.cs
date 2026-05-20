#if UNITY_EDITOR
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 基于 Odin Inspector 的 UIManager 手动测试组件，用于验证窗口打开、预加载、隐藏、销毁和堆栈弹出流程。
    /// </summary>
    public sealed class UIManagerOdinTester : MonoBehaviour
    {
        [Title("测试参数")]
        [SerializeField] private int waitMilliseconds = 500;

        private void OnEnable()
        {
            UIManager.Instance.WindowStateChanged += OnWindowStateChanged;
            UIManager.Instance.WindowOpened += OnWindowOpened;
            UIManager.Instance.WindowHidden += OnWindowHidden;
            UIManager.Instance.WindowDestroyed += OnWindowDestroyed;
            UIManager.Instance.TopWindowChanged += OnTopWindowChanged;
        }

        private void OnDisable()
        {
            UIManager.Instance.WindowStateChanged -= OnWindowStateChanged;
            UIManager.Instance.WindowOpened -= OnWindowOpened;
            UIManager.Instance.WindowHidden -= OnWindowHidden;
            UIManager.Instance.WindowDestroyed -= OnWindowDestroyed;
            UIManager.Instance.TopWindowChanged -= OnTopWindowChanged;
        }

        /// <summary>
        /// 连续打开同一个窗口两次，验证不会创建重复实例。
        /// </summary>
        [Button("测试重复打开 TestWindow")]
        public async void TestOpenSameWindowTwice()
        {
            TestWindow firstWindow = await UIManager.Instance.PopUpWindowAsync<TestWindow>();
            TestWindow secondWindow = await UIManager.Instance.PopUpWindowAsync<TestWindow>();
            Debug.Log($"[UIManagerOdinTester] 重复打开 TestWindow，firstNull:{firstWindow == null}, secondNull:{secondWindow == null}, sameInstance:{ReferenceEquals(firstWindow, secondWindow)}");
        }

        /// <summary>
        /// 并发打开同一个窗口，验证加载锁会复用同一个实例。
        /// </summary>
        [Button("测试并发打开 TestWindow")]
        public async void TestOpenSameWindowConcurrently()
        {
            UIManager.Instance.DestroyWindow<TestWindow>();
            await UniTask.Delay(waitMilliseconds);

            UniTask<TestWindow> firstTask = UIManager.Instance.PopUpWindowAsync<TestWindow>();
            UniTask<TestWindow> secondTask = UIManager.Instance.PopUpWindowAsync<TestWindow>();
            TestWindow firstWindow = await firstTask;
            TestWindow secondWindow = await secondTask;
            Debug.Log($"[UIManagerOdinTester] 并发打开 TestWindow，firstNull:{firstWindow == null}, secondNull:{secondWindow == null}, sameInstance:{ReferenceEquals(firstWindow, secondWindow)}, visible:{secondWindow?.Visible}");
        }

        /// <summary>
        /// 先预加载窗口再打开窗口，验证预加载实例可以被复用。
        /// </summary>
        [Button("测试预加载后打开 TestWindow2")]
        public async void TestPreloadThenOpen()
        {
            UIManager.Instance.DestroyWindow<TestWindow2>();
            await UniTask.Delay(waitMilliseconds);

            await UIManager.Instance.PreLoadWindowAsync<TestWindow2>();

            TestWindow2 preloadedWindow = UIManager.Instance.GetWindow<TestWindow2>();
            TestWindow2 openedWindow = await UIManager.Instance.PopUpWindowAsync<TestWindow2>();
            Debug.Log($"[UIManagerOdinTester] 预加载后打开 TestWindow2，preloadNull:{preloadedWindow == null}, openNull:{openedWindow == null}, sameInstance:{ReferenceEquals(preloadedWindow, openedWindow)}, visible:{openedWindow?.Visible}");
        }

        /// <summary>
        /// 打开、隐藏、再次打开窗口，验证可见状态可以恢复且不会重复加入可见列表。
        /// </summary>
        [Button("测试隐藏后重新打开 TestWindow")]
        public async void TestHideThenReopen()
        {
            TestWindow openedWindow = await UIManager.Instance.PopUpWindowAsync<TestWindow>();
            UIManager.Instance.HideWindow<TestWindow>();
            await UniTask.Delay(waitMilliseconds);

            TestWindow reopenedWindow = await UIManager.Instance.PopUpWindowAsync<TestWindow>();
            Debug.Log($"[UIManagerOdinTester] 隐藏后重新打开 TestWindow，sameInstance:{ReferenceEquals(openedWindow, reopenedWindow)}, visible:{reopenedWindow?.Visible}");
        }

        /// <summary>
        /// 打开、销毁、再次打开窗口，验证销毁后能够重新创建实例。
        /// </summary>
        [Button("测试销毁后重新打开 TestWindow")]
        public async void TestDestroyThenReopen()
        {
            TestWindow openedWindow = await UIManager.Instance.PopUpWindowAsync<TestWindow>();
            UIManager.Instance.DestroyWindow<TestWindow>();
            await UniTask.Delay(waitMilliseconds);

            TestWindow reopenedWindow = await UIManager.Instance.PopUpWindowAsync<TestWindow>();
            Debug.Log($"[UIManagerOdinTester] 销毁后重新打开 TestWindow，oldNull:{openedWindow == null}, newNull:{reopenedWindow == null}, sameInstance:{ReferenceEquals(openedWindow, reopenedWindow)}, visible:{reopenedWindow?.Visible}");
        }

        /// <summary>
        /// 连续压入两个堆栈窗口并关闭第一个，验证后续窗口可以继续弹出。
        /// </summary>
        [Button("测试堆栈连续弹出")]
        public async void TestStackWindows()
        {
            UIManager.Instance.ClearStackWindows();
            UIManager.Instance.DestroyWindow<TestWindow>();
            UIManager.Instance.DestroyWindow<TestWindow2>();
            await UniTask.Delay(waitMilliseconds);

            UIManager.Instance.PushAndPopStackWindow<TestWindow>(window => Debug.Log($"[UIManagerOdinTester] 栈弹出窗口:{window?.Name}"));
            UIManager.Instance.PushWindowToStack<TestWindow2>(window => Debug.Log($"[UIManagerOdinTester] 栈弹出窗口:{window?.Name}"));
            await UniTask.Delay(waitMilliseconds);

            UIManager.Instance.HideWindow<TestWindow>();
            await UniTask.Delay(waitMilliseconds);

            TestWindow2 nextWindow = UIManager.Instance.GetWindow<TestWindow2>();
            Debug.Log($"[UIManagerOdinTester] 堆栈连续弹出完成，TestWindow2Null:{nextWindow == null}, TestWindow2Visible:{nextWindow?.Visible}");
        }

        /// <summary>
        /// 首次打开窗口时传入 OpenContext，验证 ApplyOpenContext 早于 OnShow。
        /// </summary>
        [Button("测试 OpenContext 首次打开")]
        public async void TestOpenContextFirstOpen()
        {
            UIManager.Instance.DestroyWindow<TestWindow>();
            await UniTask.Delay(waitMilliseconds);

            TestWindowOpenContext context = new TestWindowOpenContext(1, "首次打开");
            TestWindow window = await UIManager.Instance.PopUpWindowAsync<TestWindow, TestWindowOpenContext>(context);
            Debug.Log($"[UIManagerOdinTester] OpenContext首次打开，windowNull:{window == null}, contextId:{window?.LastOpenContext.Id}, message:{window?.LastOpenContext.Message}, applyVersion:{window?.OpenContextVersion}, onShowObserved:{window?.OnShowObservedOpenContextVersion}, beforeOnShow:{window?.OpenContextVersion == window?.OnShowObservedOpenContextVersion}");
        }

        /// <summary>
        /// 预加载后再用 OpenContext 打开窗口，验证预加载实例能收到本次打开参数。
        /// </summary>
        [Button("测试 OpenContext 预加载后打开")]
        public async void TestOpenContextAfterPreload()
        {
            UIManager.Instance.DestroyWindow<TestWindow>();
            await UniTask.Delay(waitMilliseconds);

            await UIManager.Instance.PreLoadWindowAsync<TestWindow>();

            TestWindow preloadedWindow = UIManager.Instance.GetWindow<TestWindow>();
            TestWindowOpenContext context = new TestWindowOpenContext(2, "预加载后打开");
            TestWindow openedWindow = await UIManager.Instance.PopUpWindowAsync<TestWindow, TestWindowOpenContext>(context);
            Debug.Log($"[UIManagerOdinTester] OpenContext预加载后打开，preloadNull:{preloadedWindow == null}, openNull:{openedWindow == null}, sameInstance:{ReferenceEquals(preloadedWindow, openedWindow)}, contextId:{openedWindow?.LastOpenContext.Id}, beforeOnShow:{openedWindow?.OpenContextVersion == openedWindow?.OnShowObservedOpenContextVersion}");
        }

        /// <summary>
        /// 隐藏后使用不同 OpenContext 重新打开，验证本次打开参数会刷新。
        /// </summary>
        [Button("测试 OpenContext 隐藏后刷新")]
        public async void TestOpenContextHideThenReopen()
        {
            TestWindow firstWindow = await UIManager.Instance.PopUpWindowAsync<TestWindow, TestWindowOpenContext>(new TestWindowOpenContext(3, "隐藏前"));
            UIManager.Instance.HideWindow<TestWindow>();
            await UniTask.Delay(waitMilliseconds);

            TestWindow secondWindow = await UIManager.Instance.PopUpWindowAsync<TestWindow, TestWindowOpenContext>(new TestWindowOpenContext(4, "隐藏后"));
            Debug.Log($"[UIManagerOdinTester] OpenContext隐藏后刷新，sameInstance:{ReferenceEquals(firstWindow, secondWindow)}, contextId:{secondWindow?.LastOpenContext.Id}, message:{secondWindow?.LastOpenContext.Message}, applyVersion:{secondWindow?.OpenContextVersion}, beforeOnShow:{secondWindow?.OpenContextVersion == secondWindow?.OnShowObservedOpenContextVersion}");
        }

        /// <summary>
        /// 已显示窗口再次用 OpenContext 打开，验证会先应用参数再触发 OnShow 刷新。
        /// </summary>
        [Button("测试 OpenContext 已显示刷新")]
        public async void TestOpenContextRefreshVisibleWindow()
        {
            TestWindow firstWindow = await UIManager.Instance.PopUpWindowAsync<TestWindow, TestWindowOpenContext>(new TestWindowOpenContext(5, "显示中第一次"));
            TestWindow secondWindow = await UIManager.Instance.PopUpWindowAsync<TestWindow, TestWindowOpenContext>(new TestWindowOpenContext(6, "显示中第二次"));
            Debug.Log($"[UIManagerOdinTester] OpenContext已显示刷新，sameInstance:{ReferenceEquals(firstWindow, secondWindow)}, contextId:{secondWindow?.LastOpenContext.Id}, message:{secondWindow?.LastOpenContext.Message}, applyVersion:{secondWindow?.OpenContextVersion}, beforeOnShow:{secondWindow?.OpenContextVersion == secondWindow?.OnShowObservedOpenContextVersion}");
        }

        /// <summary>
        /// 对未实现 OpenContext 接口的窗口传入参数，验证只输出 warning 且不阻断窗口显示。
        /// </summary>
        [Button("测试 OpenContext 未实现接口")]
        public async void TestOpenContextMissingInterface()
        {
            TestWindow2 window = await UIManager.Instance.PopUpWindowAsync<TestWindow2, TestWindowOpenContext>(new TestWindowOpenContext(7, "未实现接口"));
            Debug.Log($"[UIManagerOdinTester] OpenContext未实现接口，windowNull:{window == null}, visible:{window?.Visible}");
        }

        /// <summary>
        /// 打印当前 UIManager 的窗口快照，用于检查窗口状态、层级和显示顺序。
        /// </summary>
        [Button("打印窗口快照")]
        public void PrintWindowSnapshots()
        {
            IReadOnlyList<UIWindowSnapshot> snapshots = UIManager.Instance.GetWindowSnapshots();
            Debug.Log($"[UIManagerOdinTester] 当前窗口快照数量:{snapshots.Count}");
            foreach (UIWindowSnapshot snapshot in snapshots)
            {
                Debug.Log(FormatSnapshot(snapshot));
            }

            if (UIManager.Instance.TryGetTopWindowSnapshot(out UIWindowSnapshot topSnapshot))
            {
                Debug.Log($"[UIManagerOdinTester] 当前顶层窗口:{FormatSnapshot(topSnapshot)}");
                return;
            }

            Debug.Log("[UIManagerOdinTester] 当前没有顶层窗口");
        }

        private void OnWindowStateChanged(UIWindowStateChangedEventArgs args)
        {
            Debug.Log($"[UIManagerOdinTester] 状态变化:{args.WindowName}, {args.OldState}->{args.NewState}");
        }

        private void OnWindowOpened(UIWindowSnapshot snapshot)
        {
            Debug.Log($"[UIManagerOdinTester] 窗口显示:{FormatSnapshot(snapshot)}");
        }

        private void OnWindowHidden(UIWindowSnapshot snapshot)
        {
            Debug.Log($"[UIManagerOdinTester] 窗口隐藏:{FormatSnapshot(snapshot)}");
        }

        private void OnWindowDestroyed(UIWindowSnapshot snapshot)
        {
            Debug.Log($"[UIManagerOdinTester] 窗口销毁:{FormatSnapshot(snapshot)}");
        }

        private void OnTopWindowChanged(UIWindowTopChangedEventArgs args)
        {
            string oldTop = args.HasOldTop ? args.OldTop.WindowName : "None";
            string newTop = args.HasNewTop ? args.NewTop.WindowName : "None";
            Debug.Log($"[UIManagerOdinTester] 顶层窗口变化:{oldTop}->{newTop}");
        }

        private static string FormatSnapshot(UIWindowSnapshot snapshot)
        {
            return $"Window:{snapshot.WindowName}, State:{snapshot.State}, Visible:{snapshot.Visible}, SortingOrder:{snapshot.SortingOrder}, SiblingIndex:{snapshot.SiblingIndex}, FullScreen:{snapshot.FullScreenWindow}, HasMask:{snapshot.HasMask}, HasGameObject:{snapshot.HasGameObject}";
        }
    }
}
#endif
