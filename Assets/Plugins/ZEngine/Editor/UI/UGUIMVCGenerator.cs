//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using ZEngine.Manager.UI.UGUI.Components;

namespace ZEngine.Editor.UI
{
    /// <summary>
    /// UGUI MVC 自动生成工具：从已有 Prefab 反推生成 View / Controller / Model 脚本。
    ///
    /// 工作流：
    ///   1. 选中 Prefab（或通过 UI 创建 Prefab）
    ///   2. 打开 Tools / ZEngine / UGUI MVC Generator
    ///   3. 输入面板名 + 输出目录 → 自动扫描 Prefab 子节点上的包装组件生成 [UIBind] 字段
    ///
    /// 扫描规则：
    ///   - 扫描 Prefab 根节点及子节点上挂载的 UIComponentBase 子类组件
    ///   - 按相对路径生成 [UIBind("path")] 字段
    ///   - 自动推断字段名（驼峰命名，如 "CloseBtn" → _closeBtn）
    ///   - 若 Prefab 未提供，可在窗口手动描述所需组件（自然语言 → 组件映射）
    /// </summary>
    public class UGUIMVCGenerator : EditorWindow
    {
        private string _prefix = "";
        private GameObject _prefab;
        private DefaultAsset _outputFolder;
        private string _outputPath = "Assets/GameScripts/Hotfix/Logic/UI";

        // 从 Prefab 扫描到的组件信息
        private readonly List<BindInfo> _bindInfos = new List<BindInfo>();
        private Vector2 _scroll;

        // 手动补充的组件描述
        private string _manualComponents = "";

        private struct BindInfo
        {
            public string Path;
            public string FieldName;
            public string ComponentType;
            public string EventHint; // "onClick" / "onValueChanged" / "onEndEdit" / ""
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("UGUI MVC 脚本生成器", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "从 Prefab 反推生成 View / Controller / Model 脚本。\n" +
                "Prefab 子节点上挂载的 UGUI 包装组件会被自动识别并生成 [UIBind] 字段。",
                MessageType.Info);
            GUILayout.Space(8);

            // ── 面板名 ──
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("面板名", GUILayout.Width(60));
            _prefix = EditorGUILayout.TextField(_prefix);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4);

            // ── Prefab ──
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Prefab", GUILayout.Width(60));
            _prefab = (GameObject)EditorGUILayout.ObjectField(_prefab, typeof(GameObject), false);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("从 Prefab 扫描组件", GUILayout.Height(24)) && _prefab != null)
                ScanPrefab();

            // ── 输出目录 ──
            GUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("输出目录", GUILayout.Width(60));
            _outputFolder = (DefaultAsset)EditorGUILayout.ObjectField(_outputFolder, typeof(DefaultAsset), false);
            EditorGUILayout.EndHorizontal();
            if (_outputFolder != null)
                _outputPath = AssetDatabase.GetAssetPath(_outputFolder);
            EditorGUILayout.LabelField(_outputPath, EditorStyles.miniLabel);

            // ── 扫描结果预览 ──
            if (_bindInfos.Count > 0)
            {
                GUILayout.Space(8);
                EditorGUILayout.LabelField($"扫描到 {_bindInfos.Count} 个组件：", EditorStyles.boldLabel);
                foreach (var info in _bindInfos)
                    EditorGUILayout.LabelField($"  [{info.ComponentType}] [UIBind(\"{info.Path}\")] private {info.ComponentType} {info.FieldName};");
            }

            // ── 手动补充组件 ──
            GUILayout.Space(8);
            EditorGUILayout.LabelField("手动补充组件描述（可选）：", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "格式：类型 节点名, 类型 节点名, ...\n" +
                "例：UIButton StartBtn, UIText Title, UIInput Username",
                EditorStyles.miniLabel, GUILayout.Height(25));
            _manualComponents = EditorGUILayout.TextArea(_manualComponents, GUILayout.Height(48));

            // ── 生成按钮 ──
            GUILayout.Space(12);
            bool canGen = !string.IsNullOrWhiteSpace(_prefix) && !string.IsNullOrWhiteSpace(_outputPath);
            EditorGUI.BeginDisabledGroup(!canGen);
            if (GUILayout.Button("生成 MVC 脚本", GUILayout.Height(36)))
                Generate();
            EditorGUI.EndDisabledGroup();

            if (string.IsNullOrWhiteSpace(_prefix))
                EditorGUILayout.HelpBox("请输入面板名。", MessageType.Info);

