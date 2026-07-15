# ZEngine 修复记录

> 修复日期：2026-05-13 / 2026-05-14

---

## P0 — 严重问题（会直接导致 Crash 或隐性 Bug）

---

### [P0-1] ZEngineMain：`_isDirty` 缺少 volatile 修饰

**文件**：`Assets/Plugins/ZEngine/Runtime/Core/ZEngineMain.cs`

**问题**：`_isDirty` 字段在 `CreateManager`（写）和 `Update`（读）中交叉访问，在多线程场景下 CPU 可能对写操作进行指令重排，导致读端看不到最新值。

**修复**：
```csharp
// Before
private static bool _isDirty = false;

// After
private static volatile bool _isDirty = false;
```

---

### [P0-2] ManagerSingleton：未初始化时静默返回 null

**文件**：`Assets/Plugins/ZEngine/Runtime/Core/ManagerSingleton.cs`

**问题**：`Instance` getter 在 `_instance == null` 时仅打印日志并返回 null，调用方拿到 null 后继续调用方法会出现 `NullReferenceException`，且错误堆栈指向调用方而非根因，极难排查。构造函数重复创建时也只打日志不阻止。

**修复**：改为直接抛出 `InvalidOperationException`，错误信息含类型名和使用提示。

```csharp
// Before
if (_instance == null)
    Debug.Log($"{typeof(T)} is not create...");
return _instance;  // 返回 null

// After
if (_instance == null)
    throw new InvalidOperationException(
        $"{typeof(T).Name} 尚未创建，请先调用 ZEngineMain.CreateManager<{typeof(T).Name}>()");
return _instance;
```

构造函数同步修改：重复创建时抛异常而非打日志。

---

### [P0-3] TcpChannel：心跳计时器精度问题

**文件**：`Assets/Plugins/ZEngine/Runtime/Manager/Manager.Network/TcpChannel.cs`

**问题**：`UpdateHeartbeat` 调用 `SendHeartbeat()` 后，后者将 `_lastHeartbeatSendTime = 0`（归零），若某帧出现峰值导致计时器超过两倍间隔（如 10.2s，间隔 5s），仍只发一次心跳，计时归零后下次从 0 开始，累计误差。

**修复**：拆分外部 API `SendHeartbeat()`（仍归零，供外部手动调用）与内部 `EnqueueHeartbeatPacket()`（只入队不修改计时器）。`UpdateHeartbeat` 内使用 `-=` 而非 `= 0`，保证精确节拍。

```csharp
// Before：UpdateHeartbeat 内
if (_lastHeartbeatSendTime >= HeartbeatInterval)
    SendHeartbeat();  // 内部 reset = 0，精度丢失

// After：UpdateHeartbeat 内
if (_lastHeartbeatSendTime >= HeartbeatInterval)
{
    _lastHeartbeatSendTime -= HeartbeatInterval;  // -= 保持精度
    EnqueueHeartbeatPacket();  // 不修改计时器
}
```

---

## P1 — 重要问题（影响核心功能）

---

### [P1-1] TcpChannel：重连无指数退避

**文件**：`Assets/Plugins/ZEngine/Runtime/Manager/Manager.Network/TcpChannel.cs`

**问题**：每次重连等待时间固定为 `ReconnectInterval`（默认 3s），在弱网/服务器故障期间会以固定频率持续轰炸服务器，无法有效保护服务端。

**修复**：新增 `MaxReconnectInterval` 属性和 `_currentReconnectInterval` 字段，在 `StartReconnect` 中计算带随机抖动的指数退避时间，`UpdateReconnect` 使用动态间隔。

```csharp
// Before：固定等待 ReconnectInterval
if (_reconnectWaitTime >= ReconnectInterval) DoConnect();

// After：指数退避 + ±20% 随机抖动
float backoff = ReconnectInterval * (float)Math.Pow(2, _reconnectCount - 1);
backoff = Math.Min(backoff, MaxReconnectInterval);  // 上限 60s
backoff *= UnityEngine.Random.Range(0.8f, 1.2f);   // 抖动
_currentReconnectInterval = backoff;

// 等待时间示例（基础 3s）：
// 第1次: ~3s, 第2次: ~6s, 第3次: ~12s, 第4次: ~24s, 第5次: ~48s
```

---

### [P1-2] FiniteStateMachine：不支持并行状态

**文件**：`Assets/Plugins/ZEngine/Runtime/AI/FSM/FiniteStateMachine.cs`

