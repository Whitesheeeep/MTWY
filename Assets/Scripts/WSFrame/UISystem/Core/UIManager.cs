using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using WS_Modules.Extensions;
using WS_Modules.LogModule;
using WS_Modules.ResLoadModule;
using WS_Modules.Singleton;

namespace WS_Modules.UIModule
{
    public class UIManager : SingletonBase<UIManager>
    {
        private UIManager()
        {
        }

        #region 属性 字段
        /// <summary>
        /// UI摄像机
        /// </summary>
        private Camera _UICamera;
        public Camera Camera => _UICamera;
        /// <summary>
        /// UI节点
        /// </summary>
        private Transform _UIRoot;
        /// <summary>
        /// 窗口配置表
        /// </summary>
        private WindowConfig _windowConfig;
        /// <summary>
        /// 所有已克隆的窗口的字典 (包含显示及隐藏的窗口,不含已销毁的窗口)
        /// </summary>
        private Dictionary<string, WindowBase> _allWindowDic = new Dictionary<string, WindowBase>(); //所有窗口的Dic
        /// <summary>
        /// 所有已克隆的窗口的列表(包含显示及隐藏的窗口,不含已销毁的窗口)
        /// </summary>
        private List<WindowBase> _allWindowList = new List<WindowBase>();
        /// <summary>
        /// //所有可见窗口的列表 
        /// </summary>
        private List<WindowBase> _visibleWindowList = new List<WindowBase>();
        /// <summary>
        /// 模拟弹出栈，用来管理弹窗的循环弹出
        /// </summary>
        private List<WindowBase> _windowStack = new List<WindowBase>(); //列表栈  可在循环弹出时使用
        /// <summary>
        /// 开始弹出堆栈的标记，可以用来处理多种情况，比如：正在出栈种有其他界面弹出，可以直接放到栈内进行弹出 等
        /// </summary>
        private bool _startPopStackWndStatus = false;
        #endregion

        #region 智能显隐
        private bool _smartShowHide = true; //智能显隐开关（可根据情况选择开启或关闭）
        //智能显隐：主要用来优化窗口叠加时被遮挡的窗口参与渲染计算，导致帧率降低的问题。
        //显隐规则：由程序设定某个窗口是否为全屏窗口。(全屏窗口设定方式：在窗口的OnAwake接口中设定该窗口是否为全屏窗口如 FullScreenWindow=true)
        //1.智能隐藏:当FullScreenWindow=true的全屏窗口打开时，框架会自动通过伪隐藏的方式隐藏所有被当前全屏窗口遮挡住的窗口，避免这些看不到的窗口参与渲染运算，
        //从而提高性能。
        //2.智能显示：当FullScreenWindow=true的全屏窗口关闭时，框架会自动找到上一个伪隐藏的窗口把其设置为可见状态，若上一个窗口为非全屏窗口，框架则会找上上个窗口进行显示，
        //以此类推进行循环，直到找到全屏窗口则停止智能显示流程。
        //注意：通过智能显隐进行伪隐藏的窗口在逻辑上仍属于显示中的窗口，可以通过GetWindow获取到该窗口。但是在表现上该窗口为不可见窗口，故称之为伪隐藏。
        //智能显隐逻辑与（打开当前窗口时隐藏其他所有窗口相似）但本质上有非常大的区别，
        //1.通过智能显隐设置为不可见的窗口属于伪隐藏窗口，在逻辑上属于显示中的窗口。
        //2.通过智能显隐设置为不可见的窗口可以通过关闭当前窗口，自动恢复当前窗口之前的窗口的显示。
        //3.通过智能显隐设置为不可见的窗口不会触发UGUI重绘、不会参与渲染计算、不会影响帧率。
        //4.程序只需要通过FullScreenWindow=true配置那些窗口为全屏窗口即可，智能显隐的所有逻辑均有框架自动维护处理。
        #endregion

