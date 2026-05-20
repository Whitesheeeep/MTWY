/*---------------------------------
 *Title:UI表现层脚本自动化生成工具
 *Date:2026/5/19 20:18:25
 *Description:UI 表现层，该层只负责界面的交互、表现相关的更新，不允许编写任何业务逻辑代码
 *注意:以下文件是自动生成的，再次生成不会覆盖原有的代码，会在原有的代码上进行新增，可放心使用
---------------------------------*/
using UnityEngine.UI;
using UnityEngine;

namespace WS_Modules.UIModule
{
	/// <summary>
	/// TestWindow 的临时打开参数，用于 Odin 手动测试 OpenContext 注入顺序。
	/// </summary>
	public readonly struct TestWindowOpenContext
	{
		/// <summary>
		/// 创建 TestWindow 临时打开参数。
		/// </summary>
		/// <param name="id">测试编号。</param>
		/// <param name="message">测试文本。</param>
		public TestWindowOpenContext(int id, string message)
		{
			Id = id;
			Message = message;
		}

		/// <summary>
		/// 测试编号。
		/// </summary>
		public int Id { get; }

		/// <summary>
		/// 测试文本。
		/// </summary>
		public string Message { get; }
	}

	/// <summary>
	/// UIManager 手动测试窗口。
	/// </summary>
	public class TestWindow:WindowBase, IWindowWithOpenContext<TestWindowOpenContext>
	{
	
		 public TestWindowDataComponent dataCompt;

		 /// <summary>
		 /// 最近一次收到的临时打开参数。
		 /// </summary>
		 public TestWindowOpenContext LastOpenContext { get; private set; }

		 /// <summary>
		 /// 临时打开参数应用次数。
		 /// </summary>
		 public int OpenContextVersion { get; private set; }

		 /// <summary>
		 /// OnShow 执行时观察到的临时参数版本，用于确认 ApplyOpenContext 早于 OnShow。
		 /// </summary>
		 public int OnShowObservedOpenContextVersion { get; private set; }
	
		 #region 生命周期函数
		 //调用机制与Mono Awake一致
		 public override void OnAwake()
		 {
			 dataCompt=GameObject.GetComponent<TestWindowDataComponent>();
			 dataCompt.InitComponent(this);
			 base.OnAwake();
		 }
		 //物体显示时执行
		 public override void OnShow()
		 {
			 base.OnShow();
			 OnShowObservedOpenContextVersion = OpenContextVersion;
		 }
		 //物体隐藏时执行
		 public override void OnHide()
		 {
			 base.OnHide();
		 }
		 //物体销毁时执行
		 public override void OnDestroy()
		 {
			 base.OnDestroy();
		 }
		 #endregion
		 #region API Function

		 /// <summary>
		 /// 应用 TestWindow 本次打开的临时参数。
		 /// </summary>
		 /// <param name="context">本次打开参数。</param>
		 public void ApplyOpenContext(TestWindowOpenContext context)
		 {
			 LastOpenContext = context;
			 OpenContextVersion++;
		 }
		 #endregion
		 #region UI组件事件
		 #endregion
	}
}
