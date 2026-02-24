//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using FairyGUI;
using LitJson;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using ZEngine.Utility;
using System.Diagnostics;

namespace ZEngine.Editor
{
    public class ZEngineTools
    {
        protected const string toolPath = "ZEngineTools";

        [MenuItem(toolPath + "/生成语言包", false, 10)]
        public static void LanguageExport()
        {
            string batPath = $"{UnityEngine.Application.dataPath}/Doc/DataTables/gen.bat";
            UnityEngine.Debug.Log(batPath);
            RunBatFile(batPath);
        }


        [MenuItem(toolPath + "/生成FGUI包依赖表", false, 20)]
        public static void UISettings()
        {
            GenerateUIConfig();
        }


        #region 生成语言包
        public static void RunBatFile(string batFilePath)
        {
            // 设置进程启动信息
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "cmd.exe";
            psi.Arguments = $"/c \"{batFilePath}\""; // /c 表示执行完退出
            psi.WorkingDirectory = Path.GetDirectoryName(batFilePath);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = false; // 是否显示命令行窗口
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;

            using (Process process = new Process())
            {
                process.StartInfo = psi;
                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data)) UnityEngine.Debug.Log(args.Data);
                };
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data)) UnityEngine.Debug.LogError(args.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
        }
        #endregion

        #region 生成UI包依赖表
        //资源路径
        private static string _assetPath = "/GameAssets/UI";
        //配置文件路径
        private static string _configPath = "Assets/GameAssets/Configs/UI/UIConfig.json";

        /// <summary>
        /// FairyGUI UI包之间的引用关系
        /// </summary>
        public static void GenerateUIConfig()
        {
            Dictionary<string, List<string>> dependencies = new Dictionary<string, List<string>>();
            Dictionary<string, UIPackage> packages = new Dictionary<string, UIPackage>();
            string[] packageFiles = FileUtility.GetFilesName(UnityEngine.Application.dataPath + _assetPath, "*_fui.bytes");
            //加载全部包
            foreach (var item in packageFiles)
            {
                string pathName = Path.GetFileNameWithoutExtension(item);
                string pkgName = "Assets/GameAssets/UI/" + pathName.Substring(0, pathName.Length - 4);
                UIPackage pkg = UIPackage.AddPackage(pkgName);
                packages.Add(pathName.Substring(0, pathName.Length - 4), pkg);
            }
            //获取依赖
            foreach (var key in packages.Keys)
            {
                List<string> pkgs = new List<string>();
                foreach (var item in packages[key].dependencies)
                {
                    pkgs.Add(item["name"]);
                }
                dependencies.Add(key, pkgs);
            }
            //转为JSON字符串并存储到文件中
            string json = JsonMapper.ToJson(dependencies);
            FileUtility.CreateFile(_configPath, json);
            if (FileUtility.ReadFile(_configPath) != null)
            {
                UnityEngine.Debug.Log("UI配置文件创建成功！");
            }
        }
        #endregion
    }
}
