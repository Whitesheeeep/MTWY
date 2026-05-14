/*---------------------------------
 *Date:2026/3/16 17:26:22
 *Description:变量需要以[Text]括号加组件类型的格式进行声明，然后右键窗口物体—— 一键生成UI数据组件脚本即可
 *注意:以下文件是自动生成的，任何手动修改都会被下次生成覆盖,若手动修改后,尽量避免自动生成
---------------------------------*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WS_Modules.UIModule
{
	public class TestWindowDataComponent : MonoBehaviour
	{
		//自定义字段
		public Button Button2Button;
		
		public Image ImageImage;

		public Button ButtonButton;

		public RawImage ffffRawImage;

		public TextMeshProUGUI fasTextMeshProUGUI;

		public Image Test3Image;

		public void InitComponent(WindowBase target)
		{
		     //组件事件绑定
		     TestWindow mWindow=(TestWindow)target;
		     target.AddButtonClickListener(ButtonButton,mWindow.OnButtonButtonClick);
		
			target.AddButtonClickListener(Button2Button,mWindow.OnButton2ButtonClick);
}
	}
}
