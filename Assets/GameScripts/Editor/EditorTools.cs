using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace GameScripts.Editor
{
    public class EditorTools
    {

        // ── 热更新开关 ───────────────────────────────────────────────────────

        private const string HotUpdateSymbol = "ENABLE_HOT_UPDATE";
        private const string MenuEnableHotUpdate  = "ZEngineTools/热更新/启用热更新 (ENABLE_HOT_UPDATE)";
        private const string MenuDisableHotUpdate = "ZEngineTools/热更新/禁用热更新 (ENABLE_HOT_UPDATE)";

        [MenuItem(MenuEnableHotUpdate)]
        private static void EnableHotUpdate()  => SetDefineSymbol(HotUpdateSymbol, true);

        [MenuItem(MenuDisableHotUpdate)]
        private static void DisableHotUpdate() => SetDefineSymbol(HotUpdateSymbol, false);

        // 只有未启用时，"启用"菜单项才可点击
        [MenuItem(MenuEnableHotUpdate, true)]
        private static bool EnableHotUpdateValidate()  => !HasDefineSymbol(HotUpdateSymbol);

        // 只有已启用时，"禁用"菜单项才可点击
        [MenuItem(MenuDisableHotUpdate, true)]
        private static bool DisableHotUpdateValidate() =>  HasDefineSymbol(HotUpdateSymbol);

        private static bool HasDefineSymbol(string symbol)
        {
            var target  = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            foreach (var d in defines.Split(';'))
                if (d.Trim() == symbol) return true;
            return false;
        }

        private static void SetDefineSymbol(string symbol, bool add)
        {
            var target  = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var raw     = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            var set     = new HashSet<string>(raw.Split(';', System.StringSplitOptions.RemoveEmptyEntries));

            bool changed = add ? set.Add(symbol) : set.Remove(symbol);
            if (!changed) return;

            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, string.Join(";", set));
            string state = add ? "已启用" : "已禁用";
            Debug.Log($"[EditorTools] 热更新 {state}（{symbol}）。重新编译后生效。");
        }

        // ── 打包后清理调试产物 ───────────────────────────────────────────────

        [UnityEditor.Callbacks.PostProcessBuild(100)]
        public static void CleanBuildDebugArtifacts(BuildTarget _, string buildPath)
        {
            string buildDir = Path.GetDirectoryName(buildPath);

            // 递归查找并删除所有 il2cppOutput 目录
            string[] outputFiles = Directory.GetDirectories(buildDir, "il2cppOutput", SearchOption.AllDirectories);
            foreach (string il2cppOutputDir in outputFiles)
            {
                Directory.Delete(il2cppOutputDir, true);
                Debug.Log($"[EditorTools] 已删除 il2cppOutput：{il2cppOutputDir}");
            }

            // 删除所有 .pdb 调试符号文件（GameAssembly.pdb 约 624MB）
            string[] pdbFiles = Directory.GetFiles(buildDir, "*.pdb", SearchOption.AllDirectories);
            foreach (string pdb in pdbFiles)
            {
                File.Delete(pdb);
                Debug.Log($"[EditorTools] 已删除 pdb：{pdb}");
            }

            Debug.Log("[EditorTools] 打包后清理完成。");
        }
    }
}
