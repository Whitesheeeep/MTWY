using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace WS_Modules.UIModule
{
    /// <summary>
    /// 脚本展示与生成窗口
    /// </summary>
    public class ScriptDisplayWindow : EditorWindow
    {
        private string scriptContent; // 脚本内容
        private string filePath; // 文件路径
        private string mFileName; // 文件名
        private Vector2 scroll = new Vector2(); // 滚动视图位置

        /// <summary>
        /// 显示代码展示窗口
        /// </summary>
        /// <param name="content">要显示的脚本内容</param>
        /// <param name="filePath">脚本文件路径</param>
        /// <param name="_insertDic">需要插入的方法字典 (方法名 -> 方法体)</param>
        /// <param name="fieldList">需要插入的字段列表</param>
        /// <param name="isBindData">是否为 BindData 脚本（决定事件插入位置）</param>
        public static void ShowWindow(string content, string filePath, Dictionary<string, string> _insertDic = null, List<EditorObjectData> fieldList = null, bool isBindData = false)
        {
            //创建代码展示窗口
            ScriptDisplayWindow window = (ScriptDisplayWindow)GetWindowWithRect(typeof(ScriptDisplayWindow), new Rect(100, 50, 800, 700), true, "Window生成界面");
            window.scriptContent = content;
            window.filePath = filePath;
            window.mFileName = Path.GetFileName(filePath);
            //处理代码新增
            string originScript = string.Empty;
            bool isInsterSuccess = false;

            // 如果文件已存在，并且有需要插入的内容，则进行代码注入
            if (File.Exists(window.filePath) && (_insertDic != null || fieldList != null))
            {
                originScript = File.ReadAllText(window.filePath);

                if (!string.IsNullOrEmpty(originScript))
                {
                    if (fieldList != null)
                    {
                        //插入字段(生成item脚本时使用)
                        foreach (var item in fieldList)
                        {
                            // 避免重复插入
                            if (!originScript.Contains($"{item.fieldName}{item.fieldType}"))
                            {
                                string insterArrayType = item.dataList != null ? "[]" : "";
                                string insterArray = item.dataList != null ? "Array" : "";
                                // 插入新增的字段
                                originScript = window.scriptContent = originScript.Insert(window.GetInsertFieldIndex(originScript)
                                    , $"\n\t\tpublic {item.fieldType}{insterArrayType} {item.fieldName}{item.fieldType}{insterArray};\n\t\t");
                                isInsterSuccess = true;
                            }
                        }
                    }
                    if (_insertDic != null)
                    {
                        //插入方法
                        foreach (var item in _insertDic)
                        {
                            // 避免重复插入
                            if (!originScript.Contains(item.Key))
                            {
                                int insterIndex = window.GetInsertMethodIndex(originScript);
                                // 插入新增的方法
                                originScript = window.scriptContent = originScript.Insert(insterIndex, "\n" + item.Value + "\n\t\t");
                                isInsterSuccess = true;
                            }
                        }
                    }


                    if (fieldList != null)
                    {
                        //插入事件(生成item脚本时使用)
                        foreach (var item in fieldList)
                        {
                            string field = $"{item.fieldName}{item.fieldType}";
                            string type = item.fieldType;
                            string methodName = "On" + item.fieldName;
                            string suffix;
                            StringBuilder sb = new StringBuilder();
                            bool hasBinding = false;

                            if (isBindData)
                            {
                                // BindData：使用 target.AddXXXListener 与 mWindow 回调
                                if (type.Contains("Button"))
                                {
                                    suffix = "ButtonClick";
                                    sb.AppendLine($"\t\t\ttarget.AddButtonClickListener({field},mWindow.{methodName}{suffix});");
                                    hasBinding = originScript.Contains($"AddButtonClickListener({field}");
                                }
                                else if (type.Contains("InputField"))
                                {
                                    sb.AppendLine($"\t\t\ttarget.AddInputFieldListener({field},mWindow.{methodName}InputChange,mWindow.{methodName}InputEnd);");
                                    hasBinding = originScript.Contains($"AddInputFieldListener({field}");
                                }
                                else if (type.Contains("Toggle"))
                                {
                                    suffix = "ToggleChange";
                                    sb.AppendLine($"\t\t\ttarget.AddToggleClickListener({field},mWindow.{methodName}{suffix});");
                                    hasBinding = originScript.Contains($"AddToggleClickListener({field}");
                                }
                                else
                                {
                                    continue;
                                }

                                if (!hasBinding)
                                {
                                    int insertIndex = window.GetInitComponentInsertIndex(originScript);
                                    if (insertIndex > -1)
                                    {
                                        string insertText = "\n" + sb.ToString();
                                        originScript = window.scriptContent = originScript.Insert(insertIndex, insertText);
                                        isInsterSuccess = true;
                                    }
                                }
                            }
                            else
                            {
                                // 根据组件类型，生成不同的事件监听代码
                                if (type.Contains("Button"))
                                {
                                    suffix = "ButtonClick";
                                    sb.AppendLine($"\t\t\t{field}.onClick.AddListener({methodName}{suffix});");
                                }
                                else if (type.Contains("InputField"))
                                {
                                    suffix = "InputChange";
                                    sb.AppendLine($"\t\t\t{field}.onValueChanged.AddListener({methodName}{suffix});");
                                    suffix = "InputEnd";
                                    sb.AppendLine($"\t\t\t{field}.onEndEdit.AddListener({methodName}{suffix});");
                                }
                                else if (type.Contains("Toggle"))
                                {
                                    suffix = "ToggleChange";
                                    sb.AppendLine($"\t\t\t{field}.onValueChanged.AddListener({methodName}{suffix});");
                                }
                                else
                                {
                                    continue;
                                }

                                // 避免重复添加事件监听
                                if (!originScript.Contains($"AddListener({methodName}{suffix})"))
                                {
                                    // BindItems：使用占位符插入
                                    sb.Insert(0, "//按钮事件自动注册绑定\n");
                                    originScript = window.scriptContent = originScript.Replace("//按钮事件自动注册绑定", $"{sb}");
                                    isInsterSuccess = true;
                                }
                            }
                        }
                    }
                }

                // 如果没有成功插入任何代码，则显示原始脚本
                if (isInsterSuccess == false)
                {
                    window.scriptContent = originScript;
                }
            }

            originScript = null;
            _insertDic = null;
            window.Show();
        }

        /// <summary>
        /// 绘制编辑器窗口UI
        /// </summary>
        public void OnGUI()
        {
            //绘制ScroView
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(600), GUILayout.Width(800));
            EditorGUILayout.TextArea(scriptContent);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();

            //绘制脚本生成路径
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextArea("脚本生成路径：" + filePath);
            if (GUILayout.Button("选择路径", GUILayout.Width(80)))
            {
                // 打开文件夹选择面板，并保存选择的路径
                filePath = EditorUtility.OpenFolderPanel("脚本生成路径", filePath, "WSUI") + "/" + mFileName;
                EditorPrefs.SetString("GeneratorClassPath", filePath);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            //绘制按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("生成脚本", GUILayout.Height(30)))
            {
                //按钮事件
                ButtonClick();
            }
            EditorGUILayout.EndHorizontal();

        }

        /// <summary>
        /// "生成脚本"按钮的点击事件处理
        /// </summary>
        public void ButtonClick()
        {
            // 如果文件已存在，则删除旧文件
            if (File.Exists(filePath))
                File.Delete(filePath);

            // 创建并写入新的脚本文件
            StreamWriter writer = File.CreateText(filePath);
            writer.Write(scriptContent);
            writer.Close();
            writer.Dispose();
            scriptContent = string.Empty;
            Debug.Log("Create Code finish! Cs path:" + filePath);
            AssetDatabase.Refresh(); // 刷新AssetDatabase以在Unity编辑器中显示新文件
            if (EditorUtility.DisplayDialog("自动化工具", "生成脚本成功！", "确定"))
            {
                Close(); // 关闭窗口
            }
        }
        /// <summary>
        /// 获取插入方法的代码下标
        /// </summary>
        /// <param name="content">脚本内容</param>
        /// <returns>插入点的索引</returns>
        public int GetInsertMethodIndex(string content)
        {
            //找到UI事件组件下面的第一个public 所在的位置 进行插入
            Regex regex = new Regex("UI组件事件");
            Match match = regex.Match(content);
            return match.Index + 6; // 在 "UI组件事件" 注释后插入
        }

        /// <summary>
        /// 获取插入字段的代码下标
        /// </summary>
        /// <param name="content">脚本内容</param>
        /// <returns>插入点的索引</returns>
        public int GetInsertFieldIndex(string content)
        {
            //找到UI事件组件下面的第一个public 所在的位置 进行插入
            Regex regex = new Regex("自定义字段");
            Match match = regex.Match(content);
            return match.Index + 6;

            /*Regex regex1 = new Regex("public");
            MatchCollection matchColltion = regex1.Matches(content);

            // 找到 "自定义字段" 注释后的第一个 public 关键字位置
            for (int i = 0; i < matchColltion.Count; i++)
            {
                if (matchColltion[i].Index > match.Index)
                {
                    return matchColltion[i].Index;
                }
            }
            return -1; // 未找到插入点*/
        }

        /// <summary>
        /// 获取 InitComponent 方法内的插入点（插在第一个匹配的方法结束大括号前）
        /// </summary>
        public int GetInitComponentInsertIndex(string content)
        {
            var match = Regex.Match(content, @"void\s+InitComponent\s*\(");
            if (!match.Success)
                return -1;

            // 从方法声明开始查找方法体
            int braceStart = content.IndexOf('{', match.Index);
            if (braceStart < 0)
                return -1;

            int depth = 0;
            for (int i = braceStart; i < content.Length; i++)
            {
                if (content[i] == '{')
                    depth++;
                else if (content[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        // 插在方法结束的 '}' 之前
                        return i;
                    }
                }
            }

            return -1;
        }

    }
}