        /// <summary>
        /// 这是 UIManager 初始化的同步方法
        /// </summary>
        /// <param name="uiManagerSetting"></param>
        public void Initialize(WSFrameSetting.UIManagerSetting uiManagerSetting)
        {
            Initialize(uiManagerSetting.windowConfig,
                uiManagerSetting.uiCameraPrefabPath,
                uiManagerSetting.uiRootPath,
                uiManagerSetting.uiEventSystemPrefabPath).Forget();
        }

        public async UniTaskVoid Initialize(WindowConfig windowConfig, string uiCameraPath = "UICamera",
            string uiRootPath = "UIRoot", string uiEventSystemPath = "UIEventSystem")
        {
            _windowConfig = windowConfig;
            _UIRoot = GameObject.Find("UIRoot")?.transform ??
                      GameObject.Instantiate(ResSystem.Instance.Load<GameObject>(uiRootPath)).transform;
            _UICamera = GameObject.Find("UICamera")?.GetComponent<Camera>() ?? GameObject
                .Instantiate(ResSystem.Instance.Load<GameObject>(uiCameraPath)).GetComponent<Camera>();
            var uiEventSystem = GameObject.Find("UIEventSystem") ??
                                GameObject.Instantiate(
                                    await ResSystem.Instance.LoadAsync<GameObject>(uiEventSystemPath));
            uiEventSystem.name = "UIEventSystem";
            GameObject.DontDestroyOnLoad(_UIRoot);
            GameObject.DontDestroyOnLoad(_UICamera);
            GameObject.DontDestroyOnLoad(uiEventSystem);
        }

        #region 窗口管理
        public async UniTaskVoid PreLoadWindow<T>() where T : WindowBase, new()
        {
            string wndName = typeof(T).Name;
            T windowBase = new T();
            GameObject window = await LoadWindow(wndName);
            if (window is not null)
            {
                /*windowBase.GameObject = window;
                windowBase.Transform = window.transform;
                windowBase.Canvas = window.GetComponent<Canvas>();
                windowBase.Canvas.worldCamera = _UICamera;
                windowBase.Name = window.name;*/
                BindAndSetWindowBase(windowBase, window, false);
                _allWindowDic.Add(wndName, windowBase);
                _allWindowList.Add(windowBase);
            }

            WSLog.Log("预加载窗口完成，窗口名称:" + wndName);
        }

        public void PopUpWindow<T>() where T : WindowBase, new() => PopUpWindowAsync<T>().Forget();

        private WindowBase PopUpWindow(WindowBase wnd)
        {
            string wndName = wnd.Name;
            if (_allWindowDic.TryGetValue(wndName, out WindowBase windowBase) && windowBase is not null)
            {
                return ShowWindow(wndName);
            }

            WSLog.Log("弹出窗口，窗口名称:" + wndName);
            InitializeWindow(wnd).Forget();
            return wnd;
        }
        
        public async UniTask<T> PopUpWindowAsync<T>() where T : WindowBase, new()
        {
            string wndName = typeof(T).Name;
            if (_allWindowDic.TryGetValue(wndName, out WindowBase windowBase) && windowBase is not null)
            {
                return ShowWindow(wndName) as T;
            }

            T wnd = new T();
            WSLog.Log("弹出窗口，窗口名称:" + wndName);
            await InitializeWindow(wnd);
            return wnd;
        }

        public void HideWindow<T>() where T : WindowBase => HideWindow(typeof(T).Name);

        public void HideWindow(string windowName)
        {
            if (_allWindowDic.TryGetValue(windowName, out WindowBase windowBase) && windowBase is not null)
            {
                HideWindow(windowBase);
                PopNextStackWindow(windowBase);
            }
        }

        private void HideWindow(WindowBase windowBase)
        {
            if (windowBase is { GameObject: not null, Visible: true })
            {
                _visibleWindowList.Remove(windowBase);
                windowBase.SetVisible(false);
                SetWindowMaskVisible();
                HideWindowAndModifyAllWindowCanvasGroup(windowBase, true);
                windowBase.OnHide();
            }
        }