**问题**：原设计只有单一 `_curNode`，AI Agent 需要同时处于多个状态（如"移动中"+"思考中"）时无法实现。

**修复**：新增 `_parallelNodes` 列表及三个方法，并行节点与主节点独立运行，不参与状态转换。

```csharp
// 新增方法
AddParallelNode(IFsmNode node)     // 入队并触发 OnEnter
RemoveParallelNode(string name)    // 触发 OnExit 并移除
HasParallelNode(string name)       // 查询

// Update / FixedUpdate / HandleMessage / Stop 均已扩展支持并行节点
```

**典型用法（AI 小镇）**：
```csharp
fsm.Run("IdleState");                        // 主状态
fsm.AddParallelNode(new ThinkingNode());     // 并行：LLM 思考中
fsm.AddParallelNode(new EmotionNode());      // 并行：情绪系统
fsm.Transition("WalkState");                 // 主状态切换，并行不受影响
```

---

### [P1-3] EventManager：监听器反向遍历 + Remove 双重扫描

**文件**：`Assets/Plugins/ZEngine/Runtime/Manager/Manager.Event/EventManager.cs`

**问题 1**：`SendMessage` 从 `listeners.Last` 向 `Previous` 遍历，监听器按**注册逆序**执行，与观察者模式惯例不符，容易埋下顺序依赖 Bug。

**问题 2**：`RemoveListener` 先调 `Contains`（O(n)）再调 `Remove`（O(n)），共两次全量扫描。`LinkedList.Remove` 返回 bool，不存在时不抛异常，无需 Contains 前置检查。

**修复**：
```csharp
// Before：反向遍历
var currentNode = listeners.Last;
while (currentNode != null)
{
    currentNode.Value.Invoke(message);
    currentNode = currentNode.Previous;
}

// After：正向遍历，且保存 next 防止遍历中删除导致迭代器失效
var currentNode = listeners.First;
while (currentNode != null)
{
    var next = currentNode.Next;
    currentNode.Value.Invoke(message);
    currentNode = next;
}

// Before：O(2n) 双扫描
if (_listeners[type].Contains(listener))
    _listeners[type].Remove(listener);

// After：O(n) 单次
_listeners[type].Remove(listener);
```

---

## P2 — 稳定性改进

---

### [P2-1] ResourceManager：路径拼接无验证

**文件**：`Assets/Plugins/ZEngine/Runtime/Manager/Manager.Resource/ResourceManager.cs`

**问题**：所有加载方法直接 `_locationRoot + location`，传入 null 或空字符串时得到无意义路径，YooAsset 报错堆栈不直观。

**修复**：提取 `BuildLocation(string location)` 私有方法，在入口统一校验，所有加载方法改用此方法。

```csharp
private string BuildLocation(string location)
{
    if (string.IsNullOrWhiteSpace(location))
        throw new ArgumentException("资源路径不能为空", nameof(location));
    return _locationRoot + location;
}
```

---

### [P2-2] 日志系统统一（ZEngineLog ↔ LogManager）

**文件**：
- `Assets/Plugins/ZEngine/Runtime/Core/ZEngineLog.cs`
- `Assets/Plugins/ZEngine/Runtime/Manager/Manager.Log/LogManager.cs`

**问题**：`ZEngineLog`（Log/Warning/Error/Exception）和 `LogManager`（Verbose/Debug/Info/Warning/Error/Fatal）并存，同一条日志可能走不同通道，BqLog 持久化系统收不到框架内部日志。

**修复**：

1. `ZEngineLog` 新增 `SetCallback`（替换而非追加），供 LogManager 接管：

```csharp
public static void SetCallback(Action<ELogLevel, string> callback)
{
    _callback = callback;  // 替换，避免双写
}
```

2. `LogManager.OnInit` 末尾调用 `SetCallback`，接管 ZEngineLog 输出路由到 LogManager：

```csharp
ZEngineLog.SetCallback((level, msg) =>
{
    switch (level)
    {
        case ELogLevel.Log:       Info(msg);    break;
        case ELogLevel.Warning:   Warning(msg); break;
        case ELogLevel.Error:     Error(msg);   break;
        case ELogLevel.Exception: Fatal(msg);   break;
    }
});
```

**效果**：LogManager 初始化前，ZEngineLog → Unity Debug（ZEngineMain 注册的回调）；初始化后，ZEngineLog → LogManager → Unity Debug + BqLog，全链路统一。

