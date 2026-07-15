# ZEngine UGUI 框架 AI 自动化工作流手册

> **适用场景**：Session 切换 / Model 切换后，按本文档步骤即可继续 UI 面板的 AI 自动生成。
>
> **前置条件**：Unity Editor 已打开，Unity MCP Bridge 已连接，`ZEngine`/`ZEngineEditor`/`HotUpdate` 编译通过。

---

## 零、AI 执行清单（强制，每条必须完成，不可跳过）

> **作用**：确保不同 Session/Model 之间生成结果一致。以下步骤在任何面板生成任务中**必须全部完成**，顺序不可打乱。

| # | 步骤 | 输出形式 | 检查标准 |
|---|---|---|---|
| **C1** | 阅读 §一 组件清单，确认需要的每个控件都在列表中 | 内部确认 | 不存在"描述里有但组件清单中没有"的控件 |
| **C2** | 输出组件树 | 回复中贴出表格 | 每行含：节点名、ZEngine 包装类型、层级路径、是否用工厂 |
| **C3** | 输出尺寸规划表（按 §Step 0.55 格式） | 回复中贴出表格 | 含尺寸/位置/锚点/边距 4 列，附带校验数据 |
| **C4** | **等待用户确认** | 用户回复"确认"/"执行"/"OK" | 未收到确认**绝对不能**开始写代码 |
| **C5** | 执行 Step 0 清理 + Canvas 确认 | execute_code 返回值 | 返回 "cleaned N / Canvas OK" |
| **C6** | 按规划表逐节点创建，**不自行改数字** | execute_code 分批次 | 每个 sizeDelta/anchoredPosition/offset 必须与规划表一致 |
| **C7** | 截图验证 | manage_camera screenshot | 与规划表对比尺寸偏差 |
| **C8** | 截图通过后存 Prefab | manage_prefabs | Prefab 路径 `Assets/GameAssets/Prefabs/UI/<PanelName>.prefab` |
| **C9** | 生成 MVC 脚本 | create_script × 3 | 编译 0 error |
| **C10** | 清理场景临时 GO | execute_code | 删除面板根节点 |

**补充强制约束**：

