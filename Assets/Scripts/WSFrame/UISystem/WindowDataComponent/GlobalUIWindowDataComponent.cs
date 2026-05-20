/*---------------------------------
 *Date:2026/5/19 17:07:25
 *Description:变量需要以[Text]括号加组件类型的格式进行声明，然后右键窗口物体—— 一键生成UI数据组件脚本即可
 *注意:以下文件是自动生成的，任何手动修改都会被下次生成覆盖,若手动修改后,尽量避免自动生成
---------------------------------*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WS_Modules.UIModule
{
	public class GlobalUIWindowDataComponent : MonoBehaviour
	{
		//自定义字段
		public InventoryBarView BarFrameInventoryBarView;
		
		public Button BagButton;

		public void InitComponent(WindowBase target)
		{
		     //组件事件绑定
		     GlobalUIWindow mWindow=(GlobalUIWindow)target;
		     target.AddButtonClickListener(BagButton,mWindow.OnBagButtonClick);
		}
	}
}