        public void DestroyWindow<T>() where T : WindowBase => DestroyWindow(typeof(T).Name);

        public void DestroyWindow(string windowName)
        {
            if (_allWindowDic.TryGetValue(windowName, out WindowBase windowBase) && windowBase is not null)
            {
                if (windowBase.Visible)
                {
                    HideWindow(windowBase);
                }

                _allWindowDic.Remove(windowName);
                _allWindowList.Remove(windowBase);
                _visibleWindowList.Remove(windowBase);

                windowBase.OnDestroy();
                PopNextStackWindow(windowBase);
                // 逻辑执行完毕，卸载预制体资源并销毁窗口预制体对象，注意：这里的卸载资源和销毁预制体对象的逻辑需要放在最后执行，确保在窗口的生命周期内，窗口预制体对象和预制体资源都是存在的，避免出现资源被卸载或者预制体对象被销毁导致的错误
                ResSystem.Instance.UnLoadAsync<GameObject>(_windowConfig.GetWindowData(windowName).windowPrefabPath,
                    null);
                GameObject.Destroy(windowBase.GameObject);
            }
        }

        public T GetWindow<T>() where T : WindowBase
        {
            string wndName = typeof(T).Name;
            if (_allWindowDic.TryGetValue(wndName, out WindowBase windowBase) && windowBase is not null)
            {
                return windowBase as T;
            }

            WSLog.LogError("窗口字典中不存在窗口，无法获取窗口，窗口名称:" + wndName);
            return null;
        }

        // 弹出窗口的流程：1.加载窗口预制体 2.实例化窗口预制体 3.绑定窗口基类组件 4.把窗口添加到窗口字典和窗口列表中进行管理
        private async UniTask InitializeWindow<T>(T wnd) where T : WindowBase
        {
            string wndName = typeof(T).Name;
            // 加载并实例化窗口预制体
            GameObject window = await LoadWindow(wndName);
            if (window is not null)
            {
                // 初始化窗口完成，把窗口添加到窗口字典和窗口列表中进行管理，注意：这里的窗口字典和窗口列表只管理已经克隆出来的窗口预制体，
                // 不管该窗口预制体是显示还是隐藏状态，只要没有被销毁就会被添加到窗口字典和窗口列表中进行管理
                BindAndSetWindowBase(wnd, window, true);

                RemoveContainsWindow(wndName, wnd);
                _allWindowDic.Add(wndName, wnd);
                _allWindowList.Add(wnd);
                _visibleWindowList.Add(wnd);
                // TODO_: 智慧显隐
                ShowWindowAndModifyAllWindowCanvasGroup(wnd, false);
                // TODO_: 单遮多遮
                SetWindowMaskVisible();
            }
            else
            {
                WSLog.LogError("弹出窗口失败，无法加载窗口预制体，窗口名称:" + wndName);
            }
        }

        /// <summary>
        /// 绑定窗口基类组件，主要用来处理预加载窗口和非预加载窗口的绑定逻辑，预加载窗口在预加载时已经完成了 Data 和 Window 的连接，而非预加载窗口则需要在弹出时进行连接
        /// </summary>
        /// <param name="windowBase"></param>
        /// <param name="window"></param>
        /// <param name="isVisible"></param>
        /// <typeparam name="T"></typeparam>
        private void BindAndSetWindowBase<T>(T windowBase, GameObject window, bool isVisible)
            where T : WindowBase
        {
            // 初始化窗口基类组件，并且在窗口基类组件的初始化方法中，尝试获取窗口数据组件，如果存在则调用其 InitData 方法进行数据初始化
            windowBase.OnAwake(window, _UICamera);
            windowBase.SetVisible(isVisible);
            if (isVisible) windowBase.OnShow();
            // 设置窗口预制体的 RectTransform 为全屏拉伸，确保窗口预制体在 Canvas 下能够正确显示
            RectTransform rectTrans = window.GetComponent<RectTransform>();
            rectTrans.SetFullStretch();
        }

        private void RemoveContainsWindow(string wndName, WindowBase wnd)
        {
            if (_allWindowDic.TryGetValue(wndName, out var windowBase))
            {
                if (windowBase is { GameObject: not null })
                {
                    // 去除除已经销毁的窗口预制体对象，避免窗口字典中存在已经销毁的窗口预制体对象导致的错误
                    GameObject.Destroy(windowBase.GameObject);
                    ResSystem.Instance.UnLoadAsync<GameObject>(_windowConfig.GetWindowData(wndName).windowPrefabPath,
                        null);
                    _allWindowDic.Remove(wndName);
                }
                else if (windowBase is { GameObject: null })
                    _allWindowDic.Remove(wndName);

                _allWindowList.Remove(windowBase);
                _visibleWindowList.Remove(windowBase);

                WSLog.LogWarning("窗口字典中已经存在窗口，正在移除已存在的窗口，窗口名称:" + wndName);
            }
        }

        // 加载 window 预制体，返回预制体对象，注意：该方法只负责加载预制体，不负责实例化预制体，实例化预制体的逻辑由 ShowWindow 方法来处理
        private async UniTask<GameObject> LoadWindow(string windowName)
        {
            if (_windowConfig is null)
            {
                WSLog.LogError("窗口配置表未设置，无法加载窗口预制体，窗口名称:" + windowName);
                return null;
            }

            var windowData = _windowConfig.GetWindowData(windowName);
            if (windowData is null)
            {
                WSLog.LogError("窗口配置表中不存在窗口数据，无法加载窗口预制体，窗口名称:" + windowName);
                return null;
            }

            GameObject windowPrefab = await ResSystem.Instance.LoadAsync<GameObject>(windowData.windowPrefabPath);
            GameObject window = GameObject.Instantiate(windowPrefab, _UIRoot, true);
            window.transform.Reset();
            window.name = windowName;
            return window;
        }

        private WindowBase ShowWindow(string windowName)
        {
            WindowBase wnd;
            if (_allWindowDic.TryGetValue(windowName, out wnd))
            {
                WSLog.Log("显示窗口，窗口名称:" + windowName);
                if (wnd is { GameObject: not null, Visible: false })
                {
                    _visibleWindowList.Add(wnd);
                    // 把当前窗口设置为最前面，保证当前窗口在最上层显示
                    wnd.Transform.SetAsLastSibling();
                    wnd.SetVisible(true);
                    // TODO_: 単遮多遮
                    SetWindowMaskVisible();
                    // TODO_: 智能显隐
                    ShowWindowAndModifyAllWindowCanvasGroup(wnd, false);
                    wnd.OnShow();
                    WSLog.Log("窗口显示成功，窗口名称:" + windowName);
                }
                // 窗口若已经弹出，调用 OnShow 生命周期接口刷新界面数据
                else if (wnd is { GameObject: not null, Visible: true })
                {
                    wnd.OnShow();
                }
            }
            else
                WSLog.LogError("窗口字典中不存在窗口，无法显示窗口，窗口名称:" + windowName);

            return wnd;
        }