            // 预览
            if (canGen)
            {
                GUILayout.Space(4);
                var allBinds = GetMergedBinds();
                var sb = new StringBuilder();
                sb.AppendLine($"将生成 {_prefix}View.cs / {_prefix}Model.cs / {_prefix}Controller.cs");
                sb.AppendLine($"输出目录: {Path.Combine(_outputPath, _prefix)}");
                if (allBinds.Count > 0)
                {
                    sb.AppendLine($"包含 {allBinds.Count} 个 [UIBind] 绑定:");
                    foreach (var b in allBinds)
                        sb.AppendLine($"  [{b.ComponentType}] {b.Path} → {b.FieldName}");
                }
                EditorGUILayout.HelpBox(sb.ToString(), MessageType.None);
            }

            EditorGUILayout.EndScrollView();
        }

        private void ScanPrefab()
        {
            _bindInfos.Clear();
            if (_prefab == null) return;

            var allComps = _prefab.GetComponentsInChildren<UIComponentBase>(true);
            foreach (var comp in allComps)
            {
                if (comp == null) continue;
                var path = GetRelativePath(_prefab.transform, comp.transform);
                var name = comp.gameObject.name;
                var typeName = comp.GetType().Name;

                _bindInfos.Add(new BindInfo
                {
                    Path = path,
                    FieldName = "_" + char.ToLower(name[0]) + name.Substring(1),
                    ComponentType = typeName,
                    EventHint = GetEventHint(comp)
                });
            }
        }

        private static string GetRelativePath(Transform root, Transform target)
        {
            var parts = new List<string>();
            var t = target;
            while (t != null && t != root)
            {
                parts.Add(t.name);
                t = t.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string GetEventHint(UIComponentBase comp)
        {
            // 不是每种组件都有统一的 OnClick 事件；标记常见类型帮助 Controller 生成模板
            if (comp is UIButton) return "onClick";
            if (comp is UIToggle) return "onValueChanged";
            if (comp is UISlider) return "onValueChanged";
            if (comp is UIInput) return "onEndEdit";
            if (comp is UIDropdown) return "onValueChanged";
            return "";
        }

        private List<BindInfo> GetMergedBinds()
        {
            var merged = new List<BindInfo>(_bindInfos);
            if (!string.IsNullOrWhiteSpace(_manualComponents))
            {
                foreach (var part in _manualComponents.Split(','))
                {
                    var trimmed = part.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;
                    var spaceIdx = trimmed.IndexOf(' ');
                    if (spaceIdx <= 0) continue;
                    var typeName = trimmed.Substring(0, spaceIdx).Trim();
                    var nodeName = trimmed.Substring(spaceIdx + 1).Trim();
                    merged.Add(new BindInfo
                    {
                        Path = nodeName,
                        FieldName = "_" + char.ToLower(nodeName[0]) + nodeName.Substring(1),
                        ComponentType = typeName,
                        EventHint = typeName == "UIButton" ? "onClick" : ""
                    });
                }
            }
            return merged;
        }

        private void Generate()
        {
            var prefix = _prefix.Trim();
            var basePath = Path.Combine(Application.dataPath.Replace("Assets", ""), _outputPath, prefix);

            var viewDir = basePath;
            var modelDir = basePath;
            var ctrlDir = basePath;
            Directory.CreateDirectory(basePath);

            var viewFile = Path.Combine(viewDir, $"{prefix}View.cs");
            var modelFile = Path.Combine(modelDir, $"{prefix}Model.cs");
            var ctrlFile = Path.Combine(ctrlDir, $"{prefix}Controller.cs");

            bool anyExists = File.Exists(viewFile) || File.Exists(modelFile) || File.Exists(ctrlFile);
            if (anyExists)
            {
                if (!EditorUtility.DisplayDialog("文件已存在",
                    $"以下文件已存在，是否覆盖？\n{viewFile}\n{modelFile}\n{ctrlFile}", "覆盖", "取消"))
                    return;
            }

            var binds = GetMergedBinds();
            File.WriteAllText(viewFile, BuildView(prefix, _outputPath, binds));
            File.WriteAllText(modelFile, BuildModel(prefix, _outputPath));
            File.WriteAllText(ctrlFile, BuildController(prefix, _outputPath, binds));

            AssetDatabase.Refresh();
            Debug.Log($"[UGUIMVCGenerator] 生成完成: {prefix}View / {prefix}Model / {prefix}Controller → {_outputPath}/{prefix}/");
            EditorUtility.DisplayDialog("完成",
                $"已生成：\n{prefix}View.cs\n{prefix}Model.cs\n{prefix}Controller.cs\n\n输出目录: {_outputPath}/{prefix}/",
                "确定");
        }

        #region 模板

        private static string BuildView(string prefix, string outputPath, List<BindInfo> binds)
        {
            var ns = outputPath.Replace('/', '.').Replace("Assets.", "").Replace("GameScripts.", "").Trim('.');
            // Hotfix/Logic/UI → Hotfix.Logic.UI
            ns = ns.Replace("Hotfix.", "Hotfix.").Replace("Logic.", "Logic.").Replace("UI.", "UI.");
            // 简化：从 outputPath 推算 namespace
            var ns2 = outputPath.Replace("Assets/GameScripts/", "")
                               .Replace('/', '.')
                               .Replace("..", ".");
            if (string.IsNullOrEmpty(ns2)) ns2 = "Hotfix.Logic.UI";

            var sb = new StringBuilder();
            sb.AppendLine("//------------------------------");
            sb.AppendLine("// ZEngine - UGUI 自动生成");
            sb.AppendLine("//------------------------------");
            sb.AppendLine();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using ZEngine.Manager.UI;");
            sb.AppendLine("using ZEngine.Manager.UI.UGUI;");
            sb.AppendLine("using ZEngine.Manager.UI.UGUI.Components;");
            sb.AppendLine();
            sb.AppendLine($"namespace {ns2}.{prefix}");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// {prefix} 面板 View。由 UGUIMVCGenerator 自动生成，可在 OnComplete 中补充事件绑定。");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    [UIView(\"UI/Prefabs/{prefix}\", UUILayer.Middle_Layer, isSingleton: true)]");
            sb.AppendLine($"    public class {prefix}View : UBaseView");
            sb.AppendLine("    {");

            // [UIBind] 字段
            if (binds.Count > 0)
            {
                sb.AppendLine("        #region 自动绑定字段");
                foreach (var b in binds)
                    sb.AppendLine($"        [UIBind(\"{b.Path}\")] private {b.ComponentType} {b.FieldName};");
                sb.AppendLine("        #endregion");
                sb.AppendLine();
            }

            // Initialize
            sb.AppendLine("        public override void Initialize()");
            sb.AppendLine("        {");
            sb.AppendLine("            base.Initialize();");
            sb.AppendLine($"            ModelType = typeof({prefix}Model);");
            sb.AppendLine($"            ControllerType = typeof({prefix}Controller);");
            sb.AppendLine("        }");
            sb.AppendLine();

            // OnComplete（事件绑定骨架）
            sb.AppendLine("        public override void OnComplete()");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnComplete();");
            if (binds.Count > 0)
            {
                bool hasAny = false;
                foreach (var b in binds)
                {
                    if (b.EventHint == "onClick")
                    {
                        sb.AppendLine($"            if ({b.FieldName} != null) {b.FieldName}.OnClick += On{b.FieldName.Substring(1)}Click;");
                        hasAny = true;
                    }
                }
                if (!hasAny)
                    sb.AppendLine("            // TODO: 在 Controller 中绑定组件事件");
            }
            else
            {
                sb.AppendLine("            // TODO: 在 Controller 中绑定组件事件");
            }
            sb.AppendLine("        }");
            sb.AppendLine();

            // 点击事件 stub（指向 Controller）
            if (binds.Count > 0)
            {
                sb.AppendLine("        #region 事件转发（调用 Controller）");
                foreach (var b in binds)
                {
                    if (b.EventHint == "onClick")
                    {
                        var method = $"On{b.FieldName.Substring(1)}Click";
                        sb.AppendLine($"        private void {method}()");
                        sb.AppendLine("        {");
                        sb.AppendLine($"            // TODO: 在此实现点击逻辑，或由 Controller 调用");
                        sb.AppendLine("        }");
                        sb.AppendLine();
                    }
                }
                sb.AppendLine("        #endregion");
                sb.AppendLine();
            }

            // OnRelease
            sb.AppendLine("        public override void OnRelease()");
            sb.AppendLine("        {");
            if (binds.Count > 0)
            {
                foreach (var b in binds)
                {
                    if (b.EventHint == "onClick")
                        sb.AppendLine($"            if ({b.FieldName} != null) {b.FieldName}.OnClick -= On{b.FieldName.Substring(1)}Click;");
                }
            }
            sb.AppendLine("            base.OnRelease();");
            sb.AppendLine("        }");

            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildModel(string prefix, string outputPath)
        {
            var ns = outputPath.Replace("Assets/GameScripts/", "").Replace('/', '.').Replace("..", ".");
            if (string.IsNullOrEmpty(ns)) ns = "Hotfix.Logic.UI";

            return $@"//------------------------------
// ZEngine - UGUI 自动生成
//------------------------------

using ZEngine.Manager.UI.UGUI;

namespace {ns}.{prefix}
{{
    /// <summary>
    /// {prefix} 数据模型。在此定义面板所需数据字段。
    /// </summary>
    public class {prefix}Model : UBaseModel
    {{
        // TODO: 添加业务数据字段
        // public string Title;
        // public bool IsOn;

        public override void Initialize()
        {{
        }}

        public override void OnRelease()
        {{
        }}
    }}
}}
";
        }

        private static string BuildController(string prefix, string outputPath, List<BindInfo> binds)
        {
            var ns = outputPath.Replace("Assets/GameScripts/", "").Replace('/', '.').Replace("..", ".");
            if (string.IsNullOrEmpty(ns)) ns = "Hotfix.Logic.UI";

            var sb = new StringBuilder();
            sb.AppendLine("//------------------------------");
            sb.AppendLine("// ZEngine - UGUI 自动生成");
            sb.AppendLine("//------------------------------");
            sb.AppendLine();
            sb.AppendLine("using ZEngine.Manager.UI.UGUI;");
            sb.AppendLine();
            sb.AppendLine($"namespace {ns}.{prefix}");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// {prefix} 控制器。在 Initialize 中绑定事件，在业务方法中处理逻辑。");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {prefix}Controller : UBaseController");
            sb.AppendLine("    {");
            sb.AppendLine($"        private new {prefix}Model _model => ({prefix}Model)base._model;");
            sb.AppendLine($"        private new {prefix}View _view => ({prefix}View)base._view;");
            sb.AppendLine();
            sb.AppendLine("        public override void Initialize()");
            sb.AppendLine("        {");
            sb.AppendLine("            base.Initialize();");
            sb.AppendLine("            // TODO: 在此绑定非按钮组件事件（UIToggle/UISlider/UIInput 等）");
            if (binds.Count > 0)
            {
                foreach (var b in binds)
                {
                    if (b.EventHint == "onValueChanged")
                        sb.AppendLine($"            // if (_view.{b.FieldName} != null) _view.{b.FieldName}.OnValueChanged += On{b.FieldName.Substring(1)}Changed;");
                    else if (b.EventHint == "onEndEdit")
                        sb.AppendLine($"            // if (_view.{b.FieldName} != null) _view.{b.FieldName}.OnEndEdit += On{b.FieldName.Substring(1)}EndEdit;");
                }
            }
            sb.AppendLine("        }");
            sb.AppendLine();

            // 业务方法 stub
            if (binds.Count > 0)
            {
                sb.AppendLine("        #region 业务方法");
                foreach (var b in binds)
                {
                    if (b.EventHint == "onClick")
                    {
                        sb.AppendLine($"        public void On{b.FieldName.Substring(1)}Click()");
                        sb.AppendLine("        {");
                        sb.AppendLine("            // TODO: 实现点击逻辑");
                        sb.AppendLine("        }");
                        sb.AppendLine();
                    }
                    else if (b.EventHint == "onValueChanged")
                    {
                        var typeStr = b.ComponentType == "UIToggle" ? "bool" :
                                      b.ComponentType == "UISlider" ? "float" :
                                      b.ComponentType == "UIDropdown" ? "int" : "object";
                        sb.AppendLine($"        public void On{b.FieldName.Substring(1)}Changed({typeStr} value)");
                        sb.AppendLine("        {");
                        sb.AppendLine("            // TODO: 实现值变化逻辑");
                        sb.AppendLine("        }");
                        sb.AppendLine();
                    }
                }
                sb.AppendLine("        #endregion");
                sb.AppendLine();
            }

            sb.AppendLine("        public override void OnUpdate()");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnUpdate();");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public override void OnRelease()");
            sb.AppendLine("        {");
            sb.AppendLine("            // TODO: 解除事件绑定");
            sb.AppendLine("            base.OnRelease();");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        #endregion
    }
}