---

---

## 二次审查补丁（2026-05-13）

---

### [Fix-B1] ObjectPoolManager：null 检查对象错误导致 NullReferenceException

**文件**：`Assets/Plugins/ZEngine/Runtime/Manager/Manager.Pool/ObjectPoolManager.cs`

**问题**：`OnInit` 第 60-63 行先用 `as` 做类型转换，但随后检查的是 `param == null` 而非 `parameters == null`。若传入非 null 但类型错误的对象，转换结果 `parameters` 为 null 但不触发检查，第 63 行 `parameters.DefaultMaxCapacity` 直接 NullReferenceException。

```csharp
// Before（检查错了对象）
CreateParameters parameters = param as CreateParameters;
if (param == null)  // BUG：param 不为 null，永不触发
    throw ...;
if (parameters.DefaultMaxCapacity < ...)  // NullReferenceException

// After（检查转换结果）
if (parameters == null)
    throw new Exception("需传入 CreateParameters 类型");
```

---

### [Fix-B2] Timer：两个结束条件同帧触发时 Kill() / 回调被执行两次

**文件**：`Assets/Plugins/ZEngine/Runtime/Manager/Manager.Timer/Timer.cs`

**问题**：`Update()` 中先检查 `_durationTime` 是否到期，再检查 `_maxTriggerCount` 是否到达，两个条件在同一帧都满足时会各自调用一次 `Kill()`，`CallBack` 被执行两次。

```csharp
// Before：两个条件各自独立 Kill()
if (_durationTime > 0 && _durationTimer >= _durationTime) Kill();
if (_maxTriggerCount > 0) { _triggerCount++; if (...) Kill(); }

// After：命中第一个条件即 return，确保每帧只 Kill 一次
if (_durationTime > 0 && _durationTimer >= _durationTime)
{ Kill(); return true; }
if (_maxTriggerCount > 0)
{ _triggerCount++; if (_triggerCount >= _maxTriggerCount) { Kill(); return true; } }
```

---

### [Fix-B3] UUIManager.OpenViewAsync：GetComponent 未判空导致 NullReferenceException

**文件**：`Assets/Plugins/ZEngine/Runtime/Manager/Managet.UI/UGUI/UUIManager.cs`

**问题**：异步回调中 `obj.Go.GetComponent<T>()` 若 Prefab 上不存在该组件则返回 null，下一行直接调用 `view.Initialize()` 导致 NullReferenceException。同步版 `OpenViewSync` 已有 null 检查 + AddComponent 兜底，异步版遗漏了。

```csharp
// Before
T view = obj.Go.GetComponent<T>();
view.Initialize();  // view 可能为 null

// After（与 OpenViewSync 保持一致）
T view = obj.Go.GetComponent<T>();
if (view == null)
    view = obj.Go.AddComponent<T>();
view.Initialize();
```

同时移除了 `UUIManager.cs` 中误引入的 `using FairyGUI;`（UGUI 管理器不应依赖 FairyGUI 命名空间）。

---

## 修复汇总

| 编号 | 优先级 | 文件 | 问题 |
|------|--------|------|------|
| P0-1 | P0 | ZEngineMain.cs | `_isDirty` 加 volatile |
| P0-2 | P0 | ManagerSingleton.cs | 未初始化抛异常而非返回 null |
| P0-3 | P0 | TcpChannel.cs | 心跳计时器 `-=` 修正精度 |
| P1-1 | P1 | TcpChannel.cs | 重连指数退避 + 随机抖动 |
| P1-2 | P1 | FiniteStateMachine.cs | 并行状态节点支持 |
| P1-3 | P1 | EventManager.cs | 正向遍历 + Remove 去除重复扫描 |
| P2-1 | P2 | ResourceManager.cs | 路径统一校验 |
| P2-2 | P2 | ZEngineLog.cs + LogManager.cs | 两套日志系统桥接统一 |
| Fix-B1 | P0 | ObjectPoolManager.cs | null 检查对象错误（`param` → `parameters`） |
| Fix-B2 | P0 | Timer.cs | 双条件同帧触发 Kill() 两次，回调重复执行 |
| Fix-B3 | P1 | UUIManager.cs | OpenViewAsync GetComponent 未判空 + 移除 FairyGUI 误引用 |
| Fix-B4 | P0 | UUILayer.cs | Canvas 渲染模式 WorldSpace → ScreenSpaceOverlay；层级容器缺 RectTransform |
| Fix-B5 | P0 | UUIManager.cs + UBaseView.cs | 异步路径 PoolHandle 丢失导致关闭时 Destroy 池化对象而非 Restore |
| Fix-B6 | P0 | UUIManager.cs | 单例检查在 Instantiate 之后，浪费完整加载+初始化开销 |
| Fix-B7 | P1 | UUIManager.cs + MVC/UIViewAttribute.cs | Location 改为 UIViewAttribute 特性，消除运行时反射 NRE 隐患 |
| Fix-B8 | P1 | UUIManager.cs | 新增全屏遮挡自动隐藏（RefreshVisibility）；新增 OpenViewAsyncAwait / HasView；GC 优化（缓存 ToList） |
| Fix-B9 | P2 | MVC/UBaseModel.cs | Initialize / OnRelease 由 abstract 改为 virtual，减少不必要空实现 |

