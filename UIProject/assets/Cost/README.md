# Cost Package - 成本统计UI包

这是一个完整的FairyGUI包，用于展示AI Village的成本和Token统计信息。

## 文件结构

```
Cost/
├── package.xml                 # 包配置文件
├── View/
│   └── CostView.xml           # 主视图 (1000x700)
├── Com/                       # 组件文件夹
│   ├── CostHeaderPanel.xml    # 标题栏面板
│   ├── CostBasicInfoPanel.xml # 基础信息面板
│   ├── CostCumulativePanel.xml # 累计统计面板 📊
│   ├── CostAveragePanel.xml   # 平均统计面板 📈
│   ├── CostVillagePanel.xml   # 建村成本面板 🏘️
│   ├── CostCachePanel.xml     # 缓存统计面板 💾
│   ├── CostModelsPanel.xml    # 模型统计面板 🤖
│   ├── ModelListItem.xml      # 模型列表项
│   ├── ToggleBtn.xml          # 自动刷新开关
│   ├── CloseBtn.xml           # 关闭按钮
│   ├── ProgressBar.xml        # 进度条组件
│   └── SmallProgress.xml      # 小型进度条
└── README.md                  # 说明文档
```

## 主要功能

### 1. 状态管理
- **加载状态**: 显示加载动画和提示文字
- **数据状态**: 显示完整的成本统计数据
- **错误状态**: 显示错误信息和重试按钮

### 2. 统计面板
- **📊 累计统计**: 总Token、输入/输出Token、总费用、缓存命中率
- **📈 平均统计**: 单NPC单日成本、全村单日平均
- **🏘️ 建村成本**: Day 0的初始化成本
- **💾 缓存统计**: 缓存命中Token、节省费用
- **🤖 模型统计**: 按模型分类的详细统计表格

### 3. 交互功能
- **自动刷新**: 可开启/关闭的自动刷新功能
- **手动刷新**: 立即更新数据
- **排序功能**: 模型列表支持按列排序
- **悬浮效果**: 列表项悬浮高亮
- **动画效果**: 窗口打开/关闭、数据更新闪烁

## 设计规范

### 颜色方案
- **主色调**: #2C3E50 (深蓝色)
- **强调色**: #27AE60 (绿色) - 正常状态
- **警告色**: #F39C12 (黄色) - 警告
- **错误色**: #E74C3C (红色) - 错误状态
- **背景色**: #ECF0F1 (浅灰色)

### 字体大小
- **标题**: 18px
- **副标题**: 14px
- **正文**: 12px
- **小字**: 10px

### 响应式设计
- **基础尺寸**: 1000x700
- **最小尺寸**: 800x600
- **最大尺寸**: 1200x800

## 组件依赖

### 内部依赖
所有Cost包内的组件引用都使用 `ui://Cost/` 前缀：
- CostView → 各个面板组件
- CostModelsPanel → ModelListItem
- 各面板 → ProgressBar/SmallProgress

### 外部依赖
保持对Main包通用组件的引用：
- `ui://Main/ComBtn` - 通用按钮组件

## 使用方法

1. **在FairyGUI编辑器中**:
   - 导入Cost包
   - 打开CostView.xml进行编辑
   - 所有组件引用已正确配置

2. **在Unity中**:
   - 生成的C#代码位于Main包中
   - 使用UICostView类进行控制
   - 通过stateCtrl控制器切换显示状态

3. **数据绑定**:
   - 各个文本字段有明确的name属性
   - 进度条组件支持0-1的值域设置
   - 列表组件配置了ModelListItem模板

## 注意事项

1. 确保在Unity中正确导入FairyGUI包
2. 所有文本字段都有默认显示值
3. 进度条需要在代码中设置实际数值
4. 列表数据需要通过代码动态填充
5. 控制器状态切换需要在逻辑层控制