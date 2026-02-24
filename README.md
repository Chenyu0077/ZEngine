# ZEngine

一个Unity客户端框架 | A Unity Client-Side Framework

## 简介 | Overview

ZEngine 是一个轻量级、模块化的 Unity 客户端框架，提供游戏开发中常用的核心系统。

ZEngine is a lightweight, modular Unity client framework providing common core systems for game development.

## 功能模块 | Modules

| 模块 | 描述 |
|------|------|
| **Core** | 单例基类、模块接口、框架根节点 |
| **Event** | 事件管理系统，支持订阅/取消/派发 |
| **Resource** | 资源管理，支持 Resources 和 AssetBundle |
| **UI** | UI 面板管理，支持分层显示 |
| **Audio** | 背景音乐和音效管理，支持淡入淡出 |
| **Pool** | 对象池，支持普通对象和 GameObject |
| **Timer** | 定时器系统，支持延迟和循环回调 |
| **Scene** | 场景管理，支持异步加载和进度回调 |
| **Config** | 配置表管理，基于 ScriptableObject |

## 安装 | Installation

### Unity Package Manager (推荐)

在 Unity 中打开 Package Manager，选择 **Add package from git URL**，输入：

```
https://github.com/Chenyu0077/ZEngine.git?path=Assets/ZEngine
```

### 手动安装

将 `Assets/ZEngine` 目录复制到您的 Unity 项目的 `Assets` 文件夹下。

## 快速开始 | Quick Start

### 1. 创建框架根节点

通过菜单 **ZEngine → Create GameRoot** 在场景中创建根节点，或者手动添加：

```csharp
// 在场景启动时自动创建（单例模式）
var gameRoot = ZEngine.Core.GameRoot.Instance;
```

### 2. 事件系统

```csharp
using ZEngine.Event;

// 定义事件数据
public struct PlayerDiedEvent : IEventData
{
    public string PlayerName;
}

// 订阅事件
EventManager.Instance.Subscribe<PlayerDiedEvent>(EventIds.GameEventStart + 1, OnPlayerDied);

// 派发事件
EventManager.Instance.Dispatch(EventIds.GameEventStart + 1, new PlayerDiedEvent { PlayerName = "Hero" });

// 取消订阅
EventManager.Instance.Unsubscribe<PlayerDiedEvent>(EventIds.GameEventStart + 1, OnPlayerDied);
```

### 3. 资源管理

```csharp
using ZEngine.Resource;

// 同步加载（从 Resources 文件夹）
var sprite = ResourceManager.Instance.Load<Sprite>("UI/Icons/sword");

// 异步加载
ResourceManager.Instance.LoadAsync<AudioClip>("Audio/bgm_main", clip => {
    AudioManager.Instance.PlayMusic(clip);
});

// 释放资源
ResourceManager.Instance.Unload("UI/Icons/sword");
```

### 4. UI 管理

```csharp
using ZEngine.UI;

// 定义面板
public class MainMenuPanel : UIPanel
{
    protected override void OnOpen(object data) { /* 初始化UI */ }
    protected override void OnClose() { /* 清理 */ }
}

// 打开面板（自动从 Resources 加载预制体）
UIManager.Instance.OpenPanel<MainMenuPanel>("UI/Panels/MainMenuPanel", UILayer.Normal);

// 关闭面板
UIManager.Instance.ClosePanel<MainMenuPanel>();
```

### 5. 音频管理

```csharp
using ZEngine.Audio;

// 播放背景音乐（带淡入效果）
AudioManager.Instance.PlayMusic("Audio/Music/main_theme", loop: true, fadeInDuration: 1.0f);

// 播放音效
AudioManager.Instance.PlaySound("Audio/SFX/button_click");

// 调整音量
AudioManager.Instance.MusicVolume = 0.8f;
AudioManager.Instance.SoundVolume = 1.0f;
```

### 6. 对象池

```csharp
using ZEngine.Pool;

// GameObject 对象池
var bulletPool = new GameObjectPool(bulletPrefab, poolContainer, initialSize: 20);

// 获取对象
var bullet = bulletPool.Get(spawnPosition, Quaternion.identity);

// 回收对象
bulletPool.Release(bullet);

// 通用对象池
var msgPool = new ObjectPool<NetworkMessage>(
    createFunc: () => new NetworkMessage(),
    onGet: msg => msg.Reset(),
    onRelease: msg => msg.Clear()
);
```

### 7. 定时器

```csharp
using ZEngine.Timer;

// 延迟执行
TimerManager.Instance.Delay(2.0f, () => Debug.Log("2秒后执行"));

// 重复执行
var timer = TimerManager.Instance.Repeat(0.5f, () => Debug.Log("每0.5秒执行"));

// 暂停/恢复/取消
timer.Pause();
timer.Resume();
timer.Cancel();
```

### 8. 场景管理

```csharp
using ZEngine.Scene;

// 异步加载场景（带进度回调）
SceneManager.Instance.LoadScene(
    "GameScene",
    UnityEngine.SceneManagement.LoadSceneMode.Single,
    onProgress: progress => loadingBar.fillAmount = progress,
    onComplete: () => Debug.Log("场景加载完成")
);
```

## 项目结构 | Project Structure

```
Assets/ZEngine/
├── Runtime/
│   ├── Core/
│   │   ├── Singleton.cs        # 非 MonoBehaviour 单例基类
│   │   ├── MonoSingleton.cs    # MonoBehaviour 单例基类
│   │   ├── GameRoot.cs         # 框架根节点
│   │   └── IModule.cs          # 模块接口
│   ├── Event/
│   │   ├── EventManager.cs     # 事件管理器
│   │   ├── IEventData.cs       # 事件数据接口
│   │   └── EventIds.cs         # 内置事件 ID 常量
│   ├── Resource/
│   │   └── ResourceManager.cs  # 资源管理器
│   ├── UI/
│   │   ├── UIManager.cs        # UI 管理器
│   │   ├── UIPanel.cs          # UI 面板基类
│   │   └── UILayer.cs          # UI 层级枚举
│   ├── Audio/
│   │   └── AudioManager.cs     # 音频管理器
│   ├── Pool/
│   │   ├── ObjectPool.cs       # 通用对象池
│   │   └── GameObjectPool.cs   # GameObject 对象池
│   ├── Timer/
│   │   ├── TimerManager.cs     # 定时器管理器
│   │   └── Timer.cs            # 定时器
│   ├── Scene/
│   │   └── SceneManager.cs     # 场景管理器
│   ├── Config/
│   │   ├── ConfigManager.cs    # 配置管理器
│   │   └── ConfigTable.cs      # 配置表基类
│   └── ZEngine.Runtime.asmdef
├── Editor/
│   ├── ZEngineEditor.cs        # 编辑器工具菜单
│   └── ZEngine.Editor.asmdef
└── package.json
```

## 兼容性 | Compatibility

- Unity 2021.3 LTS 及以上版本
- .NET Standard 2.1

## 许可证 | License

MIT License
