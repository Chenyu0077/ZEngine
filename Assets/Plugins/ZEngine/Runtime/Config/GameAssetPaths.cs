//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZEngine.Config
{
    public static class GameAssetPaths
    {
        // 资源根路径
        public const string AssetRoot = "Assets/GameAssets/";

        // UI资源路径
        public const string FGUIPath = "FGUI/";

        // 配置文件路径（ScriptableObject、JSON、Excel生成的资产等）
        public const string Config_UI = "Configs/UI/UIConfig.json";

        // 热更新资源路径（配合 YooAsset 使用时可以统一管理地址）
        public const string Hotfix = "HotfixDlls/";
    }
}
