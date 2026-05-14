using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace WS_Modules.UIModule
{
    public static class WindowCodeGeneratorTool
    {
        static Dictionary<string, string> methodDic = new Dictionary<string, string>();

        private static WSFrameSetting GetSetting()
        {
            var settings = AssetDatabase.FindAssets("t:WSFrameSetting");
            if (settings.Length == 0)
            {
                Debug.LogError("Can not find WSFrameSetting asset.");
                return null;
            }
            var path = AssetDatabase.GUIDToAssetPath(settings[0]);
            return AssetDatabase.LoadAssetAtPath<WSFrameSetting>(path);
        }

        [MenuItem("GameObject/UI自动绑定工具/生成Window脚本(Shift+V) #V", false, 0)]
        internal static void CreateFindComponentScripts()
        {
            GameObject obj = Selection.objects.First() as GameObject; //获取到当前选择的物体
            if (obj == null)
            {
                Debug.LogError("需要选择 GameObject");
                return;
            }

            var setting = WSFrameRoot.Instance?.FrameSetting ?? GetSetting();
            if (setting == null) return;

            //设置脚本生成路径
            if (!Directory.Exists(setting.uiManagerSetting.WindowGeneratorPath))
            {
                Directory.CreateDirectory(setting.uiManagerSetting.WindowGeneratorPath);
            }

            //生成CS脚本
            string csContnet = CreateWindowCs(obj.name);

            Debug.Log("CsConent:\n" + csContnet);
            string cspath = setting.uiManagerSetting.WindowGeneratorPath + "/" + obj.name +
                            ".cs";
            ScriptDisplayWindow.ShowWindow(csContnet, cspath, methodDic);
        }

        /// <summary>
        /// 生成Window脚本
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string CreateWindowCs(string name)
        {
            //储存字段名称
            string datalistJson = PlayerPrefs.GetString(GeneratorConfig.OBJDATALIST_KEY);
            List<EditorObjectData> objDatalist = JsonConvert.DeserializeObject<List<EditorObjectData>>(datalistJson);
            methodDic.Clear();
            StringBuilder sb = new StringBuilder();
            string nameSpaceName = "WS_Modules.UIModule";
            //添加引用
            sb.AppendLine("/*---------------------------------");
            sb.AppendLine(" *Title:UI表现层脚本自动化生成工具");
            sb.AppendLine(" *Date:" + System.DateTime.Now);
            sb.AppendLine(" *Description:UI 表现层，该层只负责界面的交互、表现相关的更新，不允许编写任何业务逻辑代码");
            sb.AppendLine(" *注意:以下文件是自动生成的，再次生成不会覆盖原有的代码，会在原有的代码上进行新增，可放心使用");
            sb.AppendLine("---------------------------------*/");
            sb.AppendLine("using UnityEngine.UI;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            //生成命名空间
            if (!string.IsNullOrEmpty(nameSpaceName))
            {
                sb.AppendLine($"namespace {nameSpaceName}");
                sb.AppendLine("{");
            }

            //生成类命
            sb.AppendLine($"\tpublic class {name}:WindowBase");
            sb.AppendLine("\t{");
            sb.AppendLine("\t");
            // if (WSFrameRoot.Instance.FrameSetting.uiManagerSetting.GeneratorType == GeneratorType.Bind)
            // {
            //生成字段
            sb.AppendLine($"\t\t public {name}DataComponent dataCompt;");
            // }
            // else
            // {
            //     //生成字段
            //     sb.AppendLine($"\t\t public {name}UIComponent uiCompt=new {name}UIComponent();");
            // }


            //生成生命周期函数 Awake
            sb.AppendLine("\t");
            sb.AppendLine($"\t\t #region 生命周期函数");
            sb.AppendLine($"\t\t //调用机制与Mono Awake一致");
            sb.AppendLine("\t\t public override void OnAwake()");
            sb.AppendLine("\t\t {");
            // if (WSFrameRoot.Instance.FrameSetting.uiManagerSetting.GeneratorType == GeneratorType.Bind)
            // {
            sb.AppendLine($"\t\t\t dataCompt=GameObject.GetComponent<{name}DataComponent>();");
            sb.AppendLine($"\t\t\t dataCompt.InitComponent(this);");
            // }
            // else
            //     sb.AppendLine($"\t\t\t uiCompt.InitComponent(this);");
            sb.AppendLine("\t\t\t base.OnAwake();");
            sb.AppendLine("\t\t }");
            //OnShow
            sb.AppendLine($"\t\t //物体显示时执行");
            sb.AppendLine("\t\t public override void OnShow()");
            sb.AppendLine("\t\t {");
            sb.AppendLine("\t\t\t base.OnShow();");
            sb.AppendLine("\t\t }");
            //OnHide
            sb.AppendLine($"\t\t //物体隐藏时执行");
            sb.AppendLine("\t\t public override void OnHide()");
            sb.AppendLine("\t\t {");
            sb.AppendLine("\t\t\t base.OnHide();");
            sb.AppendLine("\t\t }");

            //OnDestroy
            sb.AppendLine($"\t\t //物体销毁时执行");
            sb.AppendLine("\t\t public override void OnDestroy()");
            sb.AppendLine("\t\t {");
            sb.AppendLine("\t\t\t base.OnDestroy();");
            sb.AppendLine("\t\t }");

            sb.AppendLine($"\t\t #endregion");

            //API Function 
            sb.AppendLine($"\t\t #region API Function");
            sb.AppendLine($"\t\t    ");
            sb.AppendLine($"\t\t #endregion");

            //UI组件事件生成
            sb.AppendLine($"\t\t #region UI组件事件");
            foreach (var item in objDatalist)
            {
                string type = item.fieldType;
                string methodName = "On" + item.fieldName;
                string suffix = "";
                if (type.Contains("Button"))
                {
                    suffix = "ButtonClick";
                    CreateMethod(sb, ref methodDic, methodName + suffix);
                }
                else if (type.Contains("InputField"))
                {
                    suffix = "InputChange";
                    CreateMethod(sb, ref methodDic, methodName + suffix, "string text");
                    suffix = "InputEnd";
                    CreateMethod(sb, ref methodDic, methodName + suffix, "string text");
                }
                else if (type.Contains("Toggle"))
                {
                    suffix = "ToggleChange";
                    CreateMethod(sb, ref methodDic, methodName + suffix, "bool state,Toggle toggle");
                }
            }

            sb.AppendLine($"\t\t #endregion");

            sb.AppendLine("\t}");
            if (!string.IsNullOrEmpty(nameSpaceName))
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 生成UI事件方法
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="methodDic"></param>
        /// <param name="modthName"></param>
        /// <param name="param"></param>
        public static void CreateMethod(StringBuilder sb, ref Dictionary<string, string> methodDic, string methodName,
            string param = "")
        {
            //声明UI组件事件
            sb.AppendLine($"\t\t public void {methodName}({param})");
            sb.AppendLine("\t\t {");
            sb.AppendLine("\t\t");
            if (methodName == "OnCloseButtonClick")
            {
                sb.AppendLine("\t\t\tHideWindow();");
            }

            sb.AppendLine("\t\t }");

            //存储UI组件事件 提供给后续新增代码使用
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"\t\t public void {methodName}({param})");
            builder.AppendLine("\t\t {");
            builder.AppendLine("\t\t");
            builder.AppendLine("\t\t }");
            methodDic.Add(methodName, builder.ToString());
        }
    }
}