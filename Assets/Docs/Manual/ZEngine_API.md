# ZEngine API 文档

> 版本：基于 2026-05-29 代码版本  
> 适用：开发者参考 / AI 代码生成查询  
> 项目：Unity 2022.3 LTS + HybridCLR + YooAsset  
> 变更：[2026-05-14] UGUI 章节全面重写——引入 UIViewAttribute，修复 Canvas/对象池/单例检查 Bug，新增全屏遮挡、CanRemoved  
> 变更：[2026-05-14] UBaseView 新增子节点缓存——`BuildChildCache()` 在 Initialize() 前自动遍历完整层级，`GetChild<T>()` / `GetChildGO()` 按相对路径访问缓存  
> 变更：[2026-05-19] UGUI 修复——EventSystem 自动创建、BringToFront 层级修正（List 替换 Dictionary）、CanRemoved 复用重置  
> 变更：[2026-05-19] UGUI API 重构——`OpenViewAsync<T>` 改为真正的 `async UniTask<T>`，移除冗余的 `OpenViewAsyncAwait`  
> 变更：[2026-05-19] AudioManager——新增位置音效 `SetSoundListener` / `PlaySoundAtPosition`（XY 距离线性衰减，适配 2D 正交相机）  
> 变更：[2026-05-29] 网络模块——修正 Send/RegisterHandler/UnregisterHandler 签名；修正 NetworkState / DisconnectReason 枚举值；新增 WebSocketChannel 完整属性表与使用示例；新增 NetworkChannelType 枚举说明
> 变更：[2026-06-01] WebSocketChannel——新增文本（JSON）消息支持：`SendText` / `RegisterTextHandler` / `UnregisterTextHandler`；文本帧与二进制帧共享同一连接，主线程安全分发

---

## 目录

1. [架构概述](#架构概述)
2. [快速启动](#快速启动)
3. [核心 — ZEngineMain](#核心--zenglinemain)
4. [引用池 — ReferencePool](#引用池--referencepool)
5. [事件系统 — EventManager](#事件系统--eventmanager)
6. [日志系统 — LogManager](#日志系统--logmanager)
7. [计时器 — TimerManager](#计时器--timermanager)
8. [资源管理 — ResourceManager](#资源管理--resourcemanager)
9. [对象池 — ObjectPoolManager](#对象池--objectpoolmanager)
10. [网络 — NetworkManager](#网络--networkmanager)
11. [HTTP — HttpManager](#http--httpmanager)
12. [音频 — AudioManager](#音频--audiomanager)
13. [UI（UGUI）— UUIManager](#uiugui--uuimanager)
14. [UI（FGUI）— UIManager](#uifgui--uimanager)
15. [有限状态机 — FiniteStateMachine](#有限状态机--finitestatemachine)
16. [过程状态机 — ProcedureFsm](#过程状态机--procedurefsm)
17. [输入系统 — InputManager](#输入系统--inputmanager)
18. [2D 碰撞 — ColliderManager](#2d-碰撞--collidermanager)
19. [本地化 — LocalizationManager](#本地化--localizationmanager)
20. [存档 — ArchiveManager](#存档--archivemanager)
21. [工具类](#工具类)

---

## 架构概述

ZEngine 采用 **Manager 单例 + 统一生命周期** 的架构。

```
MonoBehaviour (GameLauncher)
    └── ZEngineMain.Initialize()       ← 注入驱动 MonoBehaviour
    └── ZEngineMain.CreateManager<T>() ← 按优先级注册各 Manager
    └── ZEngineMain.Update()           ← 每帧驱动所有 Manager
    └── ZEngineMain.Destroy()          ← 销毁所有 Manager
```

**核心设计原则**
- 所有 Manager 继承 `ManagerSingleton<T>`，通过 `T.Instance` 单例访问
- Manager 创建顺序：`LogManager` → `ResourceManager` → `ObjectPoolManager` → 业务 Manager
- 事件跨 Manager 通信通过 `EventManager` + `IEventMessage` 实现
- 对象复用通过 `ReferencePool` 实现，实现 `IReference` 接口的类均可入池

**命名空间速查**

| 模块 | 命名空间 |
|------|---------|
| 核心 | `ZEngine.Core` |
| 引用池 | `ZEngine.Reference` |
| 事件 | `ZEngine.Manager.Event` |
| 日志 | `ZEngine.Manager.Log` |
| 计时器 | `ZEngine.Manager.Timer` |
| 资源 | `ZEngine.Manager.Resource` |
| 对象池 | `ZEngine.Manager.Pool` |
| 网络 | `ZEngine.Manager.Network` |
| HTTP | `ZEngine.Manager.Http` |
| 音频 | `ZEngine.Manager.Audio` |
| UGUI | `ZEngine.Manager.UI.UGUI` |
| FGUI | `ZEngine.Manager.UI` |
| 状态机 | `ZEngine.AI.FSM` |
| 输入 | `ZEngine.Module.Input` |
| 碰撞 | `ZEngine.Module.Collider2D` |
| 本地化 | `ZEngine.Manager.Localization` |
| 存档 | `ZEngine.Module.Archive` |
| 配置路径 | `ZEngine.Config` |

---

## 快速启动

```csharp
// GameLauncher.cs（挂载到场景 GameObject 上）
using ZEngine.Core;
using ZEngine.Manager.Log;
using ZEngine.Manager.Resource;
using ZEngine.Manager.Pool;
using ZEngine.Manager.Event;
using ZEngine.Manager.Timer;
using YooAsset;

public class GameLauncher : MonoBehaviour
{
    private void Awake()
    {
        // 1. 初始化 ZEngine，注入驱动 MonoBehaviour
        ZEngineMain.Initialize(this);

        // 2. 按依赖顺序创建 Manager（优先级越大越早 Update）
        ZEngineMain.CreateManager<LogManager>(priority: 100);

        ZEngineMain.CreateManager<ResourceManager>(
            new InitializeParameters(), // YooAsset 初始化参数
            priority: 90);

        ZEngineMain.CreateManager<ObjectPoolManager>(
            new ObjectPoolManager.CreateParameters
            {
                DefaultMaxCapacity = 200,
                DefaultDestroyTime = 60f
            }, priority: 80);

        ZEngineMain.CreateManager<EventManager>(priority: 70);
        ZEngineMain.CreateManager<TimerManager>(priority: 60);
        ZEngineMain.CreateManager<NetworkManager>(priority: 50);
    }

    private void Update()   => ZEngineMain.Update();
    private void OnGUI()    => ZEngineMain.DrawGUI();
    private void OnDestroy()=> ZEngineMain.Destroy();
}
```

---

## 核心 — ZEngineMain

**命名空间**：`ZEngine.Core`  
**类型**：静态类

### 方法

| 方法 | 说明 |
|------|------|
| `Initialize(MonoBehaviour behaviour)` | 初始化引擎，注入驱动 MonoBehaviour，必须最先调用 |
| `CreateManager<T>(int priority = 0)` | 创建无参 Manager |
| `CreateManager<T>(object param, int priority = 0)` | 创建有参 Manager |
| `Update()` | 驱动所有 Manager 的 OnUpdate，放在 MonoBehaviour.Update 中调用 |
| `DrawGUI()` | 驱动所有 Manager 的 OnGUI |
| `Destroy()` | 销毁所有 Manager，放在 MonoBehaviour.OnDestroy 中调用 |
| `Contains<T>()` | 查询 Manager 是否已创建 |
| `Contains(Type type)` | 按 Type 查询 Manager 是否已创建 |
| `StartCoroutine(IEnumerator)` | 在驱动 MonoBehaviour 上开启协程 |
| `StopCoroutine(Coroutine)` | 停止协程 |
| `StopAllCoroutines()` | 停止全部协程 |
| `RegisterDestroyAction(Action)` | 注册引擎销毁时的回调 |

**注意**
- `priority` 越大，`OnUpdate` 越早执行（降序排列）
- 同一类型的 Manager 只能创建一次，重复创建抛 `Exception`
- Manager 未创建时调用 `Instance` 会抛 `InvalidOperationException`

---

## 引用池 — ReferencePool

**命名空间**：`ZEngine.Reference`  
**类型**：静态类

对象复用池，避免 GC。任何实现 `IReference` 的类均可入池。

### IReference 接口

```csharp
public interface IReference
{
    void OnRelease(); // 归还到池时调用，用于重置状态
}
```

### ReferencePool 方法

| 方法 | 说明 |
|------|------|
| `Spawn<T>()` | 从池中取出一个 T 实例，不存在则创建 |
| `Spawn(Type type)` | 按 Type 取出实例（返回 IReference，需强转） |
| `Release(IReference reference)` | 将对象归还到池（内部调用 OnRelease） |
| `Release<T>(T reference)` | 泛型版本 |

### 示例

```csharp
// 定义可入池的消息类
public class DamageEvent : IEventMessage, IReference
{
    public int Damage;
    public void OnRelease() => Damage = 0;

    public static DamageEvent Create(int damage)
    {
        var e = ReferencePool.Spawn<DamageEvent>();
        e.Damage = damage;
        return e;
    }
}

// 发送（EventManager 会自动归还）
EventManager.Instance.SendMessage(DamageEvent.Create(100));

// 手动归还
ReferencePool.Release(someRef);
```

---

## 事件系统 — EventManager

**命名空间**：`ZEngine.Manager.Event`  
**依赖**：`LogManager`

### 方法

| 方法 | 说明 |
|------|------|
| `AddListener<T>(Action<IEventMessage> listener)` | 注册泛型事件监听 |
| `AddListener(Type type, Action<IEventMessage> listener)` | 按 Type 注册监听 |
| `RemoveListener<T>(Action<IEventMessage> listener)` | 移除泛型事件监听 |
| `RemoveListener(Type type, Action<IEventMessage> listener)` | 按 Type 移除监听 |
| `SendMessage(IEventMessage message)` | **当帧**广播事件（同步，按注册顺序执行，执行完自动归还引用） |
| `DelayMessage(IEventMessage message, int delayFrame = 1)` | **延迟 N 帧**广播，可用于线程安全场景 |
| `ClearListener()` | 清空所有监听 |

### 定义事件消息

```csharp
// 所有事件必须实现 IEventMessage（推荐同时实现 IReference 以复用）
public class PlayerDiedEvent : IEventMessage, IReference
{
    public string PlayerName;
    public void OnRelease() => PlayerName = null;

    public static PlayerDiedEvent Create(string name)
    {
        var e = ReferencePool.Spawn<PlayerDiedEvent>();
        e.PlayerName = name;
        return e;
    }
}
```

### 使用示例

```csharp
// 注册
EventManager.Instance.AddListener<PlayerDiedEvent>(OnPlayerDied);

// 注销（组件销毁时务必注销）
EventManager.Instance.RemoveListener<PlayerDiedEvent>(OnPlayerDied);

// 发送（当帧）
EventManager.Instance.SendMessage(PlayerDiedEvent.Create("Alice"));

// 发送（延迟1帧，适用于子线程发起的事件）
EventManager.Instance.DelayMessage(PlayerDiedEvent.Create("Bob"), delayFrame: 1);

// 监听函数
private void OnPlayerDied(IEventMessage msg)
{
    var e = msg as PlayerDiedEvent;
    Debug.Log($"{e.PlayerName} 死亡");
}
```

**注意**
- `SendMessage` 执行完毕后自动调用 `ReferencePool.Release`，之后不得再访问该消息对象
- 遍历中移除监听是安全的（内部保存了 next 节点）
- 使用 `EventGroup` 可以批量管理同一组件的监听，方便统一注销

### EventGroup（批量管理）

```csharp
// 创建分组（通常在 Controller 中持有）
var group = new EventGroup();
group.AddListener<PlayerDiedEvent>(OnPlayerDied);
group.AddListener<LevelUpEvent>(OnLevelUp);

// 一次性注销该组所有监听
group.RemoveAllListener();
```

---

## 日志系统 — LogManager

**命名空间**：`ZEngine.Manager.Log`  
**说明**：基于 BqLog 的持久化日志系统，LogManager 初始化后自动接管 `ZEngineLog` 的输出

### 方法（均为静态调用）

| 方法 | 等级 | 说明 |
|------|------|------|
| `LogManager.Instance.Verbose(string msg)` | VERBOSE | 最详细，仅调试用 |
| `LogManager.Instance.Debug(string msg)` | DEBUG | 调试信息 |
| `LogManager.Instance.Info(string msg)` | INFO | 一般信息 |
| `LogManager.Instance.Warning(string msg)` | WARNING | 警告 |
| `LogManager.Instance.Error(string msg)` | ERROR | 错误 |
| `LogManager.Instance.Fatal(string msg)` | FATAL | 致命错误 |
| `LogManager.Instance.TakeSnapShot()` | — | 抓取日志快照 |
| `LogManager.Instance.Flush()` | — | 强制刷新日志到磁盘 |
| `LogManager.Instance.DecodeLogFileToString(string path)` | — | 解码 BqLog 二进制文件为字符串 |

**日志流向**
- `LogManager` 创建**前**：`ZEngineLog` → Unity Console
- `LogManager` 创建**后**：`ZEngineLog` → `LogManager` → Unity Console + BqLog 文件

**框架内部日志**：使用 `ZEngineLog.Log/Warning/Error/Exception(string msg)`，会自动路由到 LogManager。

---

## 计时器 — TimerManager

**命名空间**：`ZEngine.Manager.Timer`  
**依赖**：`LogManager`

### TimerManager 工厂方法

| 方法 | 说明 |
|------|------|
| `CreateOnceTimer(Action callback, float delay)` | 延迟 `delay` 秒后触发**一次**回调 |
| `CreatePepeatTimer(Action callback, float delay, float interval)` | 延迟后，每隔 `interval` 秒永久触发 |
| `CreatePepeatTimer(Action callback, float delay, float interval, float duration)` | 延迟后，在 `duration` 秒内每隔 `interval` 秒触发 |
| `CreatePepeatTimer(Action callback, float delay, float interval, long maxTriggerCount)` | 延迟后，每隔 `interval` 秒触发，共触发 `maxTriggerCount` 次 |
| `CreateDurationTimer(Action callback, float delay, float duration)` | 延迟后，在 `duration` 秒内**每帧**触发 |
| `CreateForeverTimer(Action callback, float delay)` | 延迟后，**每帧**永久触发 |
| `CreateTimer(Action callback, float delay, float interval, float duration, long maxTriggerCount)` | 底层通用创建，负数参数表示"不限制" |

所有方法返回 `Timer` 实例，可通过实例控制计时器行为。

### Timer 实例方法

| 方法/属性 | 说明 |
|---------|------|
| `Kill()` | 静默取消计时器（**不触发回调**），已创建的计时器可随时取消 |
| `Pause()` | 暂停（不计时，不触发回调） |
| `Resume()` | 恢复 |
| `Reset()` | 重置所有计时器状态 |
| `IsOver` | 是否已结束 |
| `IsPause` | 是否暂停中 |
| `DelayTime` | 配置的延迟时间 |
| `Remaining` | 延迟倒计时剩余时间 |
| `CallBack` | 回调函数引用 |

### Timer 语义说明

| 参数 | 负数（-1）含义 | 正数含义 |
|------|-------------|---------|
| `interval` | 每帧触发 | 每隔 N 秒触发 |
| `duration` | 不限时长 | 超过该时长后停止（**超时那帧不触发回调**） |
| `maxTriggerCount` | 不限次数 | 累计触发 N 次后停止（**第 N 次仍触发回调**） |

### 示例

```csharp
var tm = TimerManager.Instance;

// 3 秒后执行一次
tm.CreateOnceTimer(() => Debug.Log("3秒到"), 3f);

// 每 0.5 秒执行，共 5 次
tm.CreatePepeatTimer(() => Debug.Log("tick"), 0f, 0.5f, 5L);

// 每 1 秒执行，持续 10 秒（最多触发 10 次）
tm.CreatePepeatTimer(() => Debug.Log("tick"), 0f, 1f, 10f);

// 存引用，后续可取消
var t = tm.CreatePepeatTimer(() => Debug.Log("tick"), 0f, 1f);
t.Kill(); // 静默取消，不触发回调
```

---

## 资源管理 — ResourceManager

**命名空间**：`ZEngine.Manager.Resource`  
**底层**：YooAsset  
**注意**：所有 `location` 参数均为相对于 `locationRoot` 的相对路径，不能为空

### 初始化（GameLauncher 中）

```csharp
var initParam = new EditorSimulateModeParameters(); // 或 OfflinePlayModeParameters / HostPlayModeParameters
ZEngineMain.CreateManager<ResourceManager>(initParam, priority: 90);

// 异步初始化（开始游戏前必须等待完成）
var op = ResourceManager.Instance.InitializeAsync(out var package, locationRoot: "Assets/GameRes/");
await op.ToUniTask();
```

### 资源加载

| 方法 | 说明 |
|------|------|
| `LoadAssetAsync<T>(string location)` | 异步加载资源，返回 `AssetHandle` |
| `LoadAssetSync<T>(string location)` | 同步加载资源，返回 `AssetHandle` |
| `LoadAssetAsync(Type type, string location)` | 按 Type 异步加载 |
| `LoadAssetSync(Type type, string location)` | 按 Type 同步加载 |
| `LoadSubAssetsAsync<T>(string location)` | 异步加载子资源集合 |
| `LoadSubAssetsSync<T>(string location)` | 同步加载子资源集合 |

### 场景加载

| 方法 | 说明 |
|------|------|
| `LoadSceneAsync(string location, LoadSceneMode, LocalPhysicsMode, bool suspendLoad, uint priority)` | 异步加载场景 |
| `LoadSceneSync(string location, LoadSceneMode, LocalPhysicsMode)` | 同步加载场景 |

### 资源更新（热更新流程）

| 方法 | 说明 |
|------|------|
| `RequestPackageVersion()` | 异步请求远端包版本号 |
| `UpdatePackageManifestAsync(string version)` | 异步更新资源清单 |
| `DownLoadPackageFiles()` | 异步下载所有待更新资源文件 |
| `IsNeedDownLoadFromRemote(string location)` | 判断指定资源是否需要从远端下载 |
| `GetBoundleInfos(string[] tags)` | 获取带标签的资源信息列表 |

### 资源释放

| 方法 | 说明 |
|------|------|
| `Release(AssetHandle handle)` | 释放单个资源句柄 |
| `UnloadUnusedAssets()` | 异步卸载引用计数为 0 的资源 |
| `ForceUnloadAllAssets()` | 异步强制卸载所有资源 |

### 示例

```csharp
// 异步加载 Prefab
var handle = await ResourceManager.Instance.LoadAssetAsync<GameObject>("UI/MainPanel");
var go = GameObject.Instantiate(handle.AssetObject as GameObject);

// 释放句柄（GameObject 销毁后调用）
ResourceManager.Instance.Release(handle);
```

---

## 对象池 — ObjectPoolManager

**命名空间**：`ZEngine.Manager.Pool`  
**依赖**：`ResourceManager`、`LogManager`  
**说明**：专用于 Unity GameObject 的对象池，资源加载是异步的

### 创建参数 `CreateParameters`

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `EnableLazyPool` | bool | false | 是否启用惰性对象池 |
| `DefaultInitCapacity` | int | 0 | 池初始容量 |
| `DefaultMaxCapacity` | int | int.MaxValue | 池最大容量 |
| `DefaultDestroyTime` | float | -1f | 静默销毁时间（秒），-1 表示不自动销毁 |

### ObjectPoolManager 方法

| 方法 | 说明 |
|------|------|
| `Spawn(string location, ...)` | 取出（或创建）一个 GameObject，返回 `SpawnGameObject`（异步完成通过 Completed 事件） |
| `CreatePool(string location, ...)` | 预先创建对象池，返回 `GameObjectCollector` |
| `IsHasPool(string location)` | 判断是否已有该地址的对象池 |
| `GetSpawnGameObjectsByTag(string tag)` | 按标签获取所有正在使用的对象 |
| `DestroyAll()` | 销毁所有非常驻对象池 |
| `IsAllDone()` | 判断所有对象池是否都已加载完毕 |

### SpawnGameObject（取出的对象包装）

| 属性/方法 | 说明 |
|---------|------|
| `Go` | 取出的 GameObject（异步加载时可能为 null，需等待 Completed） |
| `UserData` | 用户自定义数据 |
| `Completed` | 加载完成事件 `Action<SpawnGameObject>` |
| `Restore()` | 归还对象到池 |
| `Discard()` | 销毁该对象（不归还池） |

### 示例

```csharp
// 异步取出（首次调用会加载资源）
var spawnObj = ObjectPoolManager.Instance.Spawn("Bullet");
spawnObj.Completed += (obj) =>
{
    obj.Go.transform.position = spawnPoint;
    obj.Go.SetActive(true);

    // 使用完毕后归还
    TimerManager.Instance.CreateOnceTimer(() => obj.Restore(), 3f);
};
```

---

## 网络 — NetworkManager

**命名空间**：`ZEngine.Manager.Network`  
**协议支持**：TCP、UDP（LiteNetLib）、WebSocket（websocket-sharp）  
**说明**：NetworkManager 持有三个通道，通过 EventManager 通知连接状态变化

### NetworkManager 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `TcpChannel` | `TcpChannel` | TCP 通道实例 |
| `UdpChannel` | `UdpChannel` | UDP 通道实例 |
| `WebSocketChannel` | `WebSocketChannel` | WebSocket 通道实例 |

### NetworkManager 方法

| 方法 | 说明 |
|------|------|
| `DisconnectAll()` | 断开所有通道连接 |

---

### TcpChannel

用于可靠的游戏逻辑传输（登录、背包、战斗指令等），支持心跳检测与指数退避自动重连。

**消息格式**：`[Length(4字节)] + [MessageId(2字节)] + [Body(变长)]`

#### TcpChannel 属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `HeartbeatEnabled` | bool | true | 是否启用自动心跳 |
| `HeartbeatInterval` | float | 5f | 心跳发送间隔（秒） |
| `HeartbeatTimeout` | float | 15f | 单次心跳超时时间（秒） |
| `MaxHeartbeatTimeoutCount` | int | 3 | 累计超时次数达到该值后强制断开 |
| `AutoReconnectEnabled` | bool | true | 是否启用自动重连 |
| `ReconnectInterval` | float | 3f | 重连基础间隔（秒，指数退避起点） |
| `MaxReconnectInterval` | float | 60f | 重连最大等待时间（秒） |
| `MaxReconnectCount` | int | 5 | 最大重连次数 |
| `IsConnected` | bool | — | 是否已连接 |
| `IsReconnecting` | bool | — | 是否处于重连等待中 |
| `State` | `NetworkState` | — | 当前连接状态 |
| `Host` | string | — | 当前连接的服务器地址 |
| `Port` | int | — | 当前连接的端口 |

#### TcpChannel 使用示例

```csharp
var tcp = NetworkManager.Instance.TcpChannel;

// 配置（连接前设置）
tcp.HeartbeatInterval     = 5f;
tcp.HeartbeatTimeout      = 15f;
tcp.MaxReconnectCount     = 5;
tcp.ReconnectInterval     = 3f;

// 连接
tcp.Connect("127.0.0.1", 8888);

// 断开
tcp.Disconnect();

// 发送 Protobuf 消息（messageId 与服务端约定）
tcp.Send(1001, myLoginRequest);

// 注册消息处理器
tcp.RegisterHandler<LoginResponse>(1001, msg =>
{
    Debug.Log($"登录结果: {msg.Code}");
});

// 注销消息处理器
tcp.UnregisterHandler(1001);

// 主动发送心跳（内部已自动心跳，一般无需手动调用）
tcp.SendHeartbeat();
```

---

### WebSocketChannel

用于实时推送、AI 对话、匹配等场景，底层基于 websocket-sharp，回调在后台线程触发、消息处理在主线程。

支持两种消息模式，可在同一连接上混用：

| 模式 | 帧类型 | 格式 | 适用场景 |
|------|--------|------|----------|
| **二进制（Protobuf）** | Binary | `[MessageId(2字节)] + [Body(变长)]` | 游戏逻辑、结构化数据 |
| **文本（JSON）** | Text | 任意 UTF-8 字符串 | AI 对话、灵活协议、Python/Node.js 后端 |

#### WebSocketChannel 属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `UseSsl` | bool | false | 是否使用 `wss://`（需在 Connect 前设置） |
| `HeartbeatEnabled` | bool | true | 是否启用心跳 |
| `HeartbeatInterval` | float | 5f | 心跳发送间隔（秒） |
| `HeartbeatTimeout` | float | 15f | 超时无消息后强制断开（秒） |
| `HeartbeatMessageId` | int | 1 | 心跳消息的 MessageId（仅二进制模式） |
| `HeartbeatMessageFactory` | `Func<IMessage>` | null | 自定义心跳消息工厂；为 null 时退化为 WebSocket Ping 帧 |
| `AutoReconnectEnabled` | bool | true | 是否启用自动重连 |
| `ReconnectInterval` | float | 3f | 重连等待间隔（秒，固定间隔） |
| `MaxReconnectCount` | int | 5 | 最大重连次数 |
| `IsConnected` | bool | — | 是否已连接 |
| `IsReconnecting` | bool | — | 是否处于重连等待中 |
| `State` | `NetworkState` | — | 当前连接状态 |
| `Host` | string | — | 连接的主机名 |
| `Port` | int | — | 连接的端口 |

#### WebSocketChannel 连接方式

```csharp
var ws = NetworkManager.Instance.WebSocketChannel;

// 方式一：host + port（自动拼 ws:// 或 wss:// 前缀）
ws.UseSsl = false;
ws.Connect("127.0.0.1", 9090);

// 方式二：完整 URL（推荐，支持带路径的端点）
ws.Connect("ws://127.0.0.1:9090/chat");
ws.Connect("wss://api.example.com/realtime");
```

#### 二进制消息（Protobuf）

```csharp
var ws = NetworkManager.Instance.WebSocketChannel;

// ── 配置（连接前设置）────────────────────────────────────────────────────
ws.HeartbeatEnabled  = true;
ws.HeartbeatInterval = 10f;
ws.HeartbeatTimeout  = 30f;

// 使用 Protobuf 消息作为心跳（不设置则退化为 WebSocket Ping 帧）
ws.HeartbeatMessageId      = 1;
ws.HeartbeatMessageFactory = () => new HeartbeatRequest { Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };

ws.AutoReconnectEnabled = true;
ws.MaxReconnectCount    = 5;
ws.ReconnectInterval    = 3f;

// ── 连接 ─────────────────────────────────────────────────────────────────
ws.Connect("ws://127.0.0.1:9090/gateway");

// ── 发送 ──────────────────────────────────────────────────────────────────
ws.Send(2001, new ChatMessage { Text = "你好" });

// ── 注册/注销处理器 ───────────────────────────────────────────────────────
ws.RegisterHandler<ChatMessage>(2001, msg =>
{
    Debug.Log($"收到: {msg.Text}");
});
ws.UnregisterHandler(2001);

// ── 断开 ─────────────────────────────────────────────────────────────────
ws.Disconnect();
```

#### 文本消息（JSON）

```csharp
var ws = NetworkManager.Instance.WebSocketChannel;

// ── 禁用内置心跳，由上层自行管理应用层心跳 ──────────────────────────────
ws.HeartbeatEnabled = false;
ws.AutoReconnectEnabled = true;
ws.MaxReconnectCount    = 10;

// ── 注册文本消息回调（主线程触发，线程安全）─────────────────────────────
ws.RegisterTextHandler(text =>
{
    // text 是服务端发来的原始 JSON 字符串
    Debug.Log($"收到 JSON: {text}");
});

// ── 连接 ─────────────────────────────────────────────────────────────────
ws.Connect("ws://127.0.0.1:8080/leiya");

// ── 发送文本帧 ────────────────────────────────────────────────────────────
string json = JsonConvert.SerializeObject(new { type = "ping", seq = 1 });
ws.SendText(json);

// ── 注销 ─────────────────────────────────────────────────────────────────
ws.UnregisterTextHandler();

// ── 断开 ─────────────────────────────────────────────────────────────────
ws.Disconnect();
```

#### 文本消息 API

| 方法 | 说明 |
|------|------|
| `SendText(string text)` | 发送 UTF-8 文本帧（异步，不阻塞主线程） |
| `RegisterTextHandler(Action<string> handler)` | 注册文本消息回调；每次调用覆盖前一个，通道同时只支持一个文本处理器 |
| `UnregisterTextHandler()` | 注销文本消息回调 |

> **线程安全**：文本帧到达时在 websocket-sharp 后台线程入队，在主线程 `Update()` 的 `UpdateReceiveQueue()` 中出队并调用 `handler`，业务代码始终在主线程执行，无需加锁。  
> **与二进制共存**：二进制帧和文本帧走各自独立的队列，互不干扰，可在同一连接上混用。  
> **心跳**：使用文本模式时建议将 `HeartbeatEnabled` 设为 `false`，改由上层代码定期调用 `SendText` 发送应用层心跳（避免与 Protobuf 心跳混淆）。

---

### 网络事件（通过 EventManager 监听）

所有通道共享同一套事件，通过 `ChannelType` 字段区分来源。

```csharp
// 连接成功（ChannelType / Host / Port）
EventManager.Instance.AddListener<NetworkConnectedEvent>(OnConnected);
// 断开连接（ChannelType / Reason）
EventManager.Instance.AddListener<NetworkDisconnectedEvent>(OnDisconnected);
// 连接失败（ChannelType / ErrorMessage）
EventManager.Instance.AddListener<NetworkConnectFailedEvent>(OnConnectFailed);
// 发生错误（ChannelType / ErrorMessage）
EventManager.Instance.AddListener<NetworkErrorEvent>(OnError);
// 正在重连（ChannelType / CurrentCount / MaxCount）
EventManager.Instance.AddListener<NetworkReconnectingEvent>(OnReconnecting);
// 重连成功（ChannelType / Host / Port）
EventManager.Instance.AddListener<NetworkReconnectedEvent>(OnReconnected);
// 重连失败——达到最大次数（ChannelType）
EventManager.Instance.AddListener<NetworkReconnectFailedEvent>(OnReconnectFailed);
```

```csharp
// 监听示例：区分通道来源
private void OnConnected(IEventMessage msg)
{
    var e = (NetworkConnectedEvent)msg;
    if (e.ChannelType == NetworkChannelType.WebSocket)
        Debug.Log($"WebSocket 已连接: {e.Host}:{e.Port}");
}

private void OnDisconnected(IEventMessage msg)
{
    var e = (NetworkDisconnectedEvent)msg;
    Debug.Log($"[{e.ChannelType}] 断开，原因: {e.Reason}");
}
```

### NetworkChannelType 枚举

```csharp
NetworkChannelType.Tcp        // TCP 通道
NetworkChannelType.Udp        // UDP 通道
NetworkChannelType.WebSocket  // WebSocket 通道
```

### NetworkState 枚举

```csharp
NetworkState.Disconnected   // 未连接
NetworkState.Connecting     // 连接中
NetworkState.Connected      // 已连接
NetworkState.Disconnecting  // 断开中
```

### DisconnectReason 枚举

```csharp
DisconnectReason.Unknown       // 未知原因
DisconnectReason.Manual        // 主动断开（调用 Disconnect()）
DisconnectReason.Timeout       // 心跳超时
DisconnectReason.ServerClosed  // 服务器主动关闭
DisconnectReason.NetworkError  // 网络错误
DisconnectReason.Kicked        // 被踢出
```

---

## HTTP — HttpManager

**命名空间**：`ZEngine.Manager.Http`  
**依赖**：`LogManager`  
**底层**：`UnityWebRequest` + `UniTask`  
**默认**：Content-Type: application/json，超时 30 秒

### 配置方法

| 方法 | 说明 |
|------|------|
| `SetBaseUrl(string url)` | 设置基础 URL，之后可使用相对路径 |
| `SetDefaultTimeout(int seconds)` | 设置默认超时时间 |
| `SetAuthorization(string token)` | 设置 Authorization 请求头 |
| `SetBearerToken(string token)` | 设置 `Bearer {token}` 格式认证头 |
| `ClearAuthorization()` | 清除认证头 |
| `SetHeader(string key, string value)` | 添加/修改默认请求头 |
| `RemoveHeader(string key)` | 移除默认请求头 |
| `ClearDefaultHeaders()` | 清空所有默认请求头 |

### 请求方法

| 方法 | 说明 |
|------|------|
| `GetAsync(string url, headers, timeout, ct)` | GET 请求，返回 `HttpResponse` |
| `GetAsync<T>(string url, ...)` | GET 请求，自动 JSON 反序列化为 T |
| `PostAsync(string url, object body, ...)` | POST 请求，body 自动序列化为 JSON |
| `PostAsync<T>(string url, object body, ...)` | POST 请求，返回反序列化结果 |
| `PostJsonAsync(string url, string jsonBody, ...)` | POST 原始 JSON 字符串 |
| `PutAsync(string url, object body, ...)` | PUT 请求 |
| `PutAsync<T>(string url, object body, ...)` | PUT 请求，返回反序列化结果 |
| `DeleteAsync(string url, ...)` | DELETE 请求 |
| `DeleteAsync<T>(string url, ...)` | DELETE 请求，返回反序列化结果 |
| `PostFormAsync(string url, Dictionary<string,string> formData, ...)` | 表单 POST |
| `DownloadAsync(string url, ..., IProgress<float> progress, ...)` | 下载文件（返回字节数组），支持进度回调 |
| `DownloadTextAsync(string url, ...)` | 下载文本文件 |

### HttpResponse 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `IsSuccess` | bool | 请求是否成功 |
| `StatusCode` | long | HTTP 状态码 |
| `Result` | `HttpResult` | 结果枚举 |
| `RawText` | string | 原始响应文本 |
| `RawBytes` | byte[] | 原始响应字节 |
| `Headers` | `Dictionary<string,string>` | 响应头 |
| `Data` | T（泛型版） | 反序列化后的数据 |

### HttpResult 枚举

```csharp
HttpResult.Success        // 成功（2xx）
HttpResult.NetworkError   // 网络连接错误
HttpResult.Timeout        // 超时
HttpResult.ServerError    // 服务器错误（5xx）
HttpResult.ClientError    // 客户端错误（4xx）
HttpResult.ParseError     // JSON 解析失败
HttpResult.Unknown        // 未知错误
```

### 示例

```csharp
var http = HttpManager.Instance;
http.SetBaseUrl("https://api.example.com");
http.SetBearerToken("my-jwt-token");

// GET + 反序列化
var response = await http.GetAsync<UserInfo>("/user/profile");
if (response.IsSuccess)
    Debug.Log(response.Data.Name);

// POST
var result = await http.PostAsync("/login",
    new { username = "admin", password = "123456" });

// 下载（带进度）
var progress = new Progress<float>(p => Debug.Log($"下载进度: {p:P0}"));
var file = await http.DownloadAsync("https://cdn.example.com/file.zip",
    progress: progress);
```

---

## 音频 — AudioManager

**命名空间**：`ZEngine.Manager.Audio`

### EAudioLayer 枚举

```csharp
EAudioLayer.Music    // 背景音乐（同时只播一个，切换时淡出淡入）
EAudioLayer.Ambient  // 环境音效
EAudioLayer.Voice    // 语音（同时只播一个）
EAudioLayer.Sound    // 音效（可多个同时播放）
```

### 播放方法

| 方法 | 说明 |
|------|------|
| `PlayMusic(string location, bool loop)` | 播放背景音乐 |
| `PlayAmbient(string location, bool loop)` | 播放环境音效 |
| `PlayVoice(string location)` | 播放语音 |
| `PlaySound(string location)` | 播放音效（全局，无距离衰减） |
| `PlaySound(AudioSource audioSource, string location)` | 在指定 AudioSource 上播放音效 |
| `PlaySoundAtPosition(string location, Vector3 worldPos, float minDistance, float maxDistance)` | 在世界坐标播放音效，音量按 XY 距离线性衰减（2D 适配） |
| `SetSoundListener(Transform listener)` | 注册距离衰减的监听器（通常为主摄像机），传 `null` 退化为全音量 |
| `Stop(EAudioLayer layer)` | 停止指定层级的播放 |

### 音量与静音

| 方法 | 说明 |
|------|------|
| `Mute(bool isMute)` | 全部频道静音 |
| `Mute(EAudioLayer layer, bool isMute)` | 指定频道静音 |
| `IsMute(EAudioLayer layer)` | 查询指定频道是否静音 |
| `Volume(float volume)` | 设置全部频道音量（0~1） |
| `Volume(EAudioLayer layer, float volume)` | 设置指定频道音量 |
| `GetVolume(EAudioLayer layer)` | 获取指定频道当前音量 |

### 资源管理

| 方法 | 说明 |
|------|------|
| `Preload(string location, EAudioLayer audioLayer)` | 预加载音频资源 |
| `Release(EAudioLayer audioLayer)` | 释放指定层级所有音频 |
| `Release(EAudioLayer audioLayer, string location)` | 释放指定层级的特定音频 |
| `ReleaseAll()` | 释放全部音频资源 |
| `GetAudioSource(EAudioLayer layer)` | 获取原始 AudioSource 组件 |

### 位置音效衰减规则

| 距离 | 音量 |
|------|------|
| ≤ `minDistance` | 全音量（`Sound` 层当前音量） |
| ≥ `maxDistance` | 0（静音） |
| 中间 | 线性插值 |

- 衰减基于 **XY 平面距离**（忽略 Z 轴），适配 2D 正交相机
- 未调用 `SetSoundListener` 时退化为全音量播放，不报错
- 调用 `Release(EAudioLayer.Sound)` 或 `ReleaseAll()` 时，所有进行中的位置音效立即停止并销毁

### 示例

```csharp
var audio = AudioManager.Instance;

// 普通音效
audio.PlayMusic("BGM/MainTheme", loop: true);
audio.Volume(EAudioLayer.Music, 0.8f);
audio.PlaySound("SFX/ButtonClick");
audio.Mute(EAudioLayer.Sound, true);

// 位置音效（初始化时注册监听器，之后每次播放只需传坐标）
audio.SetSoundListener(Camera.main.transform);

// NPC 脚步声：5 格内全音量，30 格外静音
audio.PlaySoundAtPosition("SFX/Footstep", npc.transform.position, minDistance: 5f, maxDistance: 30f);

// 爆炸：冲击范围大
audio.PlaySoundAtPosition("SFX/Explosion", hitPos, minDistance: 10f, maxDistance: 80f);
```

---

## UI（UGUI）— UUIManager

**命名空间**：`ZEngine.Manager.UI.UGUI`  
**依赖**：`ObjectPoolManager`、`ResourceManager`

ZEngine UGUI 采用 **MVC 架构**：View（显示）+ Controller（逻辑）+ Model（数据）

### UIRoot 创建方式

`UUIManager.OnInit()` 自动在场景中创建 UIRoot（`ScreenSpaceOverlay Canvas`，1920×1080 参考分辨率）及 7 个层级容器，**无需在场景中预先放置任何 UI 对象**。

### 层级 UUILayer 枚举

```csharp
UUILayer.Background_Layer = 1000  // 背景层
UUILayer.Bottom_Layer     = 2000  // 底层（默认）
UUILayer.Middle_Layer     = 3000  // 中层
UUILayer.Top_Layer        = 4000  // 顶层
UUILayer.Window_Layer     = 5000  // 弹窗层
UUILayer.Guide_Layer      = 6000  // 引导层
UUILayer.Max_Layer        = 7000  // 最外层
```

通过 `UUIManager.Instance.GetLayer(UUILayer)` 获取对应层级的父 GameObject。

### UIViewAttribute — 元数据特性（必须标注）

所有 View 类**必须**标注 `[UIViewAttribute]`，替代旧的静态 `Location` 字段。

```csharp
[UIViewAttribute(
    location:     "UI/MainPanel",           // 资源路径（必填）
    layer:        UUILayer.Bottom_Layer,    // 所属层级（默认 Bottom_Layer）
    isSingleton:  true,                     // 是否单例（默认 true）
    isFullScreen: false)]                   // 是否全屏（全屏时自动隐藏下方 View）
public class MainPanelView : UBaseView { ... }
```

`LayerType`、`IsSingleton`、`IsFullScreen` 属性由框架从 Attribute 初始化，**不要在子类中 override**。

### UBaseView 子节点缓存

框架在调用 `Initialize()` **之前**会自动遍历 Prefab 的完整子层级，将每个节点的 `Component[]` 缓存到内部字典（key = 相对于根节点的路径）。子类直接通过路径访问，无需 Inspector 拖拽赋值。

| 方法 | 说明 |
|------|------|
| `GetChild<T>(string relativePath)` | 按相对路径取子节点上第一个 T 类型组件，找不到返回 null |
| `GetChildGO(string relativePath)` | 按相对路径取子节点的 GameObject，找不到返回 null |

路径格式与 `Transform.Find` 相同，以子节点名称用 `/` 拼接，例如 `"Header/NameText"`、`"Footer/CloseBtn"`。

> **同名节点**：同层级存在同名子节点时，缓存只保留 Transform 顺序靠前的那个，与 `Transform.Find` 行为一致。

### View 定义规范

```csharp
using ZEngine.Manager.UI;
using ZEngine.Manager.UI.UGUI;
using UnityEngine.UI;

[UIView("UI/CharacterPanel", UUILayer.Window_Layer, isSingleton: true, isFullScreen: false)]
public class CharacterPanelView : UBaseView
{
    public override Type ControllerType => typeof(CharacterPanelController);
    public override Type ModelType      => typeof(CharacterPanelModel);

    // 缓存在 Initialize() 前已就绪，直接在 Initialize() / OnComplete() 中读取
    public Text   NameText;
    public Button CloseBtn;

    public override void Initialize()
    {
        // GetChild<T> 按 Prefab 中节点的相对路径取组件
        NameText = GetChild<Text>("Header/NameText");
        CloseBtn = GetChild<Button>("Footer/CloseBtn");
    }

    // 资源/绑定完成（此时 UI 已挂载到层级容器下）
    public override void OnComplete() { }

    // 释放：框架自动判断——异步路径调用 PoolHandle.Restore()，同步路径调用 Destroy()
    // 一般无需 override，除非有额外清理逻辑
    public override void OnRelease()
    {
        _eventGroup.RemoveAllListener();
        base.OnRelease();
    }

    // 自管理生命周期：View 内部将此属性置 true，下一帧框架自动关闭
    // 适合世界 UI（如 NPC 死亡后头顶标签自动消失）
    // this.CanRemoved = true;
}
```

### Controller 定义规范

```csharp
public class CharacterPanelController : UBaseController
{
    // _view / _model 由基类持有，通过 SetView() 由框架赋值
    public override void Initialize()
    {
        var view  = _view  as CharacterPanelView;
        var model = _model as CharacterPanelModel;

        // 刷新 UI（NameText / CloseBtn 已在 View.Initialize() 中绑定）
        view.NameText.text = model.CharName;

        // 绑定按钮
        view.CloseBtn.onClick.AddListener(
            () => UUIManager.Instance.CloseView<CharacterPanelView>());
    }

    public override void OnUpdate() { /* 每帧逻辑（仅 Visible 时调用） */ }

    public override void OnRelease()
    {
        var view = _view as CharacterPanelView;
        view?.CloseBtn.onClick.RemoveAllListeners();
        base.OnRelease(); // 必须调用，清空 _view / _model
    }
}
```

### Model 定义规范

```csharp
// UBaseModel 的 Initialize() / OnRelease() 均为 virtual，可按需 override
public class CharacterPanelModel : UBaseModel
{
    public string CharName = "周巧娘";
    public int    Level    = 10;

    public override void OnRelease()
    {
        CharName = null;
        Level    = 0;
    }
}
```

### UUIManager 方法

| 方法 | 说明 |
|------|------|
| `OpenViewSync<T>(UBaseModel model, Action<UBaseView> onOpened)` | **同步**打开 View（直接 Instantiate，不走对象池） |
| `OpenViewAsync<T>(UBaseModel model)` | **异步**打开 View（`async UniTask<T>`，走对象池，可直接 `await`） |
| `CloseView<T>(Action onClosed)` | 按类型关闭 View（关闭该类型最顶层的一个） |
| `CloseView(string viewID, Action onClosed)` | 按 GUID 精确关闭 View |
| `CloseAll()` | 关闭并归还所有 View |
| `HasView<T>()` | 查询该类型 View 是否已打开 |
| `GetLayer(UUILayer layer)` | 获取对应层级的父 GameObject |

**注意**
- 单例检查在资源加载**之前**进行，同一类型已存在时直接置顶，不重新加载资源
- `OpenViewAsync<T>` 是真正的 `async UniTask<T>`，可直接 `await`；失败时返回 `null`（不死锁）
- `OpenViewAsync` 通过 `ObjectPoolManager` 取对象，关闭时自动 `Restore()` 归还（非 Destroy）
- 全屏 View（`isFullScreen: true`）打开/关闭时，框架自动刷新其下所有 View 的可见性
- `CanRemoved` 在每次 `InitializeView` 时会被重置为 `false`，对象池复用时无需手动清除

### 使用示例

```csharp
// ── 同步打开（直接 Instantiate，不走对象池）────────────────────────────
var view = UUIManager.Instance.OpenViewSync<CharacterPanelView>();

// 携带数据（同步）
var model = ReferencePool.Spawn<CharacterPanelModel>();
model.CharName = "李大牛";
UUIManager.Instance.OpenViewSync<CharacterPanelView>(model);

// ── 异步打开（await）──────────────────────────────────────────────────
var panel = await UUIManager.Instance.OpenViewAsync<CharacterPanelView>();
if (panel == null) Debug.LogError("加载失败");  // 失败时返回 null，不死锁

// ── 异步打开（携带 Model）──────────────────────────────────────────────
var model = ReferencePool.Spawn<CharacterPanelModel>();
model.CharName = "李大牛";
var panel2 = await UUIManager.Instance.OpenViewAsync<CharacterPanelView>(model);

// ── fire-and-forget（不关心返回值时）──────────────────────────────────
UUIManager.Instance.OpenViewAsync<CharacterPanelView>().Forget();

// ── 关闭 ──────────────────────────────────────────────────────────────
UUIManager.Instance.CloseView<CharacterPanelView>();    // 按类型
UUIManager.Instance.CloseView(panel.ID);               // 按 ID（精确）
UUIManager.Instance.CloseAll();                         // 全部关闭

// ── 查询 ──────────────────────────────────────────────────────────────
bool isOpen = UUIManager.Instance.HasView<CharacterPanelView>();

// ── 非单例多实例（NPC 头顶标签）─────────────────────────────────────
[UIView("UI/World/NPCNameTag", UUILayer.Window_Layer, isSingleton: false)]
public class NPCNameTag : UBaseView
{
    void Update()
    {
        if (npc.IsDead) CanRemoved = true; // 下帧自动关闭并归还对象池
    }
}

UUIManager.Instance.OpenViewAsync<NPCNameTag>(null, view =>
{
    (view as NPCNameTag).Follow(npc.transform);
});
```

### 快速入门：完整面板步骤

```
1. 定义 Model      → 继承 UBaseModel，填充数据字段
2. 定义 View       → 继承 UBaseView，标注 [UIViewAttribute]
                     在 Initialize() 中用 GetChild<T>("路径") 取子节点组件（无需 Inspector 赋值）
3. 定义 Controller → 继承 UBaseController，Initialize() 中刷新 UI / 绑定按钮
4. 制作 Prefab     → View 组件挂到 Prefab 根节点即可，子节点命名与路径一致
5. 调用            → UUIManager.Instance.OpenViewSync<T>() 或 OpenViewAsyncAwait<T>()
```

---

## UI（FGUI）— UIManager

**命名空间**：`ZEngine.Manager.UI`  
**说明**：基于 FairyGUI 的 UI 管理，架构同 UGUI（MVC），基类为 `BaseView`、`BaseController`、`BaseModel`

### EUILayer 枚举（7 层）

```csharp
EUILayer.Background  // 背景层
EUILayer.Normal      // 普通层
EUILayer.Fixed       // 固定层
EUILayer.Window      // 窗口层
EUILayer.Popup       // 弹窗层
EUILayer.Guide       // 引导层
EUILayer.Top         // 顶层
```

---

## 有限状态机 — FiniteStateMachine

**命名空间**：`ZEngine.AI.FSM`  
**说明**：支持主状态转换 + 并行状态节点，适合 AI Agent

### 定义状态节点

```csharp
public class IdleState : IFsmNode
{
    public string Name => "IdleState";
    public FiniteStateMachine SubFsm => null; // 复合节点可有子 FSM

    public void OnEnter(FiniteStateMachine fsm) { }
    public void OnUpdate(FiniteStateMachine fsm) { }
    public void OnFixedUpdate(FiniteStateMachine fsm) { }
    public void OnExit(FiniteStateMachine fsm) { }
    public bool OnHandleMessage(FiniteStateMachine fsm, object message) => false;
}
```

### FiniteStateMachine 方法

| 方法 | 说明 |
|------|------|
| `Run(string nodeName)` | 启动 FSM，进入指定主状态 |
| `Transition(string nodeName)` | 切换主状态（触发当前状态 OnExit，新状态 OnEnter） |
| `Update()` | 驱动主状态 + 所有并行状态的 OnUpdate |
| `FixedUpdate()` | 驱动主状态 + 所有并行状态的 OnFixedUpdate |
| `HandleMessage(object message)` | 向主状态 + 所有并行状态分发消息 |
| `Stop()` | 停止 FSM（先退出所有并行状态，再退出主状态） |
| `AddParallelNode(IFsmNode node)` | 添加并行状态（立即触发 OnEnter，不参与状态转换） |
| `RemoveParallelNode(string name)` | 移除并行状态（触发 OnExit） |
| `HasParallelNode(string name)` | 查询并行状态是否存在 |
| `CurrentNodeName` | 当前主状态名称 |

### 示例（AI 小镇 Agent）

```csharp
var fsm = new FiniteStateMachine();
fsm.AddNode(new IdleState());
fsm.AddNode(new WalkState());
fsm.AddNode(new TalkState());

// 启动
fsm.Run("IdleState");

// 添加并行状态（LLM 思考、情绪系统同时运行）
fsm.AddParallelNode(new ThinkingNode());
fsm.AddParallelNode(new EmotionNode());

// 主状态切换（并行状态不受影响）
fsm.Transition("WalkState");

// 每帧驱动
fsm.Update();

// 停止（所有状态触发 OnExit）
fsm.Stop();
```

### FsmCompositeNode（复合状态，含子 FSM）

```csharp
public class CombatState : FsmCompositeNode
{
    public override string Name => "CombatState";

    protected override void ConfigureSubFsm(FiniteStateMachine subFsm)
    {
        subFsm.AddNode(new AttackState());
        subFsm.AddNode(new DodgeState());
        subFsm.Run("AttackState");
    }
}
```

---

## 过程状态机 — ProcedureFsm

**命名空间**：`ZEngine.AI.FSM`  
**说明**：线性流程控制，支持顺序切换，适合游戏启动流程、关卡过场

### ProcedureFsm 方法

| 方法 | 说明 |
|------|------|
| `AddNode(IFsmNode node)` | 添加流程节点（按添加顺序排列） |
| `Run(string nodeName)` | 启动，进入指定节点 |
| `Switch(string nodeName)` | 切换到指定节点 |
| `SwitchNext()` | 切换到下一个节点 |
| `SwitchLast()` | 切换到上一个节点 |
| `Update()` | 驱动当前节点 |
| `Current` | 当前节点 |
| `Previous` | 上一个节点 |

### 示例（启动流程）

```csharp
var procedure = new ProcedureFsm();
procedure.AddNode(new InitProcedure());
procedure.AddNode(new UpdateProcedure());
procedure.AddNode(new LoginProcedure());
procedure.AddNode(new MainMenuProcedure());
procedure.Run("InitProcedure");

// 流程内部调用
procedure.SwitchNext(); // InitProcedure → UpdateProcedure
```

---

## 输入系统 — InputManager

**命名空间**：`ZEngine.Module.Input`  
**类型**：静态类  
**底层**：Unity Input System

### InputManager 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `AllGamepads` | `ReadOnlyArray<Gamepad>` | 所有已连接手柄 |
| `AllKeyboards` | `ReadOnlyArray<Keyboard>` | 所有键盘 |
| `AllMouses` | `ReadOnlyArray<Mouse>` | 所有鼠标 |
| `MousePosition` | `Vector2` | 当前鼠标位置（屏幕坐标） |

### InputManager 方法

| 方法 | 说明 |
|------|------|
| `GetKeyDown(Key key)` | 按键按下（当帧） |
| `GetKey(Key key)` | 按键持续按住 |
| `GetKeyUp(Key key)` | 按键松开（当帧） |
| `AnyKeyDown()` | 任意按键按下 |
| `AnyKeyUp()` | 任意按键松开 |
| `GamepadVibrate(float low, float high, int index = 0)` | 手柄震动（low/high：0~1） |
| `GetStickValue(bool isLeft, int gamepadIndex = 0)` | 获取手柄摇杆值，`isLeft=true` 为左摇杆 |

### MouseButton 枚举

```csharp
MouseButton.LeftButton
MouseButton.MiddleButton
MouseButton.RightButton
MouseButton.ForwardButton
MouseButton.BackButton
MouseButton.Scroll
```

### 示例

```csharp
if (InputManager.GetKeyDown(Key.Space))
    Jump();

if (InputManager.GetKey(Key.W))
    MoveForward();

Vector2 stick = InputManager.GetStickValue(isLeft: true);
```

---

## 2D 碰撞 — ColliderManager

**命名空间**：`ZEngine.Module.Collider2D`  
**说明**：纯代码实现的 2D 碰撞系统（非 Unity PhysX），支持 Line / Box / Circle / Polygon / Sector / Chain 六种碰撞体

### 碰撞体组件（挂载到 GameObject）

| 组件 | 说明 |
|------|------|
| `LineCollider2D` | 线段碰撞体 |
| `BoxCollider2D` | 矩形碰撞体 |
| `CircleCollider2D` | 圆形碰撞体 |
| `PolygonCollider2D` | 多边形碰撞体 |
| `SectorCollider2D` | 扇形碰撞体（视野检测） |
| `ChainCollider2D` | 链式线段碰撞体 |

### BaseCollider2D 基类（所有碰撞体公共属性）

| 属性/事件 | 类型 | 说明 |
|---------|------|------|
| `Tag` | `ColliderTag` | 碰撞体标签（Player/Enemy/Floor/None） |
| `Position` | `Vector2` | 碰撞体世界位置 |
| `OnTriggerEnter` | `Action<BaseCollider2D>` | 开始重叠时触发 |
| `OnTriggerStay` | `Action<BaseCollider2D>` | 持续重叠时触发（每帧） |
| `OnTriggerExit` | `Action<BaseCollider2D>` | 结束重叠时触发 |

### ColliderTag 枚举

```csharp
ColliderTag.None
ColliderTag.Player
ColliderTag.Enemy
ColliderTag.Floor
```

### 静态工具 CollisionUtils

提供 70+ 个碰撞检测函数，覆盖各类型之间的相交判断：

```csharp
// 示例：判断圆与矩形是否相交
bool hit = CollisionUtils.CircleVsBox(circleCenter, radius, boxCenter, boxSize, rotation);

// 扇形与点（视野检测）
bool inView = CollisionUtils.SectorVsPoint(origin, direction, angle, range, targetPoint);
```

### 示例

```csharp
// 在 GameObject 上挂载
var circle = gameObject.AddComponent<CircleCollider2D>();
circle.Tag = ColliderTag.Player;
circle.OnTriggerEnter += (other) =>
{
    if (other.Tag == ColliderTag.Enemy)
        Debug.Log("接触到敌人！");
};
```

---

## 本地化 — LocalizationManager

**命名空间**：`ZEngine.Manager.Localization`

### Language 枚举

```csharp
Language.ChineseSimplified   // 简体中文
Language.ChineseTraditional  // 繁体中文
Language.English
Language.Japanese
Language.Korean
// ...（具体值见 Language.cs）
```

### LocalizationManager 使用

```csharp
// 切换语言
LocalizationManager.Instance.SetLanguage(Language.English);

// 获取本地化文本（key 对应配置表中的键）
string text = LocalizationManager.Instance.GetText("ui.start_button");
```

### LocalizationTextBind 组件

挂载在 Text 组件同一 GameObject 上，设置 key 后自动绑定，语言切换时自动刷新。

---

## 存档 — ArchiveManager

**命名空间**：`ZEngine.Module.Archive`  
**序列化**：MessagePack

### 定义存档 Slot

```csharp
[MessagePackObject]
public class PlayerSaveData : ArchiveSlotBase
{
    [Key(0)] public int Level;
    [Key(1)] public int Gold;

    public override void Init() { Level = 1; Gold = 0; }
    public override void BeforeSerialize() { /* 序列化前处理 */ }
    public override void AfterDeserializae() { /* 反序列化后处理 */ }
}
```

### ArchiveSlotBase 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `ID` | int | 存档槽 ID |
| `SlotName` | string | 存档名称 |
| `SaveTime` | string | 保存时间字符串 |
| `TimeStamp` | long | 保存时间戳 |
| `GetFileName()` | string | 存档文件名 |

### ArchiveManager 方法

```csharp
// 保存
ArchiveManager.Instance.Save(playerData);

// 加载
var data = ArchiveManager.Instance.Load<PlayerSaveData>(slotId: 0);

// 删除
ArchiveManager.Instance.Delete(slotId: 0);

// 获取所有存档信息
SlotInfo[] slots = ArchiveManager.Instance.GetAllSlots();
```

---

## 工具类

### ReferencePool（详见引用池章节）

### EnumExtension

```csharp
// 获取枚举所有值
EAudioLayer[] layers = EnumExtension.GetValues<EAudioLayer>();
```

### Vector3Extension

```csharp
Vector3 v = new Vector3(1, 2, 3);
Vector2 xy = v.V3ToXY(); // (1, 2)
Vector2 xz = v.V3ToXZ(); // (1, 3)
Vector2 yz = v.V3ToYZ(); // (2, 3)
```

### GameAssetPaths（ZEngine.Config）

集中定义所有资源路径前缀，供 Manager 的 `locationRoot` 参数使用。

```csharp
// 使用示例
ResourceManager.Instance.InitializeAsync(out var pkg,
    locationRoot: GameAssetPaths.UGUIPath);
```

### MathUtility / RandomUtility / HashUtility

```csharp
// 常用数学工具（具体 API 见 MathUtility.cs）
float clamped = MathUtility.Clamp(value, min, max);

// 随机工具
int rand = RandomUtility.Range(0, 100);

// 哈希
uint hash = HashUtility.ComputeHash("some_key");
```

---

## 常见错误排查

| 错误信息 | 原因 | 解决方案 |
|---------|------|---------|
| `XXXManager 尚未创建` | 访问 Instance 前未调用 CreateManager | 在 GameLauncher 中按依赖顺序创建 Manager |
| `XXXManager 依赖于 LogManager` | Manager 创建顺序错误 | 确保 LogManager 最先创建 |
| `资源路径不能为空` | ResourceManager 的 location 参数为 null/空 | 检查调用方传入的路径参数 |
| `ZEngine的behaviour为null` | Initialize 传入了 null | 传入有效的 MonoBehaviour |
| `ZEngine已经被初始化` | Initialize 被调用了两次 | 确保只在 Awake 中调用一次 |
| `参数priority不能是负数` | CreateManager 优先级为负 | 使用 0 或正整数 |

---

## 模块依赖图

```
LogManager（无依赖，必须最先创建）
    ↑
ResourceManager（依赖 LogManager）
    ↑
ObjectPoolManager（依赖 ResourceManager + LogManager）
EventManager（依赖 LogManager）
TimerManager（依赖 LogManager）
HttpManager（依赖 LogManager）
NetworkManager（依赖 EventManager，内部使用 EventManager 分发网络事件）
AudioManager（依赖 ResourceManager）
UUIManager（依赖 ObjectPoolManager + ResourceManager）
```
