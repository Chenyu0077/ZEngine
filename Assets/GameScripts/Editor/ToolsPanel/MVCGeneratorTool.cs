using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameScripts.Editor
{
    /// <summary>
    /// 反射生成 FGUI MVC 模板脚本工具
    /// </summary>
    public class MVCGeneratorTool
    {
        private string _prefix = "";
        private DefaultAsset _outputFolder;
        private string _outputFolderPath = "Assets/AutoGenerate";

        public void OnGUI()
        {
            EditorGUILayout.LabelField("生成 FGUI MVC 模板脚本", EditorStyles.boldLabel);
            GUILayout.Space(4);

            _prefix = EditorGUILayout.TextField("前缀名（如 Settings）", _prefix);

            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("输出目录", GUILayout.Width(60));
            _outputFolder = (DefaultAsset)EditorGUILayout.ObjectField(_outputFolder, typeof(DefaultAsset), false);
            EditorGUILayout.EndHorizontal();

            if (_outputFolder != null)
                _outputFolderPath = AssetDatabase.GetAssetPath(_outputFolder);

            EditorGUILayout.LabelField(_outputFolderPath, EditorStyles.miniLabel);

            GUILayout.Space(4);

            // 预览将生成的文件名
            if (!string.IsNullOrWhiteSpace(_prefix))
            {
                EditorGUILayout.HelpBox(
                    $"将生成：\n" +
                    $"  View/       {_prefix}View.cs\n" +
                    $"  Model/      {_prefix}Model.cs\n" +
                    $"  Controller/ {_prefix}Controller.cs",
                    MessageType.None);
            }

            GUILayout.Space(8);

            bool canGenerate = !string.IsNullOrWhiteSpace(_prefix) && !string.IsNullOrWhiteSpace(_outputFolderPath);
            EditorGUI.BeginDisabledGroup(!canGenerate);
            if (GUILayout.Button("生成 MVC 脚本", GUILayout.Height(30)))
                Generate();
            EditorGUI.EndDisabledGroup();

            if (string.IsNullOrWhiteSpace(_prefix))
                EditorGUILayout.HelpBox("请输入前缀名。", MessageType.Info);
        }

        private void Generate()
        {
            string prefix = _prefix.Trim();
            string basePath = Path.Combine(Application.dataPath.Replace("Assets", ""), _outputFolderPath);

            string viewDir  = Path.Combine(basePath, "View");
            string modelDir = Path.Combine(basePath, "Model");
            string ctrlDir  = Path.Combine(basePath, "Controller");

            Directory.CreateDirectory(viewDir);
            Directory.CreateDirectory(modelDir);
            Directory.CreateDirectory(ctrlDir);

            string viewFile  = Path.Combine(viewDir,  $"{prefix}View.cs");
            string modelFile = Path.Combine(modelDir, $"{prefix}Model.cs");
            string ctrlFile  = Path.Combine(ctrlDir,  $"{prefix}Controller.cs");

            bool anyExists = File.Exists(viewFile) || File.Exists(modelFile) || File.Exists(ctrlFile);
            if (anyExists)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "文件已存在",
                    $"以下文件已存在，是否覆盖？\n{viewFile}\n{modelFile}\n{ctrlFile}",
                    "覆盖", "取消");
                if (!overwrite) return;
            }

            File.WriteAllText(viewFile,  BuildView(prefix));
            File.WriteAllText(modelFile, BuildModel(prefix));
            File.WriteAllText(ctrlFile,  BuildController(prefix));

            AssetDatabase.Refresh();

            Debug.Log($"[MVCGeneratorTool] 生成完成：{prefix}View / {prefix}Model / {prefix}Controller");
            EditorUtility.DisplayDialog("完成", $"已生成：\n{prefix}View.cs\n{prefix}Model.cs\n{prefix}Controller.cs", "确定");
        }

        // ── 模板 ────────────────────────────────────────────────────────────────

        private static string BuildView(string prefix) => $@"//------------------------------
// ZEngine
// 作者:
//------------------------------

using FairyGUI;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{{
    public class {prefix}View : BaseView
    {{
        public override EUILayer LayerType => EUILayer.Top_Layer;
        public override bool IsSingleton => true;

        public override void Initialize()
        {{
            base.Initialize();
            _pkgName = ""{prefix}"";
            _resName = ""{prefix}View"";
            ModelType = typeof({prefix}Model);
            ControllerType = typeof({prefix}Controller);
        }}

        public override void OnComplete()
        {{
            base.OnComplete();
            _view.AddRelation(GRoot.inst, RelationType.Center_Center);
            SetToCenter();
        }}
    }}
}}
";

        private static string BuildModel(string prefix) => $@"//------------------------------
// ZEngine
// 作者:
//------------------------------

using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{{
    public class {prefix}Model : BaseModel
    {{
        public override void Initialize()
        {{
        }}

        public override void OnRelease()
        {{
        }}
    }}
}}
";

        private static string BuildController(string prefix) => $@"//------------------------------
// ZEngine
// 作者:
//------------------------------

using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{{
    public class {prefix}Controller : BaseController
    {{
        public override void Initialize()
        {{
            base.Initialize();
        }}

        public override void OnRelease()
        {{
            base.OnRelease();
        }}
    }}
}}
";
    }
}
