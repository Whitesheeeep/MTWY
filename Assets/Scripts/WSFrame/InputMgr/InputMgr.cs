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

        public enum E_InputEvent
        {
            start = 0,
            end
        }

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
        public Vector2 MoveDir => (isStart && defaultInputSystem != null)
            ? defaultInputSystem.Player.Move.ReadValue<Vector2>().normalized
            : Vector2.zero;
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
#endif
        #endregion
    }
}