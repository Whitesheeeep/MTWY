using UnityEngine;
using WS_Modules.MonoSystem;
using WS_Modules.CustomEventSystem;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace WS_Modules.InputModule
{
    /// <summary>
    /// 输入管理器，支持旧输入系统 (Input) 和新输入系统 (Input System)
    /// </summary>
    public class InputMgr : Singleton.SingletonBase<InputMgr>
    {
        private bool isStart = true;
        private IEventCenter<E_InputEvent> eventCenter = new EventCenterModule<E_InputEvent>();
        private PlayerDirection lastMoveDirection = PlayerDirection.Down;
        private Vector2 lastRawMoveInput;

        public enum E_InputEvent
        {
            start = 0,
            end
        }

        #region Mouse Properties
        /// <summary>
        /// 鼠标左键是否在当前帧按下。
        /// </summary>
        public bool LeftMouseWasPressedThisFrame
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return isStart && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// 鼠标左键是否在当前帧释放。
        /// </summary>
        public bool LeftMouseWasReleasedThisFrame
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return isStart && Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// 鼠标当前屏幕坐标。
        /// </summary>
        public Vector2 MouseScreenPosition
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return isStart && Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
                return Vector2.zero;
#endif
            }
        }
        #endregion

        private InputMgr()
        {
            // 注册到公共 Mono 的 Update 中
            PublicMono.Instance.RegisterUpdate(OnUpdate);
#if ENABLE_INPUT_SYSTEM
            // 初始化新输入系统
            InitInputSystem();
#endif
        }

        /// <summary>
        /// 开启或关闭输入检测
        /// </summary>
        /// <param name="isOpen"></param>
        public void SetInputStatus(bool isOpen)
        {
            isStart = isOpen;
            #if ENABLE_INPUT_SYSTEM
            if (defaultInputSystem != null)
            {
                if (isOpen)
                    defaultInputSystem.Enable();
                else
                    defaultInputSystem.Disable();
            }
            #endif
        }

        private void OnUpdate()
        {
            if (!isStart) return;

            // 旧输入系统逻辑
            CheckOldInput();

            // 新输入系统逻辑 (如果安装了插件并在宏定义中开启)
#if ENABLE_INPUT_SYSTEM
            CheckNewInput();
            if (defaultInputSystem != null)
            {
                UpdateLastMoveDirection(defaultInputSystem.Player.Move.ReadValue<Vector2>());
            }
#endif
        }

        #region 旧输入系统 (Legacy Input)
        private void CheckOldInput()
        {
            // 示例：检测常用按键
            CheckKeyCode(KeyCode.W);
            CheckKeyCode(KeyCode.A);
            CheckKeyCode(KeyCode.S);
            CheckKeyCode(KeyCode.D);
            CheckKeyCode(KeyCode.Space);
            CheckKeyCode(KeyCode.Escape);

            // 检测鼠标
            CheckMouse(0); // 左键
            CheckMouse(1); // 右键
        }

        private void CheckKeyCode(KeyCode key)
        {
            if (Input.GetKeyDown(key))
            {
                //EventSystem.EventTrigger_Int((int)EventSystem.E_InputEvent.OnKeyDown, key);
            }

            if (Input.GetKeyUp(key))
            {
                //EventSystem.EventTrigger_Int((int)EventSystem.E_InputEvent.OnKeyUp, key);
            }

            if (Input.GetKey(key))
            {
                //EventSystem.EventTrigger_Int((int)EventSystem.E_InputEvent.OnKey, key);
            }
        }

        private void CheckMouse(int mouseBtn)
        {
            if (Input.GetMouseButtonDown(mouseBtn))
            {
                //EventSystem.EventTrigger_Int((int)EventSystem.E_InputEvent.OnMouseButtonDown, mouseBtn);
            }

            if (Input.GetMouseButtonUp(mouseBtn))
            {
                //EventSystem.EventTrigger_Int((int)EventSystem.E_InputEvent.OnMouseButtonUp, mouseBtn);
            }

            if (Input.GetMouseButton(mouseBtn))
            {
                //EventSystem.EventTrigger_Int((int)EventSystem.E_InputEvent.OnMouseButton, mouseBtn);
            }
        }
        #endregion

        #region 新输入系统 (New Input System)
#if ENABLE_INPUT_SYSTEM
        private GlobalInput defaultInputSystem;
#endif
        public Vector2 MoveDir
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return (isStart && defaultInputSystem != null)
                    ? defaultInputSystem.Player.Move.ReadValue<Vector2>().normalized
                    : Vector2.zero;
#else
                return Vector2.zero;
#endif
            }
        }

        public bool IsRunPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return isStart && defaultInputSystem != null && defaultInputSystem.Player.Run.IsPressed();
#else
                return false;
#endif
            }
        }

        public PlayerDirection LastMoveDirection
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (isStart && defaultInputSystem != null)
                {
                    UpdateLastMoveDirection(defaultInputSystem.Player.Move.ReadValue<Vector2>());
                }
#endif
                return lastMoveDirection;
            }
        }

#if ENABLE_INPUT_SYSTEM
        private void InitInputSystem()
        {
            if (defaultInputSystem == null)
            {
                defaultInputSystem = new GlobalInput();
                defaultInputSystem.Enable();
            }

            if (isStart)
                defaultInputSystem.Enable();
        }

        // 这里可以根据需要添加新输入系统的特定逻辑
        // 例如通过 Action Map 触发事件
        private void CheckNewInput()
        {
            // 新输入系统通常是基于回调的，但如果需要每帧检查也可以在这里处理
        }
        private void UpdateLastMoveDirection(Vector2 rawMoveInput)
        {
            bool horizontalChanged = AxisBecameActiveOrChangedSign(lastRawMoveInput.x, rawMoveInput.x);
            bool verticalChanged = AxisBecameActiveOrChangedSign(lastRawMoveInput.y, rawMoveInput.y);

            if (horizontalChanged && !verticalChanged)
            {
                lastMoveDirection = rawMoveInput.x > 0f ? PlayerDirection.Right : PlayerDirection.Left;
            }
            else if (verticalChanged && !horizontalChanged)
            {
                lastMoveDirection = rawMoveInput.y > 0f ? PlayerDirection.Up : PlayerDirection.Down;
            }
            else if (rawMoveInput == Vector2.zero)
            {
                lastRawMoveInput = rawMoveInput;
                return;
            }
            else if (lastRawMoveInput == Vector2.zero)
            {
                lastMoveDirection = Mathf.Abs(rawMoveInput.x) >= Mathf.Abs(rawMoveInput.y)
                    ? rawMoveInput.x > 0f ? PlayerDirection.Right : PlayerDirection.Left
                    : rawMoveInput.y > 0f ? PlayerDirection.Up : PlayerDirection.Down;
            }

            lastRawMoveInput = rawMoveInput;
        }

        private static bool AxisBecameActiveOrChangedSign(float previous, float current)
        {
            if (Mathf.Approximately(current, 0f))
            {
                return false;
            }

            return Mathf.Approximately(previous, 0f) || Mathf.Sign(previous) != Mathf.Sign(current);
        }
#endif
        #endregion
    }
}
