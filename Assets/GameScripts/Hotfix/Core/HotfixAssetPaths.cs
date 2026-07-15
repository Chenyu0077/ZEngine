public static class HotfixAssetPaths
{
    // 音频
    public const string MusicPath  = "Audios/Music/";
    public const string VoicePath  = "Audios/Voice/";

    // 场景
    public const string ScenePath  = "Scenes/";

    // 纹理 / 贴图
    public const string TexturePath = "Textures/";
    public const string SpritePath  = "Sprites/";

    // Prefab
    public const string PrefabPath          = "Prefabs/";
    public const string PrefabGridMapPath   = "Prefabs/GridMap/";
    public const string PrefabTowerPath     = "Prefabs/Tower/";
    public const string PrefabEnemyPath     = "Prefabs/Enemy/";

    // 配置文件
    public const string ConfigPath  = "Configs/";
    public const string Config_Json = "Configs/Json/";
    public const string Config_Map  = "Configs/Maps/";

    // 材质
    public const string MaterialPath = "Materials/";

    // ScriptableObject
    public const string SOPath       = "SO/";
    public const string SOCameraPath = "SO/Camera/";
    public const string SOTileSetPath = "SO/TileSets/";
    public const string SOBuffPath    = "SO/Buff/";
    public const string SOTowerDBPath = "SO/TowerDB/";
    public const string SOEnemyDBPath  = "SO/EnemyDB/";

    // 实体配置（TowerConfig / EnemyConfig 资产路径）
    public const string SOTowerConfig = SOTowerDBPath + "TowerConfig";
    public const string SOEnemyConfig   = SOEnemyDBPath + "EnemyConfig";

    // TileSetData 资产列表（YooAsset 无目录扫描，新增 TileSet 时在此追加路径）
    public static readonly string[] TileSetAssets = new[]
    {
        SOTileSetPath + "TileSet",
    };


    // UI（FairyGUI 包体）
    public const string FGUIPath = "FGUI/";

    // 着色器
    public const string ShaderPath = "Shaders/";

    // 动画
    public const string AnimationPath = "Animation/";

    // 字体
    public const string FontPath = "Fonts/";
}
