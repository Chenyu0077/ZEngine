# 塔与纸⼈实体系统 ⼿册

> ⾃动⽣成于 2026-07,覆盖 Project-E001 除 Pool 内部实现外的完整调⽤链路。
> 所有路径均相对于 `Assets/GameScripts/Hotfix/`。

---

## ⼀、架构总览

```
                     ┌─────────────┐
                     │ GameLauncher │ • ObjectPoolManager 注册
                     └──────┬──────┘
                            │
                    ┌───────▼────────┐
         ┌─────────┤  EntityManager  ├──────────┐
         │         │ (BehvrSingleton)│          │
         │         └───┬────────┬────┘          │
         │             │        │               │
    ┌────▼────────┐ ┌──▼──────┐ ┌──────▼───────────┐
    │ TowerFactory │ │WaveMgr │ │ TowerPlacement   │
    │ (静态反射)   │ │(Singleton)│ │ Bridge(Mono)     │
    └───┬──────────┘ └──┬─────┘ └──────┬───────────┘
        │               │              │
   ┌────▼───┐    ┌─────▼──────┐  ┌───▼───────────┐
   │Tower[] │    │EnemyEntity │  │ PlacedObjMgr   │
   │子类    │    │(纸人)     │  │ (建筑放置系统)  │
   └────────┘    └─────┬──────┘  └───┬───────────┘
                       │              │
              ┌────────▼───────┐      │
              │TargetingSystem │◄─────┘
              │  (静态索敌)    │
              └────────────────┘
  ┌────────────────────────────────────────────┐
  │              ObjectPoolManager             │
  │          (Spawn → Restore 池化)            │
  └────────────────────────────────────────────┘
```

---

## ⼆、核⼼类

### 2.1 EntityBase —— 实体基类

| 项⽬ | 值 |
|------|----|
| ⽂件 | `FuncModule/Entity/EntityBase.cs` |
| 继承 | `MonoBehaviour, IBuffHandler` |
| ⼦类 | `TowerEntity`, `EnemyEntity` |

**关键成员**

```csharp
// ⽣命状态
bool IsAlive { get; set; }
event Action<EntityBase> OnEntityDeath;  // OnDeath 触发⼀次,OnDespawn 清空

// 对象池句柄 (Spawn 时 Bind, Despawn 时 Restore)
internal SpawnGameObject _poolHandle;
void BindPoolHandle(SpawnGameObject h);

// ⼦类必须实现
abstract int EntityId { get; }
abstract EntityCamp Camp { get; }           // Tower / Enemy
abstract void RecalculateAttributes();
abstract void TakeDamage(float damage, EntityBase source);
void OnDeath();                             // 触发 OnEntityDeath + 清 Buffs
virtual void OnDespawn();                   // 清 Buffs + OnEntityDeath=null(不复触发)
```

### 2.2 TowerEntity —— 塔实体

| 项⽬ | 值 |
|------|----|
| ⽂件 | `FuncModule/Entity/Tower/TowerEntity.cs` |
| ⼦类 | `DrumTower`, `PeachSwordTower`, `TalismanTower`, `LanternTower` |

**等级与属性**

```csharp
int Level;                   // 1~5
TowerData Data;              // SO 静态配置
void LevelUp();              // 升级 + 触发 RecalculateAttributes + OnLevelChanged
float GetAttribute(type);    // 取最终运⾏时值 (基值×等级曲线 + Buff 修饰)
```

**攻击驱动 (EntityManager.FixedUpdate → TryAttack)**

```csharp
bool IsAreaAttack => false;  // Drum 覆写 = true
void TryAttack()             // 索敌圆 → IsAreaAttack? FindAllInCircle : FindFirstInCircle → 填 targetBuffer
void ExecuteAttack(targets); // ⼦类实现攻击结算 (取 targets[0] 或遍历)
void TickAttackCooldownInternal(dt);
```

**攻击冷却计算 (⼦类可覆盖)**

```csharp
float GetAttackCooldown()    // 优先 AttackFrequency → 1/freq; 次选 CooldownTime
float GetAttackRange()       // 优先 Range; 次选 ShockRadius; 兜底 BuffRange
```

**辅助塔光环**

```csharp
void ApplySupportAura();     // EntityManager 每帧调⽤,通知范围内友⽅塔 RecalculateAttributes
```

**暴击 (CompassTower 5 级质变调⽤)**

```csharp
void SetNextAttackCrit(multiplier);      // 标记下⼀次攻击必暴击
bool ConsumeNextAttackCrit();            // 攻击结算时调,true=本次暴击  false=普通
```

### 2.3 EnemyEntity —— 纸⼈实体

| 项⽬ | 值 |
|------|----|
| ⽂件 | `FuncModule/Entity/Enemy/EnemyEntity.cs` |
| EnemyType | Normal, Gale, Stack, Endure, Rain |
| EnemyTier | Normal, Elite, Boss |

**运⾏时数值**

```csharp
float MaxHp;                // baseHp × tierMultiplier
float CurrentHp;
float BaseMoveSpeed;        // 配置基值
float EffectiveMoveSpeed;   // 最终 = (BaseMoveSpeed+flat)×(1+percent) × Gale倍率
float Armor;
bool IsStunned;             // 眩晕时冻结 Tick (移动+再⽣)
```

**移动**

```csharp
void SetWaypoints(waypoints);       // WaveManager 调⽤,拷⻉缓存路径
void ClearWaypoints();
bool HasReachedDestination;         // waypointIndex >= count
event Action<EnemyEntity> OnReachedDestinationEvent;  // WaveManager 订阅 → 扣基地血
```

**技能⾏为**

| 类型 | 钩⼦ | 说明 |
|------|------|------|
| 堆叠 (Stack) | `OnDeathSplit()` → `OnSplitRequested` 事件 | 死亡时触发分裂,N个分裂体各持 `splitHpRatio」⾎量 |
| 抗压 (Endure) | `UpdateRegen(dt)` (Tick 内调) | HP < 阈值随时间回复,「IsHighPriorityTarget」标记集⽕ |
| ⾬蚀 (Rain) | `TakeDamage` → `OnHitStunReflect` | 受击概率反弹眩晕到攻击者 |
| 飓⻛ (Gale) | `GetGaleSpeedMultiplier()` | 移速额外乘 `galeSpeedMultiplier` |

```
Tick(dt):
  if(!IsAlive||IsStunned) return
  UpdateMovement(dt)
  UpdateRegen(dt)  // 仅 Endure
```

---

## 三、⽣成与池化链路

### 3.1 全局启动链

```
GameLauncher
  └─ ZEngineMain.CreateManager<ObjectPoolManager>(params)   // DefaultMaxCapacity=9999
       └─ BehaviourSingleton<EntityManager>
            └─ EntityManager.Start()
                 └─ RunInitForSO() [async]
                      ├─ ResourceManager.LoadAssetAsync<TowerConfig>(SO/TowerDB/TowerConfig)
                      ├─ ResourceManager.LoadAssetAsync<EnemyConfig>(SO/EnemyDB/EnemyConfig)
                      └─ PrewarmTowerPools()
                           └─ foreach TowerData.prefabPath → ObjectPoolManager.CreatePool(... destroyTime:-1)
```

### 3.2 塔⽣成：放置流

```
玩家放置
  → PlacedObjManager.OnBuildingPlaced(PlacedObj)
      → TowerPlacementBridge.OnBuildingPlaced
           ├─ ConfigId → TowerId 映射
           ├─ MapLoader.GridToWorld(gridX, gridY)
           └─ EntityManager.SpawnTower(towerId, pos, level)
                ├─ GetTowerData(id)
                ├─ ObjectPoolManager.Spawn(data.prefabPath)
                │    → SpawnGameObject { .Go = 激活的克隆体 }
                ├─ TowerFactory.AttachTo(sg.Go, data, level)
                │    ├─ go.GetComponent<TowerEntity>()
                │    │   └─ 池复⽤: ✅ 直接取  │ ⾸次: null
                │    ├─ ?? go.AddComponent(TowerTypeMap[data.towerType])
                │    └─ entity.Init(data, level)
                ├─ tower.BindPoolHandle(sg)
                └─ OnTowerSpawned?.Invoke(tower)
```

### 3.3 纸⼈⽣成：波次流

```
PrepareNode [FSM]
  → WaveManager.StartPreparePhase()
       ├─ nextWave = _waves[nextWaveIndex]
       ├─ PrewarmEnemyPools(nextWave)
       │    └─ foreach spawnEntry → CreatePool(EnemyData.prefabPath, initCapacity=entry.count, destroyTime:-1)
       └─ OnWavePrepareStarted?.Invoke(nextWave)

BattleNode [FSM]
  → WaveManager.StartBattlePhase()
       └─ 构建 SpawnTracker[] (每个 WaveSpawnEntry 独⽴计时/延迟)

BattleNode.OnFixedUpdate
  → WaveManager.TickBattle(dt)
       └─ foreach SpawnTracker:
            ├─ 延迟计时 → delayTimer<=0 开始⽣成
            ├─ spawnTimer 倒计时 → SpawnEnemy(enemyId)
            │    └─ EntityManager.SpawnEnemy(enemyId, _spawnPos)
            │         ├─ GetEnemyData(id)
            │         └─ SpawnEnemyDirect(data, pos)
            │              ├─ ObjectPoolManager.Spawn(data.prefabPath)
            │              ├─ go.GetComponent<EnemyEntity>() ?? AddComponent<EnemyEntity>()
            │              ├─ enemy.Init(data)
            │              ├─ enemy.BindPoolHandle(sg)
            │              └─ OnEnemySpawned?.Invoke(enemy)
            ├─ enemy.SetWaypoints(GetPathCopy())
            ├─ enemy.OnReachedDestinationEvent += OnEnemyReachedBase
            └─ enemy.OnEntityDeath += OnEnemyDied
```

### 3.4 销毁 → 回池

```
死亡路径:
  TakeDamage → CurrentHp<=0 → OnDeath → IsAlive=false + OnEntityDeath?.Invoke
  WaveManager.OnEnemyDied: _enemiesAlive--, 加⾦币
  → EntityManager 下⼀帧 FixedUpdate 检测 !IsAlive → DespawnEntityInternal

到达终点:
  UpdateMovement → waypointIndex >= count → OnReachedDestinationEvent?.Invoke
  WaveManager.OnEnemyReachedBase: 扣基地血 → EntityManager.DespawnEnemy(enemy)

回池内部:
  DespawnEntityInternal(entity):
    entity.OnDespawn()
      → EntityBase.OnDespawn(): 清 Buffs + OnEntityDeath=null
      → EnemyEntity.OnDespawn(): 清 OnReachedDestinationEvent=null
    entity._poolHandle.Restore()   // SetActive(false), SetParent(poolRoot), Enqueue
    entity._poolHandle = null      // 防重⼊
```

---

## 四、每帧驱动 (EntityManager.FixedUpdate)

```
FixedUpdate (dt):
  for 塔 (逆序):
    if(!IsAlive) → RemoveAt + OnTowerDespawned + DespawnEntityInternal → continue
    if(role==Support) → ApplySupportAura
    if(role==Output)
      → TickAttackCooldownInternal(dt)
      → if(IsAttackReady) TryAttack():
           ├─ GetAttackCooldown()==0? return
           ├─ range = GetAttackRange()
           ├─ if(IsAreaAttack) FindAllInCircle(origin, range, all, buffer)
           │   else FindFirstInCircle(origin, range, all) → buffer.Add
           ├─ if(buffer.Count>0) ExecuteAttack(buffer)
           └─ ConsumeAttackCooldown(cooldown)

  for 纸⼈ (逆序):
    if(!IsAlive) → RemoveAt + OnEnemyDespawned + DespawnEntityInternal → continue
    → enemy.Tick(dt):
         ├─ UpdateMovement(dt)
         └─ UpdateRegen(dt)  // 仅 Endure 类型
```

---

## 五、攻击结算流 (示例)

### 5.1 单⽬标塔 (桃⽊剑/符纸)

```
TryAttack → FindFirstInCircle(最近)
  buffer = [nearest_enemy]
  → PeachSwordTower.ExecuteAttack([target])
       ├─ 3级斩妖: target.HpRatio<threshold → 额外伤害
       ├─ 5级剑诀: 连续命中计数→暴击
       ├─ ConsumeNextAttackCrit()? → damage*=NextCritMultiplier
       └─ target.TakeDamage(damage, this)
```

### 5.2 AOE 塔 (⿎)

```
TryAttack → IsAreaAttack=true → FindAllInCircle(全部+排序)
  buffer = [enemy1, enemy2, enemy3, ...]
  → DrumTower.ExecuteAttack(buffer)
       ├─ foreach enemy: DealDamage(enemy, damage)
       ├─ 3级削弱: ApplyWeakenDebuff(buffer, bonus%, maxStack)
       └─ 5级回响: _echoTimer=echoDelay 延迟→ExecuteEcho 再发半额伤害
```

### 5.3 辅助塔光环 (编钟/幡旗/罗盘)

```
EntityManager.FixedUpdate → if(role==Support) → ApplySupportAura()
  → if(this is ITowerSupportAura aura):
       ├─ GetTowersInRange(pos, aura.AuraRange, buffer)
       └─ foreach tower in buffer:
            ├─ tower.CollectSupportAuraModifiers
            │    → 按 (属性维度+修饰类型) 去重: 取最⾼优先级,resonate累加
            └─ tower.RecalculateAttributes()
```

---

## 六、索敌系统 (TargetingSystem)

| ⽂件 | `FuncModule/Entity/Targeting/TargetingSystem.cs` |
|------|--------------------------------------------------|

**API**

```csharp
// ⽅法1: 圆内最近 (單⽬標塔)
EnemyEntity FindFirstInCircle(Vector3 origin, float range, List<EnemyEntity> allEnemies)

// ⽅法2: 圆内全部 (AOE 塔 → 结果按最近排序)
void FindAllInCircle(Vector3 origin, float range, List<EnemyEntity> all, List<EnemyEntity> results)
```

**塔类如何选择**

| 塔 | IsAreaAttack | 索敌模式 | targets 使⽤ |
|----|-------------|---------|-------------|
| ⿎ (Drum) | true | FindAllInCircle | foreach |
| 桃⽊剑 (PeachSword) | false | FindFirstInCircle | targets[0] |
| 符纸 (Talisman) | false | FindFirstInCircle | targets[0] |
| 灯笼 (Lantern) | false | FindFirstInCircle | targets[0] |

`GetAttackRange()` 返回的是攻击**圆的半径**;TargetingSystem 纯做 2D 平⾯距离过滤 (`dx²+dy² ≤ range²`)。

---

## 七、SO 配置结构

### 7.1 TowerData

| 字段 | 类型 | 说明 |
|------|------|------|
| tower_id | int | 唯⼀ID |
| towerType | TowerType | 枚举: {Drum, PeachSword, Talisman, Lantern, Chime, Banner, Compass} |
| role | TowerRole | {Output, Support} |
| prefabPath | string | 美术预制体 YooAsset 地址 (纯表现层,不挂逻辑脚本) |
| mainAttributes | List\<TowerAttributeEntry\> | 主属性 (套五档 1x→9.5x) |
| secondaryAttributes | List\<TowerAttributeEntry\> | 次属性 (套缓增 +15~20%) |
| growth | LevelGrowthData | 五档等级曲线 |
| level3Breakpoint | SkillBreakpoint | 技能质变参数 |
| level5Breakpoint | SkillBreakpoint | 招牌终极技能参数 |

### 7.2 EnemyData

| 字段 | 类型 | 说明 |
|------|------|------|
| enemy_id | int | 唯⼀ID |
| enemyType | EnemyType | {Normal, Gale, Stack, Endure, Rain} |
| tier | EnemyTier | {Normal, Elite, Boss} |
| prefabPath | string | 美术预制体 YooAsset 地址 |
| baseHp | float | 基础⽣命值 |
| baseMoveSpeed | float | 基础移速 |
| baseArmor | float | 基础护甲 |
| tierMultiplier | float | 档次数值倍率 |
| skillParams | EnemySkillParams | 五类技能参数 (按 enemyType 填对应分组) |

---

## ⼋、对象池要点

### 8.1 预热策略

| 预热时机 | 范围 | API |
|----------|------|-----|
| EntityManager.RunInitForSO | 所有已注册 TowerData.prefabPath | `CreatePool(path, initCapacity:1, destroyTime:-1)` |
| WaveManager.StartPreparePhase | 下⼀波所有敌⼈类型 | `CreatePool(path, initCapacity:entry.count, destroyTime:-1)` |

`destroyTime: -1` → 不会被静默回收;波次结束不清池,下场战⽃直接复⽤。

### 8.2 Spawn → Restore 协议

```
Spawn(location)
  → 池已预热 + 缓存不为空 → Dequeue 缓存对象 → SetActive(true)
  → 返回 SpawnGameObject { .Go = clone }

Restore()
  → entity.OnDespawn()            ← 调⽤⽅负责在 Restore 之前调
  → spawn.Restore()
       → Destroy 收集到的 SpawnGameObject.Go → SetActive(false), SetParent(poolRoot), Enqueue
  → entity._poolHandle = null     ← 防 double-restore
```

### 8.3 Double-despawn 守卫

`_poolHandle` 在 DespawnEntityInternal 中置 null;后续路径 (OnDeath → 下⼀帧 FixedUpdate 检测 !IsAlive + reached-base) 调 `Restore` 前检查 `handle != null` → 不重复回池。

---

## 九、Buffs ⽣命周期

```
实体运⾏时:
  tower.AddBuff(id, caster) → BuffManager.AddBuff —┐
  enemy.AddBuff(id, caster) → BuffManager.AddBuff —┤
  Buff 过期/Remove → BuffManager.ChaAttrRecheck ───┘
       → entity.RecalculateAttributes() ← 重算受 Buff 修饰的最终属性

死亡:
  OnDeath → foreach buff: buff.SetEffective(false).OnBuffRemove().OnBuffDestroy() → Buffs.Clear()

回池:
  OnDespawn → 同上清 Buffs (但跳过 OnEntityDeath 触发,因为在死亡路径已⾛过)
```

---

## ⼗、关键调⽤链快速索引

| 场景 | 起点 | 终点 | 路径 |
|------|------|------|------|
| 配置加载 | GameLauncher | EntityManager.PrewarmTowerPools | ZEngineMain.CreateManager → Start → RunInitForSO |
| 放置塔 | PlacedObjManager | TowerEntity.Init | OnBuildingPlaced → Bridge → EntityManager.SpawnTower → Pool.Spawn → Factory.AttachTo |
| 波次准备 | FSM:PrepareNode | WaveManager.PrewarmEnemyPools | StartPreparePhase → foreach Pool.CreatePool |
| 波次战⽃ | FSM:BattleNode | EnemyEntity.SpawnEnemy | TickBattle → SpawnEnemy → EntityManager.SpawnEnemyDirect → Pool.Spawn |
| 纸⼈到达终点 | UpdateMovement | WaveManager.OnEnemyReachedBase | waypointIndex>=count → event → 扣血 → DespawnEnemy → OnDespawn + Restore |
| 纸⼈死亡 | TakeDamage | WaveManager.OnEnemyDied | OnDeath → OnEntityDeath event → ⾦币 → 下帧 FixedUpdate !IsAlive → DespawnEntityInternal |
| 塔攻击 | EntityManager.FixedUpdate | TowerEntity.TryAttack | IsAttackReady → FindXxxInCircle → ExecuteAttack |
| 辅助光环 | EntityManager.FixedUpdate | TowerEntity.RecalculateAttributes | ApplySupportAura → CollectSupportAuraModifiers → Recalc |
| Buff 变动 | BuffManager | TowerEntity.RecalculateAttributes | ChaAttrRecheck → RecalculateAttributes |
| 堆叠分裂 | EnemyEntity.OnDeath | EntityManager.SpawnEnemyDirect | OnSplitRequested event → 外部调 SpawnEnemyDirect |
