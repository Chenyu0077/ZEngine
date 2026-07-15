# Map Editor 使用手册

## 目录

1. [快速开始](#1-快速开始)
2. [配置系统（MapEditorConfig）](#2-配置系统-mapeditorconfig)
3. [编辑器窗口布局](#3-编辑器窗口布局)
4. [编辑模式](#4-编辑模式)
5. [图层管理](#5-图层管理)
6. [TileSet 配置与绘制](#6-tileset-配置与绘制)
7. [格子属性编辑](#7-格子属性编辑)
8. [框选与批量修改](#8-框选与批量修改)
9. [对象摆放（BuildingConfig）](#9-对象摆放buildingconfig)
10. [出生点编辑](#10-出生点编辑)
11. [地图导出与导入](#11-地图导出与导入)
12. [多配置管理](#12-多配置管理)
13. [Runtime 加载](#13-runtime-加载)
14. [MapData JSON 结构](#14-mapdata-json-结构)

---

## 1. 快速开始

### 打开编辑器

```
菜单栏 → Tools → Map Editor
```

首次打开时，编辑器自动在 `Assets/Settings/MapEditorConfig.asset` 创建默认配置文件，并初始化一张空白地图。

### 最简工作流

```
1. 打开 Map Editor
2. 在 MapEditorConfig 中添加 TileSet（可选）
3. 在 Scene View 中用画笔绘制瓦片
4. 编辑格子属性（walkable / buildable 等）
5. 导出 JSON → Runtime 加载
```

---

## 2. 配置系统（MapEditorConfig）

所有可变配置集中在一个 ScriptableObject 中，类似 YooAsset 的配置思路。

### 创建 / 找到配置

- **自动创建**：首次打开编辑器自动在 `Assets/Settings/` 生成
- **手动创建**：右键 Assets → Create → Map Editor → Config
- **在编辑器中切换**：工具栏第二行 ObjectField 拖入目标 Config

### 查看 / 修改配置

点击工具栏 **[⚙ 查看]** 按钮，在 Inspector 中打开当前配置。

### 配置项说明

| 区块 | 内容 | 说明 |
|---|---|---|
| **图层配置** | LayerDefinition 列表 | 可增删、拖拽排序，sortOrder 控制渲染深度 |
| **格子属性 Schema** | CellPropertyDef 列表 | 定义每个格子有哪些属性（Bool/Int/Float/String/Enum）|
| **地形类型** | TerrainTypeDef 列表 | terrainType 枚举的可选值 |
| **区域类型** | ZoneTypeDef 列表 | zone 枚举的可选值 |
| **出生点类型** | SpawnPointTypeDef 列表 | 出生点 type 的可选值 |
| **TileSet 资产引用** | TileSetReference 列表 | 引用 TileSetData SO，tileId 全局唯一 |
| **全局设置** | 默认地图尺寸、格子大小、导出路径、网格颜色等 | 仅影响新建地图，不自动修改已有地图 |

### 全局设置与已有地图

修改 **默认宽度 / 高度 / 格子大小** 不会自动应用到当前地图。  
若需要同步，在右侧面板地图信息区找到差异提示，点击 **[应用配置默认值到当前地图]**（会弹确认对话框，缩小时数据将被截断）。

### 配置修改即时生效

在 Inspector 中修改配置（增删图层、修改属性 Schema 等），Map Editor 通过 `OnValidate` 事件即时感知，Scene View 自动刷新，无需重启编辑器。

---

## 3. 编辑器窗口布局

```
┌──────────────────────────────────────────────────────────────────┐
│ 行1  [新建][打开][保存]  [导出JSON][导入JSON]          地图名称*  │
│ 行2  配置: [MapEditorConfig.asset ▾]  [新建配置]  [⚙查看]       │
├──────────────────────────────────┬───────────────────────────────┤
│                                  │                               │
│  左侧面板（~60% 宽）              │  右侧检查器（~40% 宽）        │
│                                  │                               │
│  [编辑模式] 2行×4列               │  地图信息                     │
│  ┌──────┬──────┬──────┬──────┐   │  · Map ID / 名称              │
│  │画笔T │擦除E │填充F │对象O │   │  · 尺寸 / 格子大小            │
│  ├──────┼──────┼──────┼──────┤   │  · 绑定配置                   │
│  │删对D │格子C │出生S │选择V │   │  · 差异提示 + 应用按钮        │
│  └──────┴──────┴──────┴──────┘   │                               │
│                                  │  格子属性检查器               │
│  [图层][瓦片][对象][出生点]       │  （C 模式下显示）             │
│  ─────────────────────────────   │  或                           │
│  ↕ 滚动区域                       │  批量修改面板                 │
│    高度随窗口高度自动调整          │  （V 模式框选后显示）         │
│    各面板每行尽量放满 Item         │                               │
│                                  │  ↩撤销  ↪重做                 │
│  叠加层显示 ▸（在图层标签页底部） │                               │
└──────────────────────────────────┴───────────────────────────────┘
```

**Scene View**（独立窗口，与编辑器配合使用）：
- 网格线（Handles 3D，网格下方）
- 瓦片 Sprite 渲染（GUI 层）
- 叠加色（walkable/buildable 等属性可视化）
- 悬停高亮 / 选中高亮 / 框选 / 对象幽灵预览

---

## 4. 编辑模式

左侧面板顶部以 **2行×4列** 按钮展示，当前激活模式高亮显示。

```
[画笔 T][擦除 E][填充 F][对象 O]
[删对 D][格子 C][出生 S][选择 V]
```

| 模式 | 快捷键 | 功能描述 |
|---|---|---|
| **画笔** | `T` | 点击或拖拽，将选中瓦片写入当前激活图层 |
| **擦除** | `E` | 点击或拖拽，清除当前激活图层该格的瓦片（仅擦激活层）|
| **填充** | `F` | 油漆桶：对相同 tileId 的连通区域进行洪水填充 |
| **对象** | `O` | 摆放大型对象 Prefab，自动读取 BuildingConfig 尺寸 |
| **删对** | `D` | 点击已放置对象将其删除 |
| **格子** | `C` | 点击格子，右侧检查器显示该格所有属性供编辑 |
| **出生** | `S` | 在 Scene View 中点击放置出生点 |
| **选择** | `V` | 框选矩形区域，右侧切换为批量属性修改面板 |

**Ctrl+Z / Ctrl+Y**：撤销 / 重做（最多 30 步）

### 注意事项

- **画笔 / 擦除 / 填充** 只操作**当前激活图层**，请在图层管理器中点击图层名激活
- 擦除只擦激活图层，若 Sprite 在其他图层需先切换到该图层再擦除
- 切换到非"格子"模式时，格子检查器的选中状态自动清除
- 切换到非"选择"模式时，框选区域自动清除

---

## 5. 图层管理

位于左侧面板 **图层** 标签页。

### 图层列表

图层按 **sortOrder 升序**排列（小在前、下方渲染；大在后、上方渲染）。  
每行显示：

```
[色块]  图层名         [激活]   可见 □   锁定 □   透明度 ──○──  80%
```

| 控件 | 说明 |
|---|---|
| 色块 | 该图层的调试色（在 Config Inspector 中修改） |
| 图层名（点击） | 将该图层设为**当前激活图层**，画笔/擦除/填充写入此层 |
| **可见** 复选框 | 隐藏 / 显示该图层 Tile（不影响数据） |
| **锁定** 复选框 | 锁定后无法绘制（锁定时背景变橙） |
| 透明度滑条 + % | 调整图层编辑器显示透明度 |

### 叠加层显示

图层列表下方的 **叠加层显示** 折叠区：  
勾选某属性后，Scene View 中满足触发条件的格子显示对应颜色覆盖层（如 walkable=false → 红色）。  
叠加颜色在 Config → 格子属性 Schema 中的 `overlayColor` 字段配置。

### 增删图层

在 **MapEditorConfig Inspector → 图层配置** 中操作：

- **[+ 添加图层]**：新图层追加到末尾，自动分配 sortOrder（最大值+10）
- **[删除]**：删除后，已有地图 JSON 再次加载时该图层数据丢弃
- **[↑][↓]**：调整列表顺序（不影响 sortOrder 值）

### 渲染层叠顺序

```
sortOrder 小（如 0）  → 最底层渲染（地面瓦片）
sortOrder 大（如 20） → 最顶层渲染（屋顶/顶层装饰）
```

---

## 6. TileSet 配置与绘制

### 创建 TileSetData

```
右键 Assets → Create → Map Editor → TileSet Data
```

在 TileSetData Inspector 中添加 TileEntry：

| 字段 | 说明 |
|---|---|
| `tileId` | **全局唯一整数**，不同 TileSet 间不得重复（建议分段：A 用 0-99，B 用 100-199）|
| `tileName` | 显示名称 |
| `sprite` | 引用 Sprite 资产；有 Sprite 时 Scene View 渲染实际贴图 |
| `fallbackColor` | 无 Sprite 时在编辑器中显示的替代颜色方块 |

### 在 Config 中注册 TileSet

**MapEditorConfig Inspector → TileSet 资产引用 → [+ 添加 TileSet]**

- `id`：标识符
- `displayName`：调色板分组名称
- `tileSet`：拖入 TileSetData 资产

> **TileSet 全局化**：不绑定图层，所有图层均可使用任意 TileSet 的瓦片。tileId 唯一即可。

### 绘制瓦片

1. 切换到 **瓦片** 标签页，调色板展示所有已注册 TileSet
2. 每行自动填满可用宽度（列数 = 面板宽度 / 图标尺寸）
3. 点击选中目标 Tile（选中后高亮为青色）
4. 在图层管理器中确认激活图层
5. 在 Scene View 中点击或拖拽进行绘制

### 橡皮擦（E）

清除**当前激活图层**上的瓦片。其他图层的瓦片不受影响，若要擦除请先切换到对应图层。

### 油漆桶（F）

从点击格开始，对相同 tileId 的连通区域全部替换为当前选中 Tile。

---

## 7. 格子属性编辑

### 单格编辑

1. 切换到 **格子（C）** 模式
2. 在 Scene View 中点击格子 → 该格被选中（蓝色边框 + 四角标记 + 坐标标签）
3. 右侧检查器显示该格所有属性（由 Schema 驱动，自动生成控件）

| 属性类型 | 控件 |
|---|---|
| Bool | Toggle（复选框）|
| Int | IntField |
| Float | FloatField |
| String | TextField |
| Enum | Popup（选项来自对应类型表）|

- 属性值等于默认值时，旁边的 **[↺]** 重置按钮不显示
- 点击 **[↺]** 一键还原为 Schema 默认值

### 内置默认属性

| 属性 key | 类型 | 默认值 | 用途 |
|---|---|---|---|
| `walkable` | Bool | true | 寻路：false = 不可通行（Scene View 红色叠加）|
| `buildable` | Bool | true | 建筑：false = 不可放置建筑（橙色叠加）|
| `farmable` | Bool | false | 种田：true = 可耕种（绿色叠加）|
| `terrainType` | Enum | grass | 地形类型，影响寻路权重 |
| `zone` | Enum | none | 功能区域标记 |
| `pathWeight` | Float | 1.0 | 寻路代价（越大越难走）|

### 自定义属性

在 **MapEditorConfig → 格子属性 Schema** 中添加新属性后，格子检查器 UI 自动出现对应控件，无需修改任何代码。

---

## 8. 框选与批量修改

### 建立选区

1. 切换到 **选择（V）** 模式
2. 在 Scene View 中**按住左键拖拽**，蓝色矩形随鼠标扩展
3. 松开鼠标 → 选区固定，Scene View 显示 `选区 W×H（N 格）`

### 批量修改

选区建立后，右侧面板切换为批量修改界面：

```
批量修改属性
已选中 N 格（W × H）

walkable    [☑ true ]  [应用]
buildable   [☑ true ]  [应用]
terrainType [grass  ▾]  [应用]
...

[清除选区]
```

- 每个属性独立操作，点击 **[应用]** 才会写入，不影响其他属性
- 整个选区的批量操作只压**一次**撤销栈，可 Ctrl+Z 整体回退
- **[清除选区]** 或切换到其他编辑模式均会清除选区

---

## 9. 对象摆放（BuildingConfig）

### BuildingConfig 脚本

`BuildingConfig` 是一个挂载在建筑 Prefab 上的 MonoBehaviour，声明该建筑占用的格子尺寸：

```csharp
// 挂载到建筑 Prefab，填写占位格子数
public class BuildingConfig : MonoBehaviour
{
    [Min(1)] public int gridWidth  = 1;  // 列数（X 方向）
    [Min(1)] public int gridHeight = 1;  // 行数（Y 方向）
}
```

**制作流程：**
1. 创建建筑 Prefab
2. 在 Prefab 上添加 `BuildingConfig` 组件
3. 在 Inspector 中填写 `Grid Width` 和 `Grid Height`
4. 将 Prefab 放入 Config 配置的 `objectPalettePath` 目录

### 摆放步骤

1. 切换到 **对象（O）** 模式，左侧标签页切换到 **对象**
2. 面板自动扫描 `objectPalettePath` 目录下所有 Prefab
3. 点击目标 Prefab 图标：
   - **有 BuildingConfig** → 自动读取 `gridWidth/Height`，面板显示只读尺寸 + `（来自 BuildingConfig）` 标注
   - **无 BuildingConfig** → 手动填写宽高 IntField
4. 在 Scene View 移动鼠标 → 出现**紫色幽灵预览**，显示占位矩形和对象名
   - 若目标位置越界或与已有对象重叠，预览变**红色**并提示无法放置
5. 左键点击 → 对象被放置，Scene View 显示黄色占位边框

### 放置合法性检查

放置时同时检查两个条件（任一不满足则拒绝放置）：

| 检查项 | 说明 |
|---|---|
| **边界检查** | 整个占位矩形（x+width, y+height）必须在地图范围内 |
| **碰撞检查** | 占位矩形与所有已有对象的 AABB 无重叠 |

### 删除对象

切换到 **删对（D）** 模式，点击格子删除覆盖该格的对象。

### Tooltip 提示

悬停 Prefab 图标时，Tooltip 显示：
```
building_house_small
2×3 格              ← 有 BuildingConfig 时额外显示
```

### 对象目录配置

在 **Config → 全局设置 → 对象 Prefab 路径** 中修改扫描目录（默认 `Assets/Resources/MapObjects`），修改后点击面板中的 **[刷新]** 按钮重新扫描。

> Runtime 加载时通过 `prefabId`（Prefab 文件名）从 `Resources/MapObjects/` 实例化对象。

---

## 10. 出生点编辑

### 放置出生点

1. 切换到 **出生（S）** 模式，左侧切换到 **出生点** 标签页
2. 在标签页顶部配置"新出生点配置"（类型、NPC ID、朝向）
3. 在 Scene View 中点击目标格子 → 出生点被放置（带颜色圆圈 + 类型标签）

### 管理出生点列表

| 操作 | 说明 |
|---|---|
| **[✏]** | 展开内联编辑，可修改坐标、类型、NPC ID、朝向 |
| **[✕]** | 删除该出生点 |

### 出生点类型

在 **MapEditorConfig → 出生点类型** 中增删类型，每个类型可配置 `gizmoColor`，Scene View 中显示对应颜色圆圈。

---

## 11. 地图导出与导入

### 导出 JSON

**工具栏 [导出 JSON]** → 选择保存路径 → 生成 `.json` 文件

导出时自动：
1. 刷新 Schema 快照（将当前 Config 状态嵌入 JSON）
2. 写入 `configPath`（绑定当前配置文件路径）
3. 稀疏存储格子数据（仅存非默认值，减少体积）

建议导出到：`Assets/Resources/Maps/{mapId}.json`

### 保存（快捷方式）

**[保存]** 按钮（或 `Ctrl+S`）：
- 已有保存路径 → 直接覆盖写入
- 尚无路径 → 弹出文件选择对话框（等同导出）

工具栏地图名称后显示 `*` 表示有未保存修改。

### 导入 JSON

**工具栏 [导入 JSON]** → 选择 `.json` 文件

导入时兼容性处理：
- Config 新增的图层 → 在地图数据中初始化为空
- Config 删除的图层 → 旧数据中该图层数据丢弃（控制台有警告）
- 新增的格子属性 → 旧格子自动使用新属性的默认值
- 删除的格子属性 → 旧数据中的 key 被忽略

### 配置绑定与不匹配处理

每个地图 JSON 在导出时记录 `configPath`（绑定的配置文件路径）。  
重新打开地图时，若配置不匹配弹出三选一对话框：

```
配置不匹配
此地图绑定的配置：ConfigA
当前使用的配置：ConfigB

[切换到地图配置]  [取消]  [强制用当前配置]
```

---

## 12. 多配置管理

不同地图（如室外小镇 / 室内建筑 / 地下城）可使用各自的配置文件，配置文件之间完全独立。

### 新建配置

**工具栏第二行 → [新建配置]** → 选择保存位置 → 自动切换并选中

### 切换配置

工具栏第二行 ObjectField 直接拖入或点选目标配置文件。  
切换后当前地图的 Schema 和图层数组自动刷新以匹配新配置。

### 地图与配置的绑定规则

- 导出/保存时自动将当前配置路径写入 `configPath`
- 下次打开该地图时若配置不一致，弹窗提示用户选择处理方式

---

## 13. Runtime 加载

### 挂载 MapLoader

将 `MapLoader` 组件挂载到场景中的任意 GameObject：

```
MapLoader
  ├── Map ID:           town_01         ← 对应 JSON 文件名（不含扩展名）
  ├── Auto Load On Start: ☑             ← 运行时自动加载
  └── Tilemap Grid:     (可选，手动指定)
```

### 加载流程

```
MapLoader.Load("town_01")
    │
    ├── 读取 Resources/Maps/town_01.json
    ├── 反序列化 MapSaveData（含 schema 快照）
    ├── 构建 GridMap[width][height]（每格 CellRuntime）
    ├── 按 sortOrder 顺序创建 Tilemap 图层
    ├── 实例化 Objects（从 Resources/MapObjects/ 加载 Prefab）
    └── 注册 SpawnPoints
```

### 代码查询 API

```csharp
var loader = GetComponent<MapLoader>();

// 格子查询
CellRuntime cell = loader.GetCell(x, y);
bool   canWalk  = cell.IsWalkable;
float  weight   = cell.PathWeight;
string terrain  = cell.TerrainType;
string zone     = cell.Zone;

// 自定义属性查询
bool   isIndoor = cell.GetBool("isIndoor");
int    lightLv  = cell.GetInt("lightLevel");
string eventId  = cell.GetString("triggerEventId");

// 坐标转换
Vector3    worldPos = loader.GridToWorld(x, y);
Vector2Int gridPos  = loader.WorldToGrid(worldPos);

// 寻路相关
bool  walkable = loader.IsWalkable(x, y);
float weight   = loader.PathWeight(x, y);

// 获取完整数据
MapSaveData mapData = loader.GetMapData();
```

---

## 14. MapData JSON 结构

```json
{
  "mapId":         "town_01",
  "mapName":       "主镇",
  "width":         40,
  "height":        40,
  "cellSize":      1.0,
  "schemaVersion": "1.0",
  "configPath":    "Assets/Settings/MapEditorConfig.asset",

  "schema": {
    "cellProperties": [
      { "key": "walkable",    "type": "Bool",  "default": "true"  },
      { "key": "terrainType", "type": "Enum",  "default": "grass" },
      { "key": "pathWeight",  "type": "Float", "default": "1.0"   }
    ],
    "terrainTypes": ["grass", "water", "road", "sand", "forest"],
    "zoneTypes":    ["none", "residential", "commercial", "nature", "farm"],
    "layers": [
      { "id": "ground",      "sortOrder": 0  },
      { "id": "decoration",  "sortOrder": 10 },
      { "id": "overlay",     "sortOrder": 20 }
    ]
  },

  "layers": [
    { "id": "ground",     "tiles": [0, -1, 1, 0, 2, ...] },
    { "id": "decoration", "tiles": [-1, -1, 5, -1, ...]  }
  ],

  "cells": [
    { "x": 5,  "y": 3,  "props": { "walkable": "false" } },
    { "x": 10, "y": 8,  "props": { "terrainType": "water", "pathWeight": "99" } }
  ],

  "objects": [
    {
      "instanceId": "obj_a1b2c3d4",
      "prefabId":   "building_house_small",
      "x": 10, "y": 8,
      "width": 2, "height": 3,
      "rotation": 0
    }
  ],

  "spawnPoints": [
    {
      "id":     "spawn_001",
      "x": 15, "y": 12,
      "type":   "npc",
      "npcId":  "npc_farmer_01",
      "facing": "down"
    }
  ]
}
```

### 格子稀疏存储

`cells` 数组只存储**与默认值不同**的格子，未出现的格子在运行时从 Schema 读取默认值填充，大幅减少 JSON 体积。

### Schema 快照

每次导出时将当前 Config 的 Schema 嵌入 JSON，使 MapData **自描述**，Runtime 加载无需引用 ScriptableObject 即可正确解析所有属性。

### 对象 width/height 来源

`objects[].width` 和 `objects[].height` 在放置时写入：
- 有 `BuildingConfig` 组件 → 取 `gridWidth / gridHeight`
- 无 `BuildingConfig` → 取对象面板手动填写的值