---

## [2026-05-14] UGUI 模块重构详情

### Fix-B4：Canvas 渲染模式错误 + 层级容器缺 RectTransform

**文件**：`UUILayer.cs`

```csharp
// Before（Bug：WorldSpace 导致 CanvasScaler 失效、UI 位置以世界单位计算）
canvas.renderMode = RenderMode.WorldSpace;
canvas.worldCamera = Camera.main;  // 初始化时 Camera.main 可能为 null

// After
canvas.renderMode = RenderMode.ScreenSpaceOverlay;
canvas.sortingOrder = 0;

// 层级容器补充 RectTransform（Before 是普通 GameObject，UGUI 子节点布局异常）
var rt = go.AddComponent<RectTransform>();
rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
rt.offsetMin = rt.offsetMax = Vector2.zero;
```

---

### Fix-B5：异步路径对象池关闭 Bug

**文件**：`UUIManager.cs`、`MVC/UBaseView.cs`

```csharp
// Before：UBaseView.OnRelease() 无论什么路径一律 Destroy，销毁了池化对象
public virtual void OnRelease()
{
    Destroy(this.gameObject); // Bug：破坏了对象池
}

// After：判断是否有 PoolHandle（异步路径），有则 Restore，否则 Destroy
internal SpawnGameObject PoolHandle { get; set; }

public virtual void OnRelease()
{
    _eventGroup.RemoveAllListener();
    if (PoolHandle != null) { PoolHandle.Restore(); PoolHandle = null; }
    else { this.transform.SetParent(null); Destroy(this.gameObject); }
}

// UUIManager.OpenViewAsync 赋值
spawnObj.Completed += (spawn) => { ...; view.PoolHandle = spawn; ... };
```

---

### Fix-B6：单例检查时机

**文件**：`UUIManager.cs`

```csharp
// Before：先加载资源+Instantiate，再检查单例——已存在则白白浪费加载开销
var handle = Resource.LoadSync(location);
var obj = Instantiate(handle.AssetObject);
var view = obj.GetComponent<T>();
view.Initialize(); view.OnComplete();
if (view.IsSingleton) { var existed = FindByType(type); if (existed != null) { view.OnRelease()... } }

// After：先查字典，已存在则 BringToFront 直接返回，不加载资源
if (attr.IsSingleton)
{
    var existed = FindViewByType(type, attr.Layer);
    if (existed != null) { BringToFront(existed); onViewOpened?.Invoke(existed); return; }
}
// 通过检查后才加载
```

---

### Fix-B7：Location 机制改为 UIViewAttribute

**文件**：`MVC/UIViewAttribute.cs`（新增）、`UUIManager.cs`、`MVC/UBaseView.cs`

```csharp
// Before：通过反射读取子类 static 字段（脆弱，无编译期检查，忘写则运行时 NRE）
var locationField = type.GetField("Location");
string location = locationField.GetValue(null) as string; // 可能 null

// After：标准 Attribute，编译期可见
[UIViewAttribute("UI/MainPanel", UUILayer.Window_Layer, isSingleton: true)]
public class MainPanelView : UBaseView { ... }

// Manager 读取方式
var attr = Attribute.GetCustomAttribute(type, typeof(UIViewAttribute)) as UIViewAttribute;
if (attr == null) throw new Exception($"View [{type.Name}] 缺少 [UIViewAttribute]");
```

---

## [2026-05-19] UGUI 模块 Bug 修复

