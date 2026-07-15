# FuncModule 功能模块使用文档

## 目录

1. [Buff 模块](#1-buff-模块)
   - [1.1 概述](#11-概述)
   - [1.2 架构与文件结构](#12-架构与文件结构)
   - [1.3 快速开始](#13-快速开始)
   - [1.4 配置 BuffData](#14-配置-buffdata)
   - [1.5 自定义 Buff 类型](#15-自定义-buff-类型)
   - [1.6 Buff 生命周期](#16-buff-生命周期)
   - [1.7 让角色支持 Buff（实现 IBuffHandler）](#17-让角色支持-buff实现-ibuffhandler)
   - [1.8 BuffManager API](#18-buffmanager-api)
   - [1.9 多次添加同种 Buff 的处理策略](#19-多次添加同种-buff-的处理策略)
   - [1.10 互斥机制](#110-互斥机制)
   - [1.11 BuffTag 标签系统](#111-bufftag-标签系统)
   - [1.12 属性刷新（ChaAttrRecheck）](#112-属性刷新chaattrrecheck)
   - [1.13 注意事项与已知限制](#113-注意事项与已知限制)

---

## 1. Buff 模块

### 1.1 概述

Buff 模块提供一个通用的 Buff/状态效果管理框架，支持：

- 基于 `BuffType` 的工厂自动注册，通过特性（Attribute）挂载子类
- 完整的生命周期：Awake → Start → Update → Remove → Destroy
- 持续时间计时 + 周期性触发（Tick）双计时机制
- 多次添加同种 Buff 的多种叠加策略（重置时间 / 叠层 / 共存）
- Buff 互斥（Mutex）判断
- 基于位运算的 `BuffTag` 标签系统
- 按优先级排序的 Buff 列表

> 命名空间：`Hotfix.FuncModule`
> 模块位置：`Assets/GameScripts/Hotfix/FuncModule/Buff/`

### 1.2 架构与文件结构

```
Buff/
├── BuffManager.cs            # Buff 管理器（BehaviourSingleton），驱动所有 Buff 更新
├── DefaultBuff.cs            # 默认 Buff 实现，无特殊效果的兜底类型
├── Interface/
│   ├── IBuffBase.cs          # Buff 生命周期接口
│   └── IBuffHandler.cs       # Buff 拥有者接口（角色/单位实现）
├── Other/
│   ├── BuffEnumType.cs       # 枚举：BuffMutipleAddType / BuffTag / BuffType
│   ├── BuffFactory.cs        # 工厂：按 BuffType 反射创建 Buff 实例
│   ├── BuffTagExtensions.cs  # BuffTag 位运算扩展方法
│   └── BuffTypeAttribute.cs  # [BuffType(...)] 特性
└── Structs/
    ├── BuffBase.cs           # Buff 抽象基类，实现完整生命周期与计时
    ├── BuffConfig.cs         # ScriptableObject：批量收集 BuffData
    └── BuffData.cs           # ScriptableObject：单条 Buff 配置
```

核心设计：

- **BuffManager** 是单例（`BehaviourSingleton<BuffManager>`），在 `FixedUpdate` 中统一驱动所有已注册 handler 的 Buff 更新。
- **BuffFactory** 在静态构造时扫描 `HotUpdate` 程序集，把所有带 `[BuffType]` 特性、继承 `BuffBase` 的非抽象类注册到 `BuffType → Type` 映射表。
- **BuffBase** 是抽象基类，子类只需实现 `OnBuffTickEffect()`（周期性效果）。其余生命周期方法均为 `virtual`，可按需 override。

### 1.3 快速开始

#### 最简工作流

```
1. 创建 BuffData 配置（右键 Create → CreateCustomSO → BuffData）
2. （可选）用 BuffConfig 的 [收集] 按钮批量收集，或通过配置表加载
3. 让需要挂 Buff 的角色实现 IBuffHandler
4. 调用 BuffManager.Instance.AddBuff(handler, buffId, caster) 添加 Buff
5. 在自定义 Buff 子类里实现 OnBuffTickEffect() 写效果逻辑
```

#### 添加一个 Buff（示例）

```csharp
// role 实现了 IBuffHandler
BuffManager.Instance.AddBuff(role, buffId: 1001, caster: attacker);
```

#### 移除一个 Buff

```csharp
// 移除该角色身上所有 id == 1001 的 Buff
BuffManager.Instance.RemoveBuff(role, buff_id: 1001);
```

### 1.4 配置 BuffData

`BuffData` 是一个 ScriptableObject，每条配置描述一种 Buff 的静态属性。

**创建方式**：Project 窗口右键 → `Create → CreateCustomSO → BuffData`

| 字段 | 类型 | 说明 |
|---|---|---|
| `buff_id` | int | 唯一 ID，重复会抛异常 |
| `buff_name` | string | 名称 |
| `buffType` | BuffType | Buff 类型，决定工厂创建哪个子类（必须与 `[BuffType]` 特性对应） |
| `icon` | string | 图标地址 |
| `description` | string | 描述 |
| `duration` | float | 持续时间。**≤ 0 表示永久 Buff**（不计时） |
| `tickInterval` | float | 周期性触发间隔。**≤ 0 不触发 Tick**（避免死循环） |
| `priority` | int | 优先级，越小越靠前执行。排序用 |
| `canAddLayer` | bool | 是否支持叠加层数 |
| `layer` | int | 初始层数 |
| `mutipleAddType` | BuffMutipleAddType | 多次添加同种 Buff 时的处理策略（见 [1.9](#19-多次添加同种-buff-的处理策略)） |
| `tag` | BuffTag | 类型标签（位运算，可多选） |
| `mutexBuffs` | BuffType[] | 互斥的 Buff 类型列表（见 [1.10](#110-互斥机制)） |

#### 批量收集（编辑器）

`BuffConfig`（右键 `Create → CreateCustomSO → BuffConfig`）提供 **[收集]** 按钮，会扫描项目中所有 `BuffData` 资源并按 `buff_id` 排序后填入 `buffDatas` 列表。Runtime 通过 SO 模式加载时使用。

#### 配置加载方式

`BuffManager.Start()` 根据加载模式初始化 Buff 数据字典：

- **SO 模式**：从 `GameAssetPaths.Config_Buff` 异步加载 `BuffConfig` 资源

> ⚠️ 会把 BuffData 按 `buff_id` 存入字典，重复 ID 会抛异常。

### 1.5 自定义 Buff 类型

通过两步注册一个新 Buff 类型：

**第一步**：在 `BuffType` 枚举中新增类型（`Other/BuffEnumType.cs`）

```csharp
public enum BuffType
{
    Default,
    Heal,
    Damage,
    Roll,
    Poison,   // 新增
}
```

**第二步**：写一个继承 `BuffBase` 的子类，用 `[BuffType]` 特性标注

```csharp
using UnityEngine;

namespace Hotfix.FuncModule
{
    [BuffType(BuffType.Poison)]
    public class PoisonBuff : BuffBase
    {
        public PoisonBuff(IBuffHandler owner, GameObject caster, BuffData buffData)
            : base(owner, caster, buffData) { }

        // 必须实现：每次 Tick 触发的效果
        public override void OnBuffTickEffect()
        {
            // 每 tickInterval 秒对 Owner 造成一次伤害
            // 通过 Owner / Caster 拿到战斗系统施加效果
            Debug.Log($"{Buff_Name} 触发，当前层数 {Layer}");
        }

        // 可选：override 其它生命周期方法
        public override void OnBuffStart()
        {
            base.OnBuffStart();
            // Buff 开始时的额外逻辑（如播放特效）
        }
    }
}
```

> ⚠️ 子类**必须**提供 `(IBuffHandler owner, GameObject caster, BuffData buffData)` 构造函数，因为 `BuffFactory` 通过 `Activator.CreateInstance(type, handler, caster, buffData)` 反射创建实例。

完成后，将对应 `BuffData` 资源的 `buffType` 字段设为 `Poison`，工厂即可自动创建 `PoisonBuff` 实例。未注册的类型会回退到 `DefaultBuff` 并打印警告。

### 1.6 Buff 生命周期

`BuffBase` 实现的完整生命周期（均为 `virtual`，可 override）：

| 方法 | 触发时机 | 默认行为 |
|---|---|---|
| `Initialize` | 构造时自动调用 | 绑定 Owner / Caster / BuffData |
| `OnBuffAwake` | Buff 加入列表后 | 读取配置初始化 duration/tickInterval/layer，`_isEffective = true` |
| `OnBuffStart` | Awake 之后 | 触发 `OnBuffAdded` 回调，开启 Tick 计时 |
| `OnBuffUpdate` | 每个 FixedUpdate | 推进持续时间和 Tick 计时；到期则扣层，层数归零自动销毁 |
| `OnBuffTickEffect` | 每个 tickInterval 周期 | **抽象方法，子类必须实现** |
| `OnBuffRemove` | Buff 被移除时 | 停止 Tick，触发 `OnBuffRemoved` 回调 |
| `OnBuffDestroy` | Buff 彻底销毁时 | 清理状态，清空回调委托链 |

**Buff 消失的三条路径**（均已统一调用 `OnBuffRemove` + `OnBuffDestroy`）：

1. **自然过期**：`OnBuffUpdate` 内持续时间耗尽且层数归零 → 自动 Remove + Destroy，由 `BuffManager.FixedUpdate` 从列表移除
2. **主动移除**：调用 `BuffManager.RemoveBuff` / `RemoveBuffByTag` / `RemoveBuffByType`
3. **管理器销毁**：`BuffManager.OnDestroy`（如场景切换）

> 💡 自然过期时 `OnBuffUpdate` 内部已调用销毁，外部 `FixedUpdate` 只负责把它从列表移除，不会重复销毁。

### 1.7 让角色支持 Buff（实现 IBuffHandler）

任何需要挂载 Buff 的角色/单位都要实现 `IBuffHandler`：

```csharp
using System.Collections.Generic;
using UnityEngine;
using Hotfix.FuncModule;

public class Character : MonoBehaviour, IBuffHandler
{
    // 必须提供：Buff 列表（BuffManager 会读写它）
    public List<BuffBase> Buffs { get; } = new List<BuffBase>();

    // 可选：转发到 BuffManager（方便调用）
    public void AddBuff(int buff_id, GameObject caster)
        => BuffManager.Instance.AddBuff(this, buff_id, caster);

    public void RemoveBuff(int buff_id)
        => BuffManager.Instance.RemoveBuff(this, buff_id);
}
```

> ⚠️ `Buffs` 列表由 `BuffManager` 在 `FixedUpdate` 中反向遍历并原地修改，实现方**不要**在其它地方并发修改这个列表，否则可能与管理器迭代冲突。

### 1.8 BuffManager API

`BuffManager.Instance` 提供以下公开方法：

| 方法 | 说明 |
|---|---|
| `AddBuff(handler, buff_Id, caster = null)` | 添加 Buff，返回创建/命中的 BuffBase；失败返回 null |
| `RemoveBuff(handler, buff_id)` | 移除该 handler 身上所有指定 ID 的 Buff |
| `RemoveBuffByTag(handler, tag)` | 移除所有拥有指定标签的 Buff（净化用） |
| `RemoveBuffByType(handler, buffType)` | 移除指定类型的所有 Buff |
| `HasBuff(handler, buff_id)` | 是否拥有指定 ID 的 Buff |
| `HasBuffWithTag(handler, tag)` | 是否拥有指定标签的 Buff |
| `GetBuffData(buff_id)` | 获取 Buff 的静态配置数据 |

#### 回调监听

`BuffBase` 暴露两个 public 委托，可在 Buff 创建后订阅（Buff 销毁时会自动清空，无需手动取消订阅）：

```csharp
var buff = BuffManager.Instance.AddBuff(role, 1001, attacker);
buff.OnBuffAdded += (handler, b) => Debug.Log("Buff 已添加");
buff.OnBuffRemoved += (handler, b) => Debug.Log("Buff 已移除");
```

### 1.9 多次添加同种 Buff 的处理策略

通过 `BuffData.mutipleAddType`（`BuffMutipleAddType` 枚举）配置：

| 类型 | 行为 |
|---|---|
| `RestTime` | 重置已存在 Buff 的持续时间计时（不叠层） |
| `AddLayer` | 在当前层数基础上 +1（需 `canAddLayer = true`） |
| `AddLayerAndResetTime` | 层数 +1 并重置时间 |
| `AddCount` | 创建一个**独立的**新 Buff 实例，与原有并存互不影响 |

> ⚠️ `AddLayer` / `AddLayerAndResetTime` 要求 `canAddLayer = true`，否则 `ModifyLayer` 会打印错误并拒绝叠加。
> 💡 判重逻辑：`AddBuff` 先用 `Find(x => x.Buff_Id == buff_Id)` 查找已存在的同 ID Buff。注意 `AddCount` 类型在二次添加时仍会命中"已存在"分支并走独立创建子流程，不会无脑叠层。

### 1.10 互斥机制

`BuffData.mutexBuffs`（`BuffType[]`）声明互斥关系。`BuffManager.CanAddBuff` 在添加前做**双向检查**：

1. 待添加 Buff 的 `mutexBuffs` 是否包含已存在 Buff 的类型
2. 已存在 Buff 的 `mutexBuffs` 是否包含待添加 Buff 的类型

任一成立则拒绝添加并打印 `无法添加Buff(互斥)` 警告。

```csharp
// 示例：中毒时无法被治疗（反过来治疗时也无法中毒）
// Heal 的 BuffData.mutexBuffs = [ BuffType.Poison ]
// Poison 的 BuffData.mutexBuffs = [ BuffType.Heal ]
```

### 1.11 BuffTag 标签系统

`BuffTag` 是位运算枚举，一个 Buff 可同时拥有多个标签：

```csharp
public enum BuffTag
{
    None      = 0,
    Buff      = 1 << 0,
    Debuff    = 1 << 1,
    Control   = 1 << 2,   // 控制（眩晕/冰冻等）
    Passive   = 1 << 3,   // 被动
    Trigger   = 1 << 4,   // 触发器
}
```

在 `BuffData.tag` 中配置（Inspector 支持多选）。运行时可用扩展方法判断：

```csharp
buff.HasTag(BuffTag.Control)              // BuffBase 实例方法
BuffTag.Control.HasTag(BuffTag.Debuff)    // 扩展方法（枚举本身）
someTag.AddTag(BuffTag.Control)           // 添加标签
someTag.RemoveTag(BuffTag.Control)        // 移除标签
```

典型用途：`RemoveBuffByTag(handler, BuffTag.Control)` 实现一次净化清除所有控制效果。

### 1.12 属性刷新（ChaAttrRecheck）

`BuffManager` 在每次 Add / Remove / 过期后都会调用 `ChaAttrRecheck(handler)` 刷新角色属性。**当前该方法为空实现**，需要业务层接入角色属性系统后补充（例如遍历 `handler.Buffs` 重新计算攻防速等属性）。

接入示例（伪代码）：

```csharp
private void ChaAttrRecheck(IBuffHandler handler)
{
    if (handler is Character cha)
    {
        cha.RecalculateAttributes(); // 遍历 cha.Buffs 重新计算
    }
}
```

### 1.13 注意事项与已知限制

- **驱动频率**：所有 Buff 在 `FixedUpdate`（默认 50Hz）中更新。大量低频 Buff 可能有性能开销，后续可考虑分级更新。
- **`ChaAttrRecheck` 未实现**：当前 Buff 不会实际影响角色属性，需业务层补全（见 [1.12](#112-属性刷新chaattrrecheck)）。
- **无暂停机制**：角色死亡/全局暂停时 Buff 仍会继续计时，如需暂停需在业务层扩展（如给 `BuffBase` 加 `SetPaused` 并在 `OnBuffUpdate` 早退）。
- **无免疫机制**：目前只有互斥（Mutex），没有"对某 tag/type 免疫"的能力，如 boss 免疫控制需后续扩展。
- **`GUID` 字段**：每个 Buff 实例会生成一个 GUID，当前未在查询/移除中使用，预留给未来按实例精确操作。
- **程序集名硬编码**：`BuffFactory` 扫描 `"HotUpdate"` 程序集，若热更程序集名变更需同步修改。
- **加载模式依赖**：`Start()` 中的 `AppConfig.BuffMode` / `LoadMode` 需业务层提供定义，否则该处会有编译错误。