- **颜色值必须精确**：不能用"绿色"、"红色"等模糊词，必须用代码中的 `new Color(r,g,b,a)` 形式。描述中的 Hex（如 `#17B85F`）必须转换为 `new Color(0.09f, 0.72f, 0.37f, 1f)`
- **工厂方法优先**：对于 §1.4 中标注"多层级+字段引用"的组件（UIInput/UISlider/UIDropdown/UIListView），**必须使用工厂方法**，禁止手动 new GameObject 替代（否则字段引用会缺失）
- **倒序清理**：Step 0 清理必须用 `for (int i = all.Length - 1; i >= 0; i--)` 倒序，禁止正序 foreach 删除
- **本地函数禁用**：`execute_code` 使用 CodeDom(C#6)，代码中不得出现本地函数、元组、`var T = Type; T.Method()` 赋值类型调用

---

## 一、框架速览

### 1.1 组件清单（ZEngine.Manager.UI.UGUI.Components 命名空间）

**基础控件**（每个都是 `UIComponentBase` 子类 + `[RequireComponent(原生UGUI)]` + `OnInit` 注册事件监听 / `OnRelease` 注销事件监听）：

| 包装组件       | 包装的原生          | 子层级                                                                                                                                   |
| -------------- | ------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| `UIButton`   | `Button`          | `Label` (TMP)                                                                                                                          |
| `UIText`     | `TextMeshProUGUI` | 无                                                                                                                                       |
| `UIImage`    | `Image`           | 无                                                                                                                                       |
| `UIInput`    | `TMP_InputField`  | `Text Area` > `Placeholder` + `Text`                                                                                               |
| `UIToggle`   | `Toggle`          | `Checkmark` (Image)                                                                                                                    |
| `UISlider`   | `Slider`          | `Background` + `Fill Area` > `Fill` + `Handle Area` > `Handle`                                                                 |
| `UIDropdown` | `TMP_Dropdown`    | `Label` + `Arrow` + `Template` > `Viewport`(Mask) > `Content`(VerticalLayoutGroup) > `Item` > `Checkmark` + `Item Label` |

**容器/列表**：
| `UIGrid` | `GridLayoutGroup` | 无（纯布局容器） |
| `UITree` | `VerticalLayoutGroup` | `RowTemplate`(Indent + Expand + Label)，运行时当 rowPrefab 传给 `SetData` |
| `UITab` | — | `TabStrip`(HorizontalLayoutGroup) + `Pages`，运行时 `AutoBind()` 自动配对 |
| `UIListView` | `LoopVerticalScrollRect` | `Viewport`(Mask) > `Content`(VerticalLayoutGroup)，自带 PrefabSource 池化 |

**窗口/弹窗/通用**：
| `UIWindow` | `UBaseView` | `CloseBtn`(UIButton)，`OnOpenAnimation`/`OnCloseAnimation` 返回 `Tween` |
| `UIPopup : UIWindow` | — | `Mask`(Image+UIImage+Button+UIButton) + `Body` + `CloseBtn` |
| `UIToastView` | `UBaseView` | `Text`(UIText) |
| `UILoading` | `UBaseView` | `Tip`(UIText) |

### 1.2 关键机制

- **`[UIBind("path")]`**：View 类上的字段自动绑定特性。路径格式 = 相对于 View 根节点的 `Transform.Find` 路径。支持原生 UGUI 类型和包装类型。
- **`UIBinder`**：沿基类链 `DeclaredOnly` 收集 `[UIBind]` 字段，一次反射按类型缓存。`BuildChildCache()` 末尾自动执行。
- **`UIView("location", layer, isSingleton, isFullScreen)`**：声明 View 对应的 YooAsset 加载路径 + 层级 + 单例标记。
- **`UBaseView.ModelType/ControllerType`**：`virtual` 属性（有 `protected set`），子类 `override` 返回 `typeof(XxxModel/XxxController)`。
- **组件生命周期**：`BuildChildCache` → 收集 `UIComponentBase` → `OnInit`(UIButton.OnInit 注册 Button.onClick 监听等) → `OnRelease`(UIButton.OnRelease 注销监听) → 批量 `OnRelease` 释放。此处的"注册/注销事件监听"与工厂方法层面的"字段引用注入"是**两个不同层面**——前者是运行时事件监听生命周期，后者是创建时把子节点赋给父组件的字段。

### 1.3 现有编辑器工具

| 工具路径                                                 | 功能                                                              |
| -------------------------------------------------------- | ----------------------------------------------------------------- |
| `ZEngineTools/UGUI MVC Generator`                      | 从 Prefab 扫描包装组件 → 反推生成 View/Model/Controller 三个脚本 |
| `GameObject/UI/ZEngine/<组件>`                         | 右键 MenuItem，一键创建带完整子层级的组件 GameObject              |
| `UIComponentMenu.CreateXXX(RectTransform parent, ...)` | `public static` 工厂方法，可被 `execute_code` 直接调用        |

### 1.4 组件复杂度等级

| 等级                      | 特征                                                                  | 组件                                         |
| ------------------------- | --------------------------------------------------------------------- | -------------------------------------------- |
| **叶子**            | 无子节点，只挂底层 UGUI + 包装组件                                    | UIText, UIImage                              |
| **带 1 层子节点**   | 有固定命名的子节点                                                    | UIButton(`Label`), UIToggle(`Checkmark`) |
| **多层级+字段引用** | 需要把子节点赋给底层组件的对应字段(textViewport/fillRect/template 等) | UIInput, UISlider, UIDropdown, UIListView    |
| **脚手架**          | 含样例/模板子节点供运行时扩展                                         | UITab(TabStrip+Pages), UITree(RowTemplate)   |
| **框架基**          | 约定子节点(CloseBtn/Mask/Body)+生命周期                               | UIWindow, UIPopup, UIToastView, UILoading    |

### 1.5 预生成组件可用性矩阵（YooAsset EditorSimulateMode）

| 预制体路径`"UI/Prefabs/<Name>"` | 对应 View 类     | 过程化 Fallback              | Yoo 加载        |
| --------------------------------- | ---------------- | ---------------------------- | --------------- |
| `"UI/Prefabs/Toast"`            | `UIToastView`  | ✅ 有                        | ❌ 待提供预制体 |
| `"UI/Prefabs/Loading"`          | `UILoading`    | ✅ 有                        | ❌ 待提供预制体 |
| `"UI/Prefabs/<新面板>"`         | `<新面板>View` | ❌ 无（业务面板无 fallback） | ❌ 待提供预制体 |

---

## 二、AI 自动生成 Prefab + MVC 脚本工作流（核心）

### 工作流概览

```
用户描述面板（自然语言 或 UI图片描述）
  ↓
AI 拆解为组件树（控件类型 + 节点名 + 文本内容）
  ↓
AI 输出尺寸规划表（节点 → 尺寸/位置/锚点/边距）→ 用户确认或调整
  ↓
execute_code 按规划表创建 Prefab（工厂方法 + 手动自定义样式）
  ↓
截图验证（比较规划表与实际效果，偏差过大则调 Layout）
  ↓
manage_prefabs create_from_gameobject 存为 Prefab
  ↓
create_script 生成 View / Model / Controller 三个 .cs 文件
  ↓
编译验证 → refresh_unity + read_console 确认 0 error
```

### 详细步骤

#### Step 0: 清理 + 确认 Canvas（每条 Session 开始创建面板前必做，不依赖 Session 记忆）

**⚠️ 执行原则**：这步只做通用清理 + 环境确认，不依赖上一 Session 的具体面板名。每次创建面板前都跑一遍。

**第一步 — 通用清理（删除所有常见的测试残留）**：

```csharp
// 清理常见测试面板名（可安全执行，不存在的 GO 会被跳过）
var staleNames = new string[] { "SettingsPanel","SettingsPanel1","MainMenuPanel","MainMenuPanel1",
    "UIButton","UIText","UIImage","UIInput","UIToggle","UISlider","UIDropdown",
    "UIGrid","UITab","UITree","UIListView","UIWindow","UIPopup","UILoading","UIToast",
    "Btn_StartGame","Btn_Options","Btn_Credits","<当前要创建的面板名>" };
int k = 0;
foreach (var t in UnityEngine.Object.FindObjectsOfType<UnityEngine.Transform>()) {
    foreach (var n in staleNames) { if (t.name == n) { UnityEngine.Object.DestroyImmediate(t.gameObject); k++; } }
}
return "cleaned " + k + " stale test GOs";
```

`staleNames` 列表根据当前要创建的面板名**追加一条即可**，不用每次改全部。

**第二步 — 确认 Canvas 存在（不存在则创建）**：

```csharp
var cv = UnityEngine.Object.FindObjectOfType<UnityEngine.Canvas>();
if (cv == null) {
    // 创建 Canvas（ScreenSpaceOverlay，无父节点即场景根）
    var cvGo = new UnityEngine.GameObject("MainCanvas");
    cv = cvGo.AddComponent<UnityEngine.Canvas>();
    cv.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
    // 适配参考分辨率（与 UILayer 保持一致）
    var scaler = cvGo.AddComponent<UnityEngine.UI.CanvasScaler>();
    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new UnityEngine.Vector2(1920, 1080);
    cvGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
    // 确保有 EventSystem
    if (UnityEngine.EventSystems.EventSystem.current == null) {
        var esGo = new UnityEngine.GameObject("EventSystem");
        esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }
    return "Canvas created: 1920x1080 ScaleWithScreenSize";
}
return "Canvas OK: " + cv.name + ", renderMode=" + cv.renderMode;
```

#### Step 0.5: 工厂 vs 自定义 — 选择策略（每次创建节点前必须判断）

```
┌─ 这个控件在框架组件清单（第一节）里吗？
│      │
│      ├─ 是 → 用工厂方法 CreateXXX() 创建
│      │      │
│      │      ├─ 颜色/字号/尺寸是默认吗？
│      │      │      ├─ 是 → 工厂方法一条搞定，返回后即完成
│      │      │      └─ 否 → 工厂创建后，用 manage_components set_property 调色
│      │      │             或在 execute_code 中手动对返回的 GO 调 .GetComponent<Image>().color
│      │      │
│      │      └─ 需要非标装饰（阴影/描边/底部高光线）？
│      │             └─ 是 → 在工厂返回的 GO 上手动 AddChild 装饰层（见 Step 2 Layout 调优）
│      │
│      └─ 否（装饰元素/分割线/面板根/布局容器）→ 手动 new GameObject + AddComponent
│
├─ 需要设置字段引用（InputField.textViewport / Slider.fillRect / Dropdown.template）？
│      └─ 工厂方法已处理（见 1.4 组件复杂度等级）
│
├─ 面板根节点（背景/描边/圆角）→ 永远手动，工厂不覆盖
│
└─ 布局容器（VerticalLayoutGroup/HorizontalLayoutGroup/ContentSizeFitter）
       → 永远手动，spacing/padding/childAlignment 与具体面板绑定
```

**工厂方法完整清单 + 已知限制**：

| 工厂方法签名                                            | 不会帮你做的事                                                                                                                                              |
| ------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `CreateUIButton(parent, name, pos, size)`             | 不设阴影/底部厚线/圆角；Button 默认白色底                                                                                                                   |
| `CreateUIText(parent, name, pos, size, text)`         | 字体默认白色，字号 24，居中 —— 需手动调色                                                                                                                 |
| `CreateUIImage(parent, name, pos, size, color?)`      | color 参数可选，默认白色                                                                                                                                    |
| `CreateUIInput(parent, name, pos, size, placeholder)` | placeholder 白底浅灰虚文；`TextArea` 已拉伸+留边，`textViewport/textComponent/placeholder` 均已设置字段引用                                             |
| `CreateUIToggle(parent, name, pos, size, isOn)`       | isOn 默认 true；Checkmark 黑色 ✓                                                                                                                           |
| `CreateUISlider(parent, name, pos, size, value)`      | Background+FillArea+Fill+HandleArea+Handle 完整子层级 +`fillRect/handleRect/targetGraphic` 均已设置字段引用                                               |
| `CreateUIDropdown(parent, name, pos, size)`           | Label/Arrow/Template>Viewport(Mask)>Content>Item(Checkmark+ItemLabel) 完整子层级 +`captionText/template/itemText/options` 均已设置字段引用；默认 3 个选项 |
| `CreateUIGrid(parent, name, pos, size)`               | GridLayoutGroup 默认 cell 100×100 spacing (4,4) —— 需手动调                                                                                              |
| `CreateUITree(parent, name, pos, size)`               | RowTemplate(Indent+Expand+Label) 隐藏模板                                                                                                                   |
| `CreateUITabScaffold(parent, name, pos, size)`        | TabStrip + 2 样例 toggle(Tab0/Tab1) + Pages + 2 样例 page；运行时`AutoBind()`                                                                             |
| `CreateUIListView(parent, name, pos, size)`           | Viewport(Mask)>Content(VerticalLayoutGroup) +`viewport/content` 均已设置字段引用                                                                          |
| `CreateUIWindow(parent, name, pos, size)`             | CloseBtn 子节点 + 动画钩子                                                                                                                                  |
| `CreateUIPopup(parent, name, pos, size)`              | Mask(Image+UIImage+Button+UIButton)+Body+CloseBtn + IsModal                                                                                                 |
| `CreateUIToast(parent, name, pos, size, text)`        | Text 子节点 + UIToastView                                                                                                                                   |
| `CreateUILoading(parent, name, pos, size, tip)`       | Tip 子节点 + UILoading + 拉伸铺满                                                                                                                           |

#### Step 0.55: 尺寸规划表（先规划后编码，避免尺寸偏差）

**⚠️ 在写任何 execute_code 之前，必须先输出尺寸规划表给用户确认。**

面板整体尺寸由 Canvas 参考分辨率推算（默认 1920×1080，如有改动在此声明）。AI 拆解完成后，输出如下格式的规划表：

```
## 尺寸规划表

面板整体: <Width>×<Height>，居中 anchor(0.5,0.5)
Canvas 参考分辨率: 1920×1080（面板宽约占 Canvas 的 <百分比>%）

| 节点 | 类型 | 尺寸 | 位置/锚点 | 边距/间距 |
|---|---|---|---|---|
| <PanelName> 根 | Image(背景色) | W×H | anchor(0.5,0.5) | — |
| <PanelName>/InnerBorder | Image(内描边) | (W-6)×(H-6) | 居中 | 比根小 3px |
| <PanelName>/BgPanel | Image(背景) | (W-14)×(H-14) | 居中 | 比根小 7px |
| BgPanel/Txt_Title | UIText | W×H | anchor(L,T) pivot(L,T) | left=N, top=N |
| BgPanel/Btn_Close | UIButton(自定义) | W×H | anchor(R,T) pivot(R,T) | right=N, top=N |
| BgPanel/ContentArea | 容器(VerticalLayoutGroup) | 填满 | anchor(0,0)-(1,1) | top=N, bottom=N, left=N, right=N |
| ContentArea/Row_* | 容器(HorizontalLayoutGroup) | fill×prefH | — | spacing=N |
| ContentArea/Row_*/Label | TMP(LayoutElement) | prefW×fill | — | — |
| ContentArea/Row_*/Dropdown | UIDropdown(工厂+调色) | W×H | — | — |
| BgPanel/Footer | Image(#ECECEC)+HLG | fill×H | anchor(L,B)-(R,B) pivot(C,B) | — |
| Footer/Btn_Cancel | UIButton(自定义) | prefW×prefH | — | spacing=N, padding(L,R,T,B) |
| Footer/Btn_Apply | UIButton(自定义) | prefW×prefH | — | — |

图例: L=Left(0), R=Right(1), T=Top(1), B=Bottom(0), C=Center(0.5)
```

**规划要点**：

1. **先定根面板尺寸**：从描述中的比例推算（如"约 Canvas 宽度的 25-30%"）或从描述中直接提取的绝对值
2. **子组件尺寸来源**：
   - 有工厂方法的 → 工厂参数中的 `size` 就是节点的 `sizeDelta`
   - 自定义的 → 从描述提取的近似值（如"按钮约 35-40px 高"→ 取 38px）
   - 填满父级的 → 标注 `fill`
3. **边距检查**：确保 left+right 边距 ≤ 父级宽度，top+bottom 边距 ≤ 父级高度
4. **Layout 一致性**：VerticalLayoutGroup 的子节点 spacing 和 padding 提前定好，避免后期 Layout 拆开调
5. **Anchor 约定**：
   - 居中 → anchor(0.5,0.5) pivot(0.5,0.5)
   - 左上角 → anchor(0,1) pivot(0,1)
   - 右上角 → anchor(1,1) pivot(1,1)
   - 底部水平条 → anchor(0,0)-(1,0) pivot(0.5,0)

**用户确认后**，AI 将规划表中的数字直接填入 `execute_code`，不自行修改。

#### Step 0.6: 节点命名规范

**AI 拆解面板后为每个子控件命名时，必须遵守以下约定：**

1. **唯一性**：每个子节点的 `name` 在同级下唯一（同一父节点下不重名）
2. **语义化**：名称反映控件用途，而非组件类型 —— 用 `Btn_Start` 而非 `UIButton1`，用 `Txt_Title` 而非 `UIText1`
3. **前缀约定**（推荐）：
   ```
   Btn_*    → UIButton
   Txt_*    → UIText
   Img_*    → UIImage
   Input_*  → UIInput
   Toggle_* → UIToggle
   Slider_* → UISlider
   Drop_*   → UIDropdown
   ```
4. **嵌套深度**：`[UIBind]` 路径必须精确匹配 `Transform.Find` 路径。如果按钮在 ContentArea 下，路径是 `"ContentArea/Btn_Start"` 而非 `"Btn_Start"`。声明 `[UIBind]` 时取相对于 View 根的路径。

#### Step 1: 创建根面板 + 用工厂方法构建子组件

**重要**：`execute_code` 使用 CodeDom (C#6)，**不支持本地函数、元组、`var` 赋值类型**。所有调用必须内联完整限定名。

**模式 A — 用工厂方法（标准 UGUI 包装组件）**：

```csharp
var cv = UnityEngine.Object.FindObjectOfType<UnityEngine.Canvas>();
var root = new UnityEngine.GameObject("<PanelName>", typeof(UnityEngine.RectTransform));
var rootRt = (UnityEngine.RectTransform)root.transform;
rootRt.SetParent(cv.transform, false);
rootRt.anchorMin = rootRt.anchorMax = new UnityEngine.Vector2(0.5f, 0.5f);
rootRt.sizeDelta = new UnityEngine.Vector2(600, 450);
var img = root.AddComponent<UnityEngine.UI.Image>();
img.color = new UnityEngine.Color(r, g, b, a);

// 逐个加子组件（工厂方法返回带完整子层级的 GO）
ZEngine.Editor.UI.UIComponentMenu.CreateUIText(
    (UnityEngine.RectTransform)root.transform, "Title", 
    new UnityEngine.Vector2(0, 190), new UnityEngine.Vector2(400, 40), "标题");

ZEngine.Editor.UI.UIComponentMenu.CreateUIInput(
    (UnityEngine.RectTransform)root.transform, "Input_Username", 
    new UnityEngine.Vector2(0, 120), new UnityEngine.Vector2(400, 44), "请输入...");

ZEngine.Editor.UI.UIComponentMenu.CreateUIButton(
    (UnityEngine.RectTransform)root.transform, "Btn_OK", 
    new UnityEngine.Vector2(0, -130), new UnityEngine.Vector2(140, 44));

// 验证
int total = 0; foreach (var t2 in root.GetComponentsInChildren<UnityEngine.Transform>(true)) total++;
return "panel built. totalTransforms=" + total + " (should be >> childCount)";
```

**模式 B — 自定义样式（非标颜色/描边/阴影，工厂方法不覆盖）**：

参考 `UIComponentMenu` 里的 helper 模式：

```csharp
// 手动创建节点 + 挂 Image/Button/包装组件 + 设颜色
var go = new UnityEngine.GameObject("Btn_Custom", typeof(UnityEngine.RectTransform));
go.transform.SetParent(parent, false);
var goRt = (UnityEngine.RectTransform)go.transform;
goRt.anchorMin = goRt.anchorMax = new UnityEngine.Vector2(0.5f, 0.5f);
goRt.sizeDelta = new UnityEngine.Vector2(200, 44);
var bodyImg = go.AddComponent<UnityEngine.UI.Image>();
bodyImg.color = new UnityEngine.Color(1f, 0.42f, 0.51f, 1f); // 自定义红色
var btn = go.AddComponent<UnityEngine.UI.Button>(); btn.targetGraphic = bodyImg;
go.AddComponent<ZEngine.Manager.UI.UGUI.Components.UIButton>();
// Label...
```

**踩过的坑**：

- `VerticalLayoutGroup` 下子节点的 `Shadow`/`Border` 等装饰层必须设 `LayoutElement.ignoreLayout = true` + `Body` 挂 `LayoutElement.preferredHeight`，否则高度被压缩
- CodeDom 不支持本地函数 → 全部内联；不支持 `var T = TypeName; T.Method()` → 必须全名调用
- `TMP_InputField` 的 `placeholder` 字段不能用 `textArea.transform.Find("Placeholder")` 在创建后立即拿（Transform 层级还没刷新）

#### Step 2: Layout 调优（如有需要）

```csharp
// 调 VerticalLayoutGroup
var vlg = ca.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
vlg.spacing = 16;
vlg.padding = new UnityEngine.RectOffset(10, 10, 10, 10);

// 给 Body 加 LayoutElement，Shadow/Border 加 ignoreLayout
foreach (var name in new string[]{"Btn_A","Btn_B"}) {
    var btn = ca.Find(name); if (btn == null) continue;
    var body = btn.Find("Body");
    if (body != null) {
        var le = body.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        le.preferredHeight = 38f;
    }
    var shadow = btn.Find("Shadow");
    if (shadow != null) shadow.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
    var border = btn.Find("Border");
    if (border != null) border.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
}
```

#### Step 3: 截图验证（每轮必做）

**方式一 — MCP 截图**（查看视觉布局）：

```
mcp__unity-mcp__manage_camera action=screenshot include_image=true screenshot_file_name=<panel>-v#
```

第一轮看布局是否对（控件有没有被截断/重叠），第二轮调间距后确认，第三轮确认最终效果。

**方式二 — execute_code 结构诊断**（查看内部结构）：

```csharp
var root = UnityEngine.GameObject.Find("<PanelName>");
int totalTransforms = 0; 
foreach (var t2 in root.GetComponentsInChildren<UnityEngine.Transform>(true)) totalTransforms++;
// 打印每个子节点的组件
var sb = new System.Text.StringBuilder();
sb.Append("childCount="+root.transform.childCount+" total="+totalTransforms+"\n");
for (int i=0;i<root.transform.childCount;i++) {
    var c = root.transform.GetChild(i);
    sb.Append("  ["+i+"] "+c.name+" comps="+c.GetComponents<UnityEngine.Component>().Length+"\n");
}
return sb.ToString();
```

期望：叶子组件 childCount=0，带子层级组件 >0（如 UIInput childCount≥1，UISlider childCount≥3）。

**方式三 — `manage_prefabs get_hierarchy`**（存 Prefab 后用，最权威）：

```
mcp__unity-mcp__manage_prefabs action=get_hierarchy prefab_path=Assets/GameAssets/Prefabs/UI/<PanelName>.prefab
```

检查：(1) `[UIBind]` 路径是否与 Prefab 实际路径匹配；(2) 每个节点上挂的组件是否符合预期；(3) 接线引用的字段（captionText/template/fillRect 等）是否正确赋值。

#### Step 4: 存 Prefab

```csharp
// MCP: mcp__unity-mcp__manage_prefabs
//   action=create_from_gameobject
//   target=<PanelName>
//   prefab_path=Assets/GameAssets/Prefabs/UI/<PanelName>.prefab
//   unlink_if_instance=true  (如果是覆盖已有 prefab 的实例)
```

#### Step 5: 生成 MVC 脚本

AI 根据 Prefab 中实际存在的 `[UIBind]` 可绑定组件生成三个脚本。**在 View 中 `[UIBind]` 路径必须精确匹配 Prefab 子节点路径**。

**View 模板**：

```csharp
using System;
using ZEngine.Manager.UI;
using ZEngine.Manager.UI.UGUI;
using ZEngine.Manager.UI.UGUI.Components;

namespace Hotfix.Logic.UI.<PanelName>
{
    [UIView("UI/Prefabs/<PanelName>", UUILayer.Middle_Layer, isSingleton: true)]
    public class <PanelName>View : UBaseView
    {
        [UIBind("<Path>")] private <ComponentType> _<camelName>;
        // ... 每个子组件一行

        public override Type ModelType => typeof(<PanelName>Model);
        public override Type ControllerType => typeof(<PanelName>Controller);

        public override void Initialize() { base.Initialize(); }

        public override void OnComplete()
        {
            base.OnComplete();
            // UIButton → _xxx.OnClick += handler;
            // UIToggle → _xxx.OnValueChanged += ...
            // etc.
        }

        public override void OnRelease()
        {
            // 解绑所有事件
            base.OnRelease();
        }
    }
}
```

**Controller 模板**：

```csharp
using ZEngine.Manager.UI.UGUI;
using ZEngine.Manager.UI.UGUI.Components;

namespace Hotfix.Logic.UI.<PanelName>
{
    public class <PanelName>Controller : UBaseController
    {
        private new <PanelName>Model _model => (<PanelName>Model)base._model;
        private new <PanelName>View _view => (<PanelName>View)base._view;

        public override void Initialize()
        {
            base.Initialize();
            // 订阅 View 事件 / 通过 GetChild<T>(path) 取组件接事件
        }

        public override void OnUpdate() { base.OnUpdate(); }

        public override void OnRelease() { /* 解绑事件 */ base.OnRelease(); }
    }
}
```

**Model 模板**：

```csharp
using ZEngine.Manager.UI.UGUI;

namespace Hotfix.Logic.UI.<PanelName>
{
    public class <PanelName>Model : UBaseModel
    {
        // 业务数据字段
        public override void Initialize() { }
        public override void OnRelease() { }
    }
}
```

#### Step 6: 编译验证

```
refresh_unity compile=request mode=force scope=all wait_for_ready=true
→ read_console types=["error"] 确认 0 条
```

---

## 三、提示词模板

### 3.1 Session 开始时加载本手册

```
请先阅读 Assets/Docs/Manual/UGUI-AI-workflow.md 了解本项目的 UGUI 框架和 AI 自动化工作流，然后等待我的指令。
```

### 3.2 图片 → Prefab 生成（发送给 AI）

```
这是一张 UI 面板设计图，请按照 UGUI-AI-workflow.md 中的工作流：
1. 先描述你从图片中识别到的控件类型、位置、层级关系、文本内容
2. 给出组件树（节点名 + ZEngine 包装组件类型）
3. 输出尺寸规划表（节点 → 尺寸/位置/锚点/边距）并等待我确认
4. 确认后用 execute_code 创建完整 Prefab（优先用 UIComponentMenu 工厂方法，非标样式手动构建）
5. 截图验证
6. 存 Prefab 到 Assets/GameAssets/Prefabs/UI/
7. 生成 View/Model/Controller 三个脚本
```

### 3.3 自然语言描述 → Prefab

```
请按UGUI-AI-workflow.md的工作流程创建一个面板：
[面板描述]
- 标题 "xxx" UIText居中
- 输入框 "xxx" UIInput
- 开关 "xxx" UIToggle
- 确认按钮 UIButton
...

要求：先输出尺寸规划表给我确认，确认后再执行创建。
```

### 3.4 从已有 Prefab 反推 MVC（只生成脚本）

```
请按UGUI-AI-workflow.md的 Step 5 模式，从 Assets/GameAssets/Prefabs/UI/<PanelName>.prefab 
反推生成 View/Model/Controller 三个脚本到 Assets/GameScripts/Hotfix/Logic/UI/<PanelName>/
```

### 3.5 编译验证命令（每次生成脚本后必做）

```
Please refresh Unity and check console for errors.
If there are errors, fix them and recompile.
```

---

## 四、文件路径约定

| 资源                         | 路径                                                                          |
| ---------------------------- | ----------------------------------------------------------------------------- |
| Prefab                       | `Assets/GameAssets/Prefabs/UI/<PanelName>.prefab`                           |
| View                         | `Assets/GameScripts/Hotfix/Logic/UGUI/<PanelName>/<PanelName>View.cs`       |
| Model                        | `Assets/GameScripts/Hotfix/Logic/UGUI/<PanelName>/<PanelName>Model.cs`      |
| Controller                   | `Assets/GameScripts/Hotfix/Logic/UGUI/<PanelName>/<PanelName>Controller.cs` |
| View 的`[UIView]` location | `"UI/Prefabs/<PanelName>"`                                                  |

---

## 五、Execute_Code 常见陷阱速查

| 陷阱                                                         | 解法                                                                                                                      |
| ------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------- |
| CodeDom 不支持本地函数                                       | 全部内联，每个操作写完整代码                                                                                              |
| CodeDom 不支持`var T = Namespace.Type; T.Method()`         | 使用完整限定名`Namespace.Type.Method(...)`                                                                              |
| CodeDom 不支持元组                                           | 用`out` 参数或返回字符串拼接                                                                                            |
| `VerticalLayoutGroup` + 子节点带 decorative 层 → 高度 = 0 | `Body` 挂 `LayoutElement.preferredHeight`，Shadow/Border 挂 `ignoreLayout = true`                                   |
| `VerticalLayoutGroup.childAlignment` 被忽略                | 确保`childControlWidth=true, childForceExpandWidth=true`，子节点 stretch 填满                                           |
| `TMP_InputField` 的 placeholder 在创建瞬间拿不到           | 在第二步 layout 调优时，通过`GetComponent<TMP_InputField>().placeholder` 设置                                           |
| `TMP_Dropdown` 模板创建后 `AddOptions` 不生效            | 确保 Template/Viewport/Content 层级存在，`Content` 挂 `VerticalLayoutGroup` + `ContentSizeFitter`（工厂方法已处理） |
| `manage_prefabs` 报 "already linked to prefab"             | 加`unlink_if_instance=true`                                                                                             |
| 多个同名重名 GO 在场景中 →`GameObject.Find` 返回不确定    | 清理残留：用`FindObjectsOfType<Transform>` 全删后再创                                                                   |
| `UBaseView.ModelType/ControllerType` 无法赋值              | 用`override` 属性而非 `Initialize` 内赋值                                                                             |
| `apply_text_edits` / `script_apply_edits` 匹配到错误行   | 先`Read` 确认内容，再 `get_sha` 取 sha256，最后 `apply_text_edits` 带 `precondition_sha256`                       |
| Prefab 实例化后`GameObject.Find` 找不到                    | Prefab 实例可能在 Canvas 下，用`FindObjectsOfType<Transform>` 搜索                                                      |
| `execute_code` 看不到 HotUpdate 程序集                     | 不需要——View 用`create_script` 创建（在 HotUpdate 程序集内，编译时处理）                                              |

---

## 六、框架约束速查（避免生成编译错误）

| 约束                    | 说明                                                                                                                                                 |
| ----------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| `[UIBind]` 路径       | 精确匹配 Prefab 中子节点相对于 View 根的`Transform.Find` 路径                                                                                      |
| `UIView` 的第一个参数 | `"UI/Prefabs/<PanelName>"` — 不带 `Assets/GameAssets/` 前缀，不带 `.prefab` 后缀                                                              |
| `using` 引用          | View 需要`using ZEngine.Manager.UI;` + `.UGUI;` + `.UGUI.Components;`；Controller 也需要 `.Components;` 才能用包装类型                       |
| HotUpdate 程序集        | 已引用 Zengine + TMP，`Components` 命名空间可用                                                                                                    |
| 组件事件                | UIButton →`OnClick`(Action)；UIToggle/UISlider/UIDropdown → `OnValueChanged`(Action<T></t>)；UIInput → `OnEndEdit`(Action<string></string>) |
| 组件生命周期            | `OnInit` 接线 / `OnRelease` 解线 —— 在 View 的 `OnComplete` 里用 `+=`，在 `OnRelease` 里用 `-=`                                        |

---

## 七、MCP 工具选择策略

| 操作                                           | 优先工具                                                  | 备注                                                                                          |
| ---------------------------------------------- | --------------------------------------------------------- | --------------------------------------------------------------------------------------------- |
| 创建单个组件（有工厂方法）                     | `execute_code` 调 `UIComponentMenu.CreateXXX()`       | 一条代码完成子层级+接线，比`manage_gameobject create` + 逐层 `manage_components` 快 10 倍 |
| 创建面板根节点                                 | `manage_gameobject create` 或 `execute_code`          | 简单操作，两者等效                                                                            |
| 修改已创建 GO 的属性（颜色/尺寸/锚点）         | `manage_components set_property`                        | MCP 原生支持，无需写代码                                                                      |
| 批量创建多个组件                               | `execute_code` 一次性调多个工厂方法                     | 避免 N 次 MCP 往返                                                                            |
| 手动创建复杂节点（含特殊子层级）               | `execute_code`                                          | `new GameObject + AddComponent` 链式调用                                                    |
| Layout 调优（改 spacing/padding/ignoreLayout） | `execute_code`                                          | 需要 foreach 遍历 child，MCP 做不到                                                           |
| 存 Prefab                                      | `manage_prefabs create_from_gameobject`                 | MCP 唯一方式                                                                                  |
| 创建脚本                                       | `create_script`                                         | MCP 唯一方式                                                                                  |
| 截图验证                                       | `manage_camera screenshot`                              | MCP 唯一方式                                                                                  |
| 编译验证                                       | `refresh_unity` + `read_console`                      | MCP 唯一方式                                                                                  |
| 反推脚本                                       | `Tools/ZEngine/UGUI MVC Generator` 或 `create_script` | Editor 窗口方式更省力（扫描 Prefab 自动生成 [UIBind] 字段）                                   |

**核心原则**：**创建走 execute_code，存储走 MCP**。`execute_code` 可以把 N 次 MCP create+set 调用压缩成一条代码，Debug 也快（返回字符串即反馈）；`manage_prefabs`/`create_script`/`manage_camera` 是 MCP 独有的持久化和可视化能力，两者互补。`manage_gameobject create` 只在创建极简单的单节点（无子层级）时用。