### UGUI-1：UILayer.Initialize 未创建 EventSystem

**文件**：`Assets/Plugins/ZEngine/Runtime/Manager/Managet.UI/UGUI/Layer/UUILayer.cs`

**问题**：代码动态创建的 Canvas 不会像编辑器那样自动附带 EventSystem，导致所有按钮点击和 UI 事件无法响应。

**修复**：在 `Initialize` 末尾检测 `EventSystem.current == null`，若缺失则自动创建：

```csharp
if (EventSystem.current == null)
{
    var esGo = new GameObject("EventSystem");
    esGo.transform.SetParent(root != null ? root.transform : null);
    esGo.AddComponent<EventSystem>();
    esGo.AddComponent<StandaloneInputModule>();
}
```

---

### UGUI-2：BringToFront 依赖 Dictionary 插入顺序导致层级错乱

**文件**：`Assets/Plugins/ZEngine/Runtime/Manager/Managet.UI/UGUI/UUIManager.cs`

**问题**：`_viewDic` 内层使用 `Dictionary<string, UBaseView>`，`BringToFront` 通过先 Remove 再 Add 让 View 排到字典末尾，但 `Dictionary` 不保证枚举顺序，导致 `SortLayer` 和 `RefreshVisibility` 的层级逻辑不可靠。

**修复**：将内层集合改为 `List<UBaseView>`，顺序明确（末尾 = 最高 sibling index = 最顶层），全部相关方法同步更新：

```csharp
// Before
private Dictionary<UUILayer, Dictionary<string, UBaseView>> _viewDic;

// After
private Dictionary<UUILayer, List<UBaseView>> _viewDic;

// BringToFront：List.Remove + Add 末尾，顺序可控
var list = _viewDic[layer];
if (list.Remove(view)) { list.Add(view); SortLayer(layer); RefreshVisibility(); }
```

---

### UGUI-3：CloseView\<T\> 关闭的是最后一个匹配项而非第一个

**文件**：`UUIManager.cs`

**问题**：内层循环找到匹配 View 后不 break，最终 `target` 是字典中最后一个同类型 View，对非单例场景行为与预期不符。

**修复**：找到第一个匹配即 break：

```csharp
if (list[j].GetType() == type) { target = list[j]; break; }
```

---

### UGUI-4：UILayer.LayerDic 重复初始化抛 ArgumentException

**文件**：`UUILayer.cs`

**问题**：`Initialize` 直接 `LayerDic.Add`，若被调用两次（热重载、场景切换等）会因 key 已存在抛 `ArgumentException`。

**修复**：`Initialize` 开头加 `LayerDic.Clear()`。

---

### UGUI-5：OnRelease 未清空 \_data 引用

**文件**：`Assets/Plugins/ZEngine/Runtime/Manager/Managet.UI/UGUI/MVC/UBaseView.cs`

**问题**：`OnRelease` 将对象归还对象池后，`_data` 字段仍持有旧 Model 引用，直到下次 `InitializeView` 赋值前都是悬空引用。

**修复**：

```csharp
public virtual void OnRelease()
{
    _eventGroup.RemoveAllListener();
    OnDataChanged = null;
    _data = null;  // 新增：清空悬空引用
    _childComponents.Clear();
    ...
}
```

---

### UGUI-6：CloseAll 未调用 RefreshVisibility

**文件**：`UUIManager.cs`

**问题**：`CloseAll` 关闭所有 View 后未刷新可见性，若后续有逻辑查询 Active 状态可能读到脏数据。

**修复**：`CloseAll` 末尾加 `RefreshVisibility()`（此时所有 List 已清空，调用为空操作，无性能影响）。

---

### UGUI-7：对象池复用的 View CanRemoved 未重置

**文件**：`UUIManager.cs`

**问题**：`InitializeView` 重置了 `LayerType`、`IsSingleton`、`IsFullScreen`，但遗漏了 `CanRemoved`。对象池复用时若上次关闭前 `CanRemoved = true`，重新打开后 `OnUpdate` 下一帧立即将其再次关闭（View 打开即关闭）。

**修复**：

```csharp
view.CanRemoved = false; // 对象池复用时必须重置
```

---

### UGUI-8：OpenViewAsyncAwait 在异步加载失败时永久挂起

**文件**：`UUIManager.cs`