        private void SetWindowMaskVisible()
        {
            if (WSFrameRoot.Instance is null || WSFrameRoot.Instance.FrameSetting is null ||
                WSFrameRoot.Instance.FrameSetting.uiManagerSetting is null) return;
            var setting = WSFrameRoot.Instance.FrameSetting.uiManagerSetting;
            if (!setting.isSingleMask) return;
            WSLog.Log("设置窗口遮罩显示状态，当前可见窗口数量:" + _visibleWindowList.Count);
            WindowBase maxOrderWndBase = null; //最大渲染层级的窗口
            int maxOrder = 0; //最大渲染层级
            int maxIndex = 0; //最大排序下标 在相同父节点下的位置下标
            //1.关闭所有窗口的Mask 设置为不可见
            //2.从所有可见窗口中找到一个层级最大的窗口，把Mask设置为可见
            foreach (var window in _visibleWindowList)
            {
                if (window != null && window.GameObject != null)
                {
                    window.SetMaskVisible(false);
                    if (maxOrderWndBase == null)
                    {
                        maxOrderWndBase = window;
                        maxOrder = window.Canvas.sortingOrder;
                        maxIndex = window.Transform.GetSiblingIndex();
                    }
                    else
                    {
                        //找到最大渲染层级的窗口，拿到它
                        if (maxOrder < window.Canvas.sortingOrder)
                        {
                            maxOrderWndBase = window;
                            maxOrder = window.Canvas.sortingOrder;
                        }
                        //如果两个窗口的渲染层级相同，就找到同节点下最靠下一个物体，优先渲染Mask    
                        else if (maxOrder == window.Canvas.sortingOrder &&
                                 maxIndex < window.Transform.GetSiblingIndex())
                        {
                            maxOrderWndBase = window;
                            maxIndex = window.Transform.GetSiblingIndex();
                        }
                    }
                }
            }
            // WSLog.Log("当前最大渲染层级的窗口名称:" + maxOrderWndBase?.Name);
            maxOrderWndBase?.SetMaskVisible(true);
        }
        #endregion

