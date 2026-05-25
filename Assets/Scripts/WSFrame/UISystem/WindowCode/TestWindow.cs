// WSFrame WindowCode 生成规则（以此处说明为准）：
// 1. 本文件首次由生成器创建，创建后作为手写窗口逻辑入口。
// 2. 后续重新生成不会整体覆盖本文件。
// 3. 生命周期方法、API 方法、MVVM 绑定和业务逻辑不会被生成器修改。
// 4. UI 事件方法一旦存在，生成器不会覆盖其方法体。
// 5. 当 UI 新增可绑定事件组件时，生成器只会追加缺失的事件空方法。
// 6. 当 UI 删除、重命名或修改组件类型时，旧事件方法不会自动删除，请手动清理。
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
	public partial class TestWindow:WindowBase, IWindowWithOpenContext<TestWindowOpenContext>
	{
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
			 BindGeneratedComponents();
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