**问题**：`OpenViewAsync` 回调在 `spawn.Go == null` 时只记录 Log 并 return，不调用 `onViewOpened`，导致 `OpenViewAsyncAwait` 创建的 `UniTaskCompletionSource` 永远不被 resolve，`await` 死锁。

**修复**：加载失败时调用 `onViewOpened?.Invoke(null)`，让 tcs 正常完成，调用方收到 null 即可判断失败。

---

### UGUI-9：OpenViewAsync 并非真正的 async/await

**文件**：`UUIManager.cs`

**问题**：`OpenViewAsync` 是 `void` 方法 + 回调，`OpenViewAsyncAwait` 是对其的二次 UniTask 包装，架构倒置且 API 冗余。

**修复**：将 `OpenViewAsync<T>` 直接改为 `async UniTask<T>`，移除 `OpenViewAsyncAwait`，新增私有 `SpawnAsync` 辅助方法将回调包装为可 await 的 UniTask：

```csharp
// Before：void + 回调 + 再包装
public void OpenViewAsync<T>(... Action<UBaseView> onViewOpened)
public async UniTask<T> OpenViewAsyncAwait<T>(...)  // 多余包装层

// After：直接 async UniTask<T>
public async UniTask<T> OpenViewAsync<T>(UBaseModel model = null) where T : UBaseView
{
    var spawn = await SpawnAsync(attr.Location);  // 等待池加载
    ...
    return view;
}

private static UniTask<SpawnGameObject> SpawnAsync(string location)
{
    var tcs = new UniTaskCompletionSource<SpawnGameObject>();
    var handle = ObjectPoolManager.Instance.Spawn(location);
    handle.Completed += spawn => tcs.TrySetResult(spawn);
    return tcs.Task;
}
```

---

## [2026-05-19] AudioManager 新增位置音效

### Audio-1：新增基于 XY 距离的位置音效（PlaySoundAtPosition）

**文件**：
- `Assets/Plugins/ZEngine/Runtime/Manager/Manager.Audio/AudioManager.cs`
- `Assets/Plugins/ZEngine/Runtime/Manager/Manager.Audio/IAudioManager.cs`

**背景**：原音频管理器每个 `EAudioLayer` 只有一个共享 `AudioSource`，所有音效音量相同，不支持距离衰减。2D 正交相机场景下无法使用 Unity 内置 `spatialBlend`（Z 轴差异导致计算偏差），需要手动 XY 距离计算。

**新增内容**：

| 新增 | 说明 |
|------|------|
| `PositionalSoundHandle` 内部类 | 管理临时 AudioSource，按 XY 距离线性计算音量，播完自动销毁 |
| `_soundListener: Transform` | 监听器（通常为主摄像机），null 时退化为全音量 |
| `_positionalSounds: List<PositionalSoundHandle>` | 活跃句柄列表 |
| `SetSoundListener(Transform)` | 注册监听器 |
| `PlaySoundAtPosition(location, worldPos, minDist, maxDist)` | 在世界坐标播放音效，距离衰减 |
| `OnUpdate` 更新 | 每帧刷新音量 + 清理已结束句柄 |
| `ReleaseAll` / `Release(Sound)` 更新 | 释放时同步销毁所有位置音效 |

**衰减公式**：

```
t = Clamp01((distance - minDistance) / (maxDistance - minDistance))
volume = baseVolume × (1 - t)
// distance ≤ minDistance → 全音量
// distance ≥ maxDistance → 静音
// 中间线性插值
```

**使用示例**：

```csharp
// 初始化时注册监听器（一次即可）
AudioManager.Instance.SetSoundListener(Camera.main.transform);

// NPC 发出脚步声（5 格内全音量，30 格外静音）
AudioManager.Instance.PlaySoundAtPosition("Sounds/footstep", npc.transform.position, 5f, 30f);
```

---

## 本次修改汇总（2026-05-19）