        #region 智能显隐
        private void ShowWindowAndModifyAllWindowCanvasGroup(WindowBase window, bool canInteract)
        {
            if (!_smartShowHide)
            {
                return;
            }

            //if (WorldManager.IsHallWorld && window.FullScreenWindow) 可以以此种方式决定智能显隐开启场景
            if (window.FullScreenWindow)
            {
                try
                {
                    // 4. 层级保护检查 (防误杀逻辑)
                    //    检测场景：如果打开了一个全屏窗口，但它其实是在“底层”显示的（比如更换了背景），
                    //    那么位于它上层的悬浮窗不应该被隐藏。
                    if (_visibleWindowList.Count > 1)
                    {
                        // 获取当前窗口列表中的倒数第二个窗口（也就是在新窗口 window 打开之前，处于最上层的那个窗口）
                        WindowBase curShowBase = _visibleWindowList[^2];

                        // 判断条件：
                        // A. !curShowBase.FullScreenWindow: 上一个窗口不是全屏窗口（比如是一个小悬浮窗）
                        // B. window.Canvas.sortingOrder < curShowBase.Canvas.sortingOrder: 新打开的全屏窗口层级 < 上一个窗口
                        // 结论：新来的全屏窗口是被压在底下的。
                        if (!curShowBase.FullScreenWindow &&
                            window.Canvas.sortingOrder < curShowBase.Canvas.sortingOrder)
                        {
                            return; // 这种情况不能隐藏其他窗口，否则顶部的悬浮窗就没了。
                        }
                    }

                    // 5. 执行伪隐藏
                    //    如果通过了以上所有检查，说明确实打开了一个盖在最上面的全屏窗口。
                    //    那么遍历所有可见窗口，把除自己以外的其他窗口都隐藏掉。
                    for (int i = _visibleWindowList.Count - 1; i >= 0; i--)
                    {
                        WindowBase item = _visibleWindowList[i];
                        if (item.Name != window.Name) // 排除自己
                        {
                            item.PseudoHidden(canInteract); // 将 CanvasGroup Alpha 设为 0
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error:" + ex);
                }
            }
        }

        private void HideWindowAndModifyAllWindowCanvasGroup(WindowBase window, bool canInteract)
        {
            if (!_smartShowHide)
            {
                return;
            }

            //if (WorldManager.IsHallWorld && window.FullScreenWindow) 可以以此种方式决定智能显隐开启场景
            if (window.FullScreenWindow)
            {
                for (int i = _visibleWindowList.Count - 1; i >= 0; i--)
                {
                    if (_visibleWindowList[i] != window)
                    {
                        _visibleWindowList[i].PseudoHidden(canInteract);
                        //找到上一个窗口，如果是全屏窗口，将其设置可见，终止循转。否则循环至最终
                        if (_visibleWindowList[i].FullScreenWindow)
                        {
                            break;
                        }
                    }
                }
            }
        }
        #endregion
        
        #region 堆栈系统
    /// <summary>
    /// 弹出堆栈中第一个弹窗
    /// </summary>
    public void StartPopFirstStackWindow()
    {
        if (_startPopStackWndStatus) return;
        _startPopStackWndStatus = true;//已经开始进行堆栈弹出的流程，
        PopStackWindow();
    }
    /// <summary>
    /// 进栈一个界面
    /// </summary>
    /// <param name="popCallBack">压栈弹窗弹出回调</param>
    /// <param name="single">是否只允许存在一个</param>
    /// <param name="pushToStackTop">是否压到栈顶(优先弹出)</param>
    /// <typeparam name="T">准备压栈的弹窗</typeparam>
    public void PushWindowToStack<T>(Action<WindowBase> popCallBack = null, bool single = false, bool pushToStackTop = false) where T : WindowBase, new()
    {
        string winName = typeof(T).Name;
        if (single)
        {
            //压栈去重
            foreach (var item in _windowStack)
            {
                if (item.Name.Equals(winName)) return; 
            }
            
            //压栈去显
            WindowBase win = GetWindow<T>();
            if (win != null)
            {
                Debug.Log($"{winName} 弹窗已显示，single模式不处理压栈");
                win.OnShow();
                return;
            }
        }
        
        Debug.Log($"Stack Window Push :{winName}" );
        
        T wndBase = new T { PopStackListener = popCallBack, Name = winName };
        
        if (pushToStackTop )
        {
            _windowStack.Insert(0, wndBase);
            return;
        }
        _windowStack.Add(wndBase);
    }
    
    /// <summary>
    /// 压入并且弹出堆栈弹窗，若已弹出则只压入
    /// </summary>
    /// <typeparam name="T">准备压栈的弹窗</typeparam>
    /// <param name="popCallBack">压栈弹窗弹出回调</param>
    /// <param name="single">是否只允许存在一个</param>
    /// <param name="pushToStackTop">是否压到栈顶(优先弹出)</param>
    public void PushAndPopStackWindow<T>(Action<WindowBase> popCallBack = null,bool single = false, bool pushToStackTop = false) where T : WindowBase, new()
    {
        PushWindowToStack<T>(popCallBack,single,pushToStackTop);
        StartPopFirstStackWindow();
    }
    /// <summary>
    /// 弹出堆栈中的下一个窗口
    /// </summary>
    /// <param name="windowBase"></param>
    private void PopNextStackWindow(WindowBase windowBase)
    {
        if (windowBase != null && _startPopStackWndStatus && windowBase.PopStack)
        {
            windowBase.PopStack = false;
            PopStackWindow();
        }
    }
    /// <summary>
    /// 弹出堆栈弹窗
    /// </summary>
    /// <returns></returns>
    public bool PopStackWindow()
    {
        if (_windowStack.Count > 0)
        {
            WindowBase window = _windowStack[0];
            _windowStack.RemoveAt(0);
            WindowBase popWindow = PopUpWindow(window);
            popWindow.PopStackListener = window.PopStackListener;
            popWindow.PopStack = true;
            popWindow.PopStackListener?.Invoke(popWindow);
            popWindow.PopStackListener = null;
            return true;
        }
        else
        {
            _startPopStackWndStatus = false;
            return false;
        }
    }
    public void ClearStackWindows()
    {
        _windowStack.Clear();
    }
    #endregion
    }
}