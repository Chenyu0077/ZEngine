你正在开发 Unity 2022.3 LTS 项目《AI 小镇模拟器》，是2D游戏，风格是星露谷物语以及Rimworld。请先阅读：
1. Assets/Docs/ZEngine_API.md
2. Assets/Docs/plan.md

## 一、项目主目录

下面是项目的主要目录结构，不可直接删除和添加其主目录结构，只可添加、删除和修改其下的子目录文件夹和文件

```
Assets/
├── Datas/                    # 静态数据资源
│   ├── DataTables/           # 表格数据（Excel 导出）
│   ├── Json/                 # JSON 配置文件
│   └── ScriptableObject/     # ScriptableObject 资产
├── Docs/                     # 项目文档（设计文档、API 文档、修复记录）
├── GameAssets/               # 游戏美术与运行时资源
│   ├── Audios/               # 音频文件
│   ├── Prefabs/              # 预制体
│   ├── Scenes/               # 场景文件
│   ├── Sprites/ Textures/    # 图片资源
│   ├── FGUI/                 # FGUI 导出的图集与二进制文件等
│   ├── UGUI/                 # UGUI 导出的图集等
│   └── ...
├── GameScripts/              # 游戏逻辑代码
│   ├── Editor/               # 编辑器专属脚本（不参与打包）
│   │   └── Map/
│   ├── Hotfix/               # 热更新程序集 HotUpdate.asmdef（HybridCLR）
│   │   ├── Core/             # 热更新启动逻辑
│   │   ├── FuncModule/       # 功能模块（AI Agent、对话等）
│   │   ├── Main/             # 热更新入口
│   │   ├── Tables/           # 热更新数据表
│   │   └── UI/               # FGUI外置编辑器导出的自动生成的代码
│   └── Main/
│       └── Runtime/          # 主程序集 MainScript.asmdef（宿主，不热更）
├── Plugins/                  # 第三方插件（只读，不得修改插件本体）
│   ├── ZEngine/Runtime/      # ZEngine 框架（Manager 生命周期由此驱动）
│   ├── UniTask/              # 异步工具（Cysharp.Threading.Tasks）
│   ├── FairyGUI/             # FGUI（本项目 UI 选用 UGUI，此目录暂不使用）
│   ├── MessagePack/          # 存档序列化
│   ├── InstalledPackages/    # Protobuf / LiteNetLib / WebSocket / Newtonsoft.Json
│   ├── BqLog/                # 持久化日志
│   └── ...
├── HybridCLRGenerate/        # HybridCLR 自动生成文件（不得手动修改）
├── Resources/                # 不走 YooAsset 的少量内置资源
├── Settings/                 # Unity 工程设置（渲染管线等）
├── StreamingAssets/yoo/      # YooAsset 本地包体缓存
└── Test/                     # （不参与打本地测试场景包）
    └── Scenes/
```

## 二、程序集边界

| 程序集 | 文件 | 说明 |
|-------|------|------|
| `MainScript` | `GameScripts/Main/Runtime/` | 宿主，启动 ZEngine，**不可热更** |
| `HotUpdate` | `GameScripts/Hotfix/` | 热更新逻辑，依赖 HybridCLR，**业务代码写在这里** |
| `GameScripts.Editor` | `GameScripts/Editor/` | 编辑器工具，**不打包** |

**热更规则**：热更新程序集不得引用任何 `Main/Runtime/` 下的类型；反向引用（Main 引用 Hotfix）同样禁止。跨程序集通信通过 `EventManager` 事件或接口完成。

## 三、核心框架速查

**ZEngine 统一生命周期**（入口：`GameLauncher.cs`）
```
ZEngineMain.Initialize(this)
ZEngineMain.CreateManager<LogManager>()          // 优先级 100，最先创建
ZEngineMain.CreateManager<ResourceManager>(...)  // 优先级 90，依赖 LogManager
ZEngineMain.CreateManager<ObjectPoolManager>()   // 优先级 80
ZEngineMain.CreateManager<EventManager>()        // 优先级 70
ZEngineMain.CreateManager<TimerManager>()        // 优先级 60
... 其他 Manager 按依赖顺序添加
```

**常用 Manager 访问**
```csharp
LogManager.Instance.Info("...");
EventManager.Instance.SendMessage(MyEvent.Create(...));
TimerManager.Instance.CreateOnceTimer(callback, delay: 1f);
ResourceManager.Instance.LoadAssetAsync<GameObject>("path");
ObjectPoolManager.Instance.Spawn("prefab/path");
UUIManager.Instance.OpenViewAsync<MyView>();
```

**资源路径**：所有路径以 `GameAssetPaths`（`ZEngine.Config`）中定义的前缀为基准，热更新中的高频路径写在 `HotfixAssetPaths`（`ZEngine.Config.Hotfix`）中。

**详细 API 见**：`Assets/Docs/ZEngine_API.md`

## 四、约束

强制要求：
- 使用 Unity 2022.3 LTS + C#，**禁止**使用 Godot / 其他引擎语法。
- UI 统一使用 **UGUI**（`UUIManager`）使用 FairyGUI。
- 异步操作统一使用 **UniTask**，禁止裸 `async/await Task`。
- 大量对象复用必须走 **ReferencePool** 或 **ObjectPoolManager**，禁止在热路径中直接 `new`。
- 修改 `ZEngine/Runtime/` 下的任何文件，必须同步更新 `Assets/Docs/fixed.md` 和 `Assets/Docs/ZEngine_API.md`。
- 先保证可运行，再逐步增强表现；每次提交说明改了哪些文件、实现了什么、如何验证。
- `HybridCLRGenerate/` 目录由工具链自动维护，**禁止手动修改**。
- 发现设计或实现不明确时，先在 `Assets/Docs/open-questions.md` 中追加问题（只可追加，不可删除原有问题）。
- 不要用 // ── xxx ──────────────────────────────────────────────────────── 去隔开代码块，可用 #region xxx #endregion 来隔开代码块，方便在 IDE 中折叠。
