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
        // 预制体路径
        public const string PrefabPlayer = "Prefabs/Characters/";

        // 音频路径
        public const string MusicPath = "Audios/Music/";
        public const string VoicePath = "Audios/Voice/";

        //场景路径
        public const string ScenePath = "Scenes/";

        // UI资源路径
        public const string UIPath = "UI/";
        public const string UGUIPath = "UI/UGUI/";

        // 配置文件路径（ScriptableObject、JSON、Excel生成的资产等）
        public const string ConfigPath = "Configs/";
        public const string Config_Buff = "Configs/Buff/BuffConfig";
        public const string Config_Skill = "Configs/Skill/SkillConfig";
        public const string Config_UI = "Configs/UI/UIConfig.json";
        public const string Config_Json = "Configs/Json/";

        // 热更新资源路径（配合 YooAsset 使用时可以统一管理地址）
        public const string Hotfix = "HotfixDlls/";

        // 资源根路径
        public const string AssetRoot = "Assets/GameAssets/";
    }
}
