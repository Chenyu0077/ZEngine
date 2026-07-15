# 项目 Luban 集成指南

## 概述

本项目使用 Luban 作为配置表方案，生成格式为 **cs-newtonsoft-json**（C# 代码）+ **json**（JSON 数据），生成代码位于热更程序集 `GameScripts/Hotfix/AutoGenerate/Tables/`。

## 项目目录结构

```
Assets/Datas/DataTables/                          # 配置工程根目录
├── luban.conf                                    # Luban 主配置文件
├── gen.bat                                       # 导出脚本（唯一入口）
├── Defines/                                      # XML Schema 定义
│   └── builtin.xml                               # 内置类型（vector2/vector3/vector4）
├── Datas/                                        # Excel 数据源目录
│   ├── __tables__.xlsx                           # 表注册索引
│   ├── __beans__.xlsx                            # Bean 复合类型定义
│   ├── __enums__.xlsx                            # 枚举类型定义
│   ├── #Buff.xlsx                                # 状态/Buff 表（自动导入）
│   ├── #BuildableItem.xlsx                       # 可建造物品表
│   ├── #Bullet.xlsx                              # 子弹表
│   ├── #ChaControlState.xlsx                     # 角色控制状态表
│   ├── #ChaProperty.xlsx                         # 角色属性表
│   ├── #ChaResource.xlsx                         # 角色资源表
│   ├── #Equipment.xlsx                           # 装备表
│   ├── #Item.xlsx                                # 物品表
│   ├── #Skill.xlsx                               # 技能表
│   └── #Timeline.xlsx                            # 时间轴表
└── output/                                       # JSON 数据输出目录
    ├── tbbuff.json
    ├── tbbuildableitem.json
    └── ...（每表一个 JSON 文件）

Tools/Luban/                                      # Luban 工具链
├── Luban.dll                                     # Luban 主程序
└── Luban.exe

Assets/GameScripts/Hotfix/AutoGenerate/Tables/    # 生成的 C# 代码（热更）
├── Tables.cs                                     # 表管理类（cfg.Tables）
├── Item.cs / TbItem.cs                           # 物品表数据类 + 表类
├── Buff.cs / TbBuff.cs                           # Buff 表数据类 + 表类
└── ...（每表生成对应类文件）
```

## luban.conf 实际配置

```json
{
    "groups": [
        {"names": ["c"], "default": true},
        {"names": ["s"], "default": true},
        {"names": ["e"], "default": true}
    ],
    "schemaFiles": [
        {"fileName": "Defines", "type": ""},
        {"fileName": "Datas/__tables__.xlsx", "type": "table"},
        {"fileName": "Datas/__beans__.xlsx", "type": "bean"},
        {"fileName": "Datas/__enums__.xlsx", "type": "enum"}
    ],
    "dataDir": "Datas",
    "targets": [
        {"name": "server", "manager": "Tables", "groups": ["s"], "topModule": "cfg"},
        {"name": "client", "manager": "Tables", "groups": ["c"], "topModule": "cfg"},
        {"name": "all",    "manager": "Tables", "groups": ["c","s","e"], "topModule": "cfg"}
    ]
}
```

**关键约定**：
- `topModule` 为 `cfg`，生成代码命名空间为 `cfg.xxx`
- 分组 `c`（客户端）、`s`（服务端）、`e`（编辑器）
- 代码目标：`cs-newtonsoft-json`，数据目标：`json`

## 导出脚本（gen.bat）

**位置**：`Assets/Datas/DataTables/gen.bat`

```bat
cd /d "%~dp0"
set GEN_CLIENT=..\..\..\Tools\Luban\Luban.dll
set CONF_ROOT=.

dotnet %GEN_CLIENT% ^
    -t client ^
    -c cs-newtonsoft-json ^
    -d json ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=..\..\GameScripts\Hotfix\AutoGenerate\Tables ^
    -x outputDataDir=output
```

### AI 调用导表命令

```powershell
cmd /c "cd /d Assets\Datas\DataTables && gen.bat"
```

## 配置数据访问

### 基础访问

```csharp
// 获取表实例（需先完成加载初始化）
var tables = /* 通过业务层获取 cfg.Tables 实例 */;

// Map 表：按主键查询
var itemCfg = tables.TbItem.Get(1001);

// 遍历所有数据
foreach (var item in tables.TbItem.DataList)
{
    Debug.Log($"{item.Id} - {item.Name}");
}
```

### 推荐封装访问类

复杂模块建议封装访问类，避免业务代码直接散落：

```csharp
public class ItemConfigMgr
{
    private static ItemConfigMgr _instance;
    public static ItemConfigMgr Instance => _instance ??= new ItemConfigMgr();

    public Item GetItem(int id)
        => Tables.TbItem.Get(id);

    public List<Item> GetByQuality(EQuality quality)
        => Tables.TbItem.DataList.Where(i => i.Quality == quality).ToList();
}
```

## Excel 表定义规范

### __tables__.xlsx 注册表

| full_name | value_type | read_mode | comment |
|-----------|------------|-----------|---------|
| cfg.TbItem | Item | map | 物品表 |
| cfg.TbSkill | Skill | map | 技能表 |
| cfg.TbBuff | Buff | list | Buff表 |

- `full_name`：`cfg.表名`，生成 `cfg.Tables.TbXxx`
- `value_type`：对应的数据类名
- `read_mode`：`map`（按主键索引）/ `list`（列表）

### __beans__.xlsx 复合类型

| full_name | fields.name | fields.type | fields.comment |
|-----------|-------------|-------------|----------------|
| ItemDrop | itemId | int | 道具ID |
| ItemDrop | count | int | 数量 |
| ItemDrop | probability | float | 概率 |

### __enums__.xlsx 枚举

| enum_name | item_name | item_value | item_alias |
|-----------|-----------|------------|------------|
| EQuality | White | 0 | 白 |
| EQuality | Green | 1 | 绿 |

### 业务数据表格式（#前缀自动导入）

数据表文件名使用 `#` 前缀（如 `#Item.xlsx`），Luban 会自动注册。

```
第1行（字段名）: id    | name   | hp   | atk  | desc
第2行（类型）:   int   | string | int  | int  | string
第3行（分组）:   c     | c      | c    | c    | c
第4行（注释）:   物品ID| 名称   | 血量 | 攻击 | 描述
第5行+（数据）: 1001  | 木剑   | 0    | 10   | 新手木剑
```

## 添加新配置表完整流程

```
1. 在 Datas/__tables__.xlsx 中注册新表
   full_name: cfg.TbNewTable   value_type: NewTableRow   read_mode: map

2. 创建 Datas/#NewTable.xlsx（# 前缀，列结构按规范填写）

3. （如需复合类型）在 __beans__.xlsx 定义 Bean
   （如需枚举）在 __enums__.xlsx 定义枚举
   （如需 Unity 类型）在 Defines/builtin.xml 添加 mapper

4. 运行导出脚本
   cmd /c "cd /d Assets\Datas\DataTables && gen.bat"

5. 验证生成结果：
   - AutoGenerate/Tables/ 下新增 TbNewTable.cs 和 NewTableRow.cs
   - output/ 下新增 tbnewtable.json
```

## ExternalTypeUtil Unity 类型映射

通过 `Defines/builtin.xml` 将 Luban 向量类型映射到 Unity 内置类型：

```csharp
public static class ExternalTypeUtil
{
    public static Vector2 NewVector2(cfg.vector2 v) => new Vector2(v.x, v.y);
    public static Vector3 NewVector3(cfg.vector3 v) => new Vector3(v.x, v.y, v.z);
    public static Vector4 NewVector4(cfg.vector4 v) => new Vector4(v.x, v.y, v.z, v.w);
}
```

对应 builtin.xml 中：
```xml
<bean name="vector3" valueType="1" sep=",">
    <var name="x" type="float"/>
    <var name="y" type="float"/>
    <var name="z" type="float"/>
    <mapper target="client" codeTarget="cs-newtonsoft-json">
        <option name="type" value="UnityEngine.Vector3"/>
        <option name="constructor" value="ExternalTypeUtil.NewVector3"/>
    </mapper>
</bean>
```

## 兼容性注意事项

- **新增字段**：前向兼容，旧客户端自动忽略新字段
- **删除字段**：不兼容，旧客户端会报错
- **修改字段类型**：不兼容，需同步更新代码
- **重命名字段**：不兼容，影响热更包
- 生成代码（`AutoGenerate/Tables/` 目录）**不要手动修改**，下次生成会覆盖