| 编号 | 优先级 | 文件 | 问题 |
|------|--------|------|------|
| UGUI-1 | P0 | UUILayer.cs | 缺少 EventSystem，UI 事件全部失效 |
| UGUI-2 | P0 | UUIManager.cs | Dictionary 顺序不保证，BringToFront 层级错乱 → 改 List |
| UGUI-3 | P1 | UUIManager.cs | CloseView\<T\> 关闭最后一个而非第一个匹配项 |
| UGUI-4 | P1 | UUILayer.cs | 重复初始化 LayerDic 抛 ArgumentException |
| UGUI-5 | P2 | UBaseView.cs | OnRelease 未清空 \_data 悬空引用 |
| UGUI-6 | P2 | UUIManager.cs | CloseAll 后未调 RefreshVisibility |
| UGUI-7 | P0 | UUIManager.cs | CanRemoved 对象池复用未重置，View 打开即关闭 |
| UGUI-8 | P0 | UUIManager.cs | 异步加载失败时 await 永久死锁 |
| UGUI-9 | P1 | UUIManager.cs | OpenViewAsync 重构为真正 async UniTask\<T\>，移除冗余包装层 |
| Audio-1 | 新功能 | AudioManager.cs / IAudioManager.cs | 新增位置音效（XY 距离线性衰减） |

---

## [2026-05-20] HttpManager 模块修复

### Http-1：`Timeout` 枚举值从未被命中

**文件**：`Assets/Plugins/ZEngine/Runtime/Manager/Manager.Http/HttpManager.cs`

**问题**：`CreateResponse` 将所有 `ConnectionError` 统一返回 `HttpResult.NetworkError`，但 Unity 超时后 `result` 也是 `ConnectionError`，`error` 消息含 "timeout"。导致 `HttpResult.Timeout` 枚举值永远不会被使用，调用方无法区分网络断开与超时。

**修复**：在 `ConnectionError` 分支内先检查 error 消息：

```csharp
// Before
if (request.result == UnityWebRequest.Result.ConnectionError)
    return HttpResponse.Fail(HttpResult.NetworkError, statusCode, request.error);

// After
if (request.result == UnityWebRequest.Result.ConnectionError)
{
    if (request.error != null && request.error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
        return HttpResponse.Fail(HttpResult.Timeout, statusCode, request.error);
    return HttpResponse.Fail(HttpResult.NetworkError, statusCode, request.error);
}
```

---

### Http-2：取消请求返回 `Unknown` 而非 `Cancelled`

**文件**：`HttpManager.cs`（`SendRequestAsync`、`PostFormAsync`、`DownloadAsync`）

**问题**：`OperationCanceledException`（CancellationToken 触发）被宽泛的 `catch (Exception)` 捕获，统一返回 `HttpResult.Unknown`。调用方无法区分"主动取消"与"真实错误"，导致错误日志/重试逻辑误判。

**修复**：新增 `Cancelled = 6` 枚举值，并在三处位置单独捕获：

```csharp
// HttpResult.cs — 新增
Cancelled = 6,  // 请求被 CancellationToken 取消

// HttpManager.cs — SendRequestAsync / PostFormAsync（统一修复）
catch (OperationCanceledException)
{
    return HttpResponse.Fail(HttpResult.Cancelled, 0, "请求已取消");
}
catch (Exception ex)
{
    return HttpResponse.Fail(HttpResult.Unknown, 0, ex.Message);
}

// DownloadAsync（原已有 catch，改正返回值）
// Before
return HttpResponse.Fail(HttpResult.Unknown, request.responseCode, "请求已取消");
// After
return HttpResponse.Fail(HttpResult.Cancelled, request.responseCode, "请求已取消");
```

---

### Http-3：`GetFullUrl` 在 URL 和 BaseUrl 均为空时无保护

**文件**：`HttpManager.cs`，`GetFullUrl` 方法

**问题**：`url` 为空时直接返回 `BaseUrl`，若 `BaseUrl` 也未设置则返回空字符串，`UnityWebRequest` 以空地址发请求，底层错误堆栈不直观，难以定位根因。

**修复**：提前 fail-fast，抛出 `ArgumentException`：

```csharp
// Before
if (string.IsNullOrEmpty(url))
    return BaseUrl;

// After
if (string.IsNullOrEmpty(url))
{
    if (string.IsNullOrEmpty(BaseUrl))
        throw new ArgumentException("[HttpManager] URL 和 BaseUrl 均为空，无法构建请求地址");
    return BaseUrl;
}
```

---

## 本次修改汇总（2026-05-20）

| 编号 | 优先级 | 文件 | 问题 |
|------|--------|------|------|
| Http-1 | P1 | HttpManager.cs | `Timeout` 枚举从不命中，超时与断网无法区分 |
| Http-2 | P1 | HttpResult.cs + HttpManager.cs | 取消请求归入 Unknown，新增 Cancelled 枚举并分类捕获 |
| Http-3 | P2 | HttpManager.cs | 空 URL fail-fast 保护，提前抛 ArgumentException |
