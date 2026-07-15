//------------------------------
// ZEngine - 全组件测试面板（单场景切换 + 运行时交互）
// 用途：PlayMode 下经 InitNode → UUIManager.OpenViewSync<ComponentTestView>() 打开。
//       左栏列出框架全部组件，点击切换：右栏「Stage」加载该组件的测试预制体（Yoo 路径
//       "Prefabs/UI/Test/Test_<X>"，由 ComponentTestPrefabGenerator 生成）并接线交互事件；
//       底部「交互日志」实时打印操作信息（同时输出 Console）。
//       窗口族（UIWindow/UIPopup/UIDialog）经 UUIManager 打开到 Window 层（不走 Stage）。
// 依赖：先在 Unity 运行菜单 Tools/ZEngine/生成全组件测试Prefab 生成预制体。
// 生命周期关键点：Stage 内实例化的包装组件不会自动 OnInit（BuildChildCache 仅在 View 初始化时跑一次），
//                故加载后手动 InitComponents → OnInit（如 UIButton.OnInit 才会把 Button.onClick 接到 OnClick）。
//------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using YooAsset;
using ZEngine.Manager.UI;
using ZEngine.Manager.UI.UGUI;
using ZEngine.Manager.UI.UGUI.Components;
using ZEngine.Manager.UI.UGUI.Animation;
using ZEngine.Manager.Resource;

namespace Hotfix.Logic.UI
{
    [UIView("Prefabs/UI/ComponentTest", UUILayer.Middle_Layer, isSingleton: true)]
    public class ComponentTestView : UBaseView
    {
        public override Type ModelType => typeof(UBaseModel);
        public override Type ControllerType => typeof(UBaseController);

        // ── 布局节点 ──
        private RectTransform _stage;          // 右栏：组件实例化区
        private RectTransform _ctrlBar;         // 右栏顶部：每组件的控制按钮条
        private TextMeshProUGUI _logTmp;       // 底部日志文本
        private readonly List<string> _logLines = new List<string>();

        // ── 资源句柄/实例（切换时清理，避免引用计数泄漏）──
        private AssetHandle _currentHandle;    // 当前 Stage 预制体句柄
        private GameObject _currentInstance;   // 当前 Stage 实例
        private AssetHandle _cellHandle;       // Test_Cell 预制体句柄（UIGrid/UIListView 共用，全程持有）
        private GameObject _cellPrefab;
        private RectTransform _animTarget;      // UIAnimation 演示目标（UIAnimation 专用）

        // ── 目录 ──
        private struct Entry { public string name; public Action onSelect; }
        private readonly List<Entry> _entries = new List<Entry>();
        private readonly List<GameObject> _listButtons = new List<GameObject>();
        private int _selected = -1;

        // ═══════════════════════════════════════════
        //  生命周期
        // ═══════════════════════════════════════════

        public override void OnComplete()
        {
            base.OnComplete();
            // 根节点铺满 Middle 层（预制体根为 1920×1080 居中，这里改为拉伸自适应）
            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero; root.offsetMax = Vector2.zero;
            if (root.GetComponent<Image>() == null)
            { var img = root.gameObject.AddComponent<Image>(); img.color = new Color(0.12f, 0.12f, 0.18f, 1f); }

            BuildLayout();
            LoadCellPrefab();
            BuildCatalog();
            BuildListButtons();
            Log("就绪：左栏点组件名切换，右栏交互，此处实时打印。");
            if (_entries.Count > 0) Select(0); // 默认 UIButton
        }

        public override void OnRelease()
        {
            ClearStage();
            if (_cellHandle != null) { _cellHandle.Release(); _cellHandle = null; _cellPrefab = null; }
            // 关闭可能仍打开的测试窗口
            if (UUIManager.Instance != null)
            {
                UUIManager.Instance.CloseView<TestUIWindowView>();
                UUIManager.Instance.CloseView<TestUIPopupView>();
                UUIManager.Instance.CloseView<TestUIDialogView>();
            }
            base.OnRelease();
        }

        // ═══════════════════════════════════════════
        //  布局
        // ═══════════════════════════════════════════

        private void BuildLayout()
        {
            // 标题条
            var title = NewGO("Title", transform, typeof(RectTransform), typeof(TextMeshProUGUI));
            SetRect((RectTransform)title.transform, new Vector2(0, 1), Vector2.one, Vector2.zero, new Vector2(0, 36), new Vector2(0.5f, 1f));
            var tt = title.GetComponent<TextMeshProUGUI>();
            tt.text = "ZEngine 全组件测试 ｜ 左栏切换 · 右栏交互 · 底部日志";
            tt.fontSize = 18; tt.fontStyle = FontStyles.Bold; tt.alignment = TextAlignmentOptions.Center;
            tt.color = new Color(0.07f, 0.66f, 0.90f, 1f); tt.raycastTarget = false;

            // 底部日志面板
            var log = NewGO("LogPanel", transform, typeof(RectTransform), typeof(Image));
            SetRect((RectTransform)log.transform, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 150), new Vector2(0.5f, 0f));
            log.GetComponent<Image>().color = new Color(0.10f, 0.10f, 0.14f, 1f);
            var logRt = (RectTransform)log.transform;
            // 清空按钮
            var clr = NewGO("Btn_Clear", logRt, typeof(RectTransform), typeof(Image), typeof(Button));
            var clrRt = (RectTransform)clr.transform;
            clrRt.anchorMin = new Vector2(1, 1); clrRt.anchorMax = new Vector2(1, 1); clrRt.pivot = new Vector2(1, 1);
            clrRt.sizeDelta = new Vector2(70, 24); clrRt.anchoredPosition = new Vector2(-6, -4);
            clr.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.36f, 1f);
            var clrLbl = NewGO("L", clrRt, typeof(RectTransform), typeof(TextMeshProUGUI));
            Stretch((RectTransform)clrLbl.transform);
            var clt = clrLbl.GetComponent<TextMeshProUGUI>(); clt.text = "清空"; clt.fontSize = 13; clt.alignment = TextAlignmentOptions.Center; clt.color = Color.white; clt.raycastTarget = false;
            clr.GetComponent<Button>().onClick.AddListener(() => { _logLines.Clear(); if (_logTmp != null) _logTmp.text = ""; });
            // 日志文本（可滚动）
            var logScroll = NewGO("Scroll", logRt, typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
            Stretch((RectTransform)logScroll.transform);
            logScroll.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            logScroll.GetComponent<Mask>().showMaskGraphic = false;
            var vp = NewGO("Viewport", logScroll.transform, typeof(RectTransform), typeof(Image));
            Stretch((RectTransform)vp.transform); vp.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            var content = NewGO("Content", vp.transform, typeof(RectTransform));
            var ctRt = (RectTransform)content.transform;
            ctRt.anchorMin = new Vector2(0, 1); ctRt.anchorMax = Vector2.one; ctRt.pivot = new Vector2(0.5f, 1f);
            ctRt.sizeDelta = Vector2.zero;
            content.AddComponent<VerticalLayoutGroup>().childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var sr = logScroll.GetComponent<ScrollRect>();
            sr.viewport = (RectTransform)vp.transform; sr.content = ctRt; sr.vertical = true; sr.horizontal = false;
            var logTmpGo = NewGO("LogText", content.transform, typeof(RectTransform), typeof(TextMeshProUGUI));
            var ltRt = (RectTransform)logTmpGo.transform; ltRt.anchorMin = new Vector2(0, 1); ltRt.anchorMax = Vector2.one; ltRt.pivot = new Vector2(0.5f, 1f);
            _logTmp = logTmpGo.GetComponent<TextMeshProUGUI>();
            _logTmp.text = ""; _logTmp.fontSize = 15; _logTmp.alignment = TextAlignmentOptions.TopLeft;
            _logTmp.color = new Color(0.85f, 0.9f, 0.7f, 1f); _logTmp.raycastTarget = false;

            // 左栏列表（可滚动）
            var list = NewGO("ListArea", transform, typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(Mask));
            SetRect((RectTransform)list.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 160), new Vector2(280, -44));
            list.GetComponent<Image>().color = new Color(0.14f, 0.14f, 0.18f, 1f);
            list.GetComponent<Mask>().showMaskGraphic = false;
            var lvp = NewGO("Viewport", list.transform, typeof(RectTransform), typeof(Image), typeof(Mask));
            Stretch((RectTransform)lvp.transform); lvp.GetComponent<Image>().color = new Color(0, 0, 0, 0); lvp.GetComponent<Mask>().showMaskGraphic = false;
            var lct = NewGO("Content", lvp.transform, typeof(RectTransform));
            var lctRt = (RectTransform)lct.transform;
            lctRt.anchorMin = new Vector2(0, 1); lctRt.anchorMax = Vector2.one; lctRt.pivot = new Vector2(0.5f, 1f); lctRt.sizeDelta = Vector2.zero;
            var lvlg = lct.AddComponent<VerticalLayoutGroup>();
            lvlg.childControlWidth = true; lvlg.childForceExpandWidth = true;
            lvlg.childControlHeight = true; lvlg.childForceExpandHeight = false;
            lvlg.spacing = 4; lvlg.padding = new RectOffset(6, 6, 8, 8);
            lct.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var lsr = list.GetComponent<ScrollRect>();
            lsr.viewport = (RectTransform)lvp.transform; lsr.content = lctRt; lsr.vertical = true; lsr.horizontal = false;
            _listAreaContent = lctRt;

            // 右栏（控制条 + Stage）
            var right = NewGO("RightArea", transform, typeof(RectTransform), typeof(Image));
            SetRect((RectTransform)right.transform, new Vector2(0, 0), Vector2.one, new Vector2(290, 160), new Vector2(0, -44));
            right.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.22f, 1f);
            var ctrl = NewGO("CtrlBar", right.transform, typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            var ctrlRt = (RectTransform)ctrl.transform;
            ctrlRt.anchorMin = new Vector2(0, 1); ctrlRt.anchorMax = Vector2.one; ctrlRt.pivot = new Vector2(0.5f, 1f); ctrlRt.sizeDelta = new Vector2(0, 44);
            ctrl.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 1f);
            var chlg = ctrl.GetComponent<HorizontalLayoutGroup>();
            chlg.childControlWidth = false; chlg.childForceExpandWidth = false;
            chlg.childControlHeight = true; chlg.childForceExpandHeight = true;
            chlg.spacing = 6; chlg.padding = new RectOffset(8, 8, 6, 6); chlg.childAlignment = TextAnchor.MiddleLeft;
            _ctrlBar = ctrlRt;
            var stage = NewGO("Stage", right.transform, typeof(RectTransform), typeof(Image));
            SetRect((RectTransform)stage.transform, Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(8, 52));
            stage.GetComponent<Image>().color = new Color(0.13f, 0.13f, 0.17f, 1f);
            _stage = (RectTransform)stage.transform;
        }

        private RectTransform _listAreaContent;

        // ═══════════════════════════════════════════
        //  目录（全部组件）
        // ═══════════════════════════════════════════

        private void BuildCatalog()
        {
            Add("UIButton",      BuildUIButton);
            Add("UIText",         BuildUIText);
            Add("UIImage",        BuildUIImage);
            Add("UIInput",        BuildUIInput);
            Add("UIToggle",       BuildUIToggle);
            Add("UISlider",       BuildUISlider);
            Add("UIDropdown",     BuildUIDropdown);
            Add("UIGrid",         BuildUIGrid);
            Add("UITree",         BuildUITree);
            Add("UITab",          BuildUITab);
            Add("UIListView",     BuildUIListView);
            Add("UIProgressBar",  BuildUIProgressBar);
            Add("UIRadioGroup",   BuildUIRadioGroup);
            Add("UIScrollbar",    BuildUIScrollbar);
            Add("UICountdown",    BuildUICountdown);
            Add("UIPagination",   BuildUIPagination);
            Add("UIDraggable",    BuildUIDraggable);
            Add("UIRawImage",     BuildUIRawImage);
            Add("UITooltip",      BuildUITooltip);
            Add("UIToast",        BuildUIToast);
            Add("UILoading",      BuildUILoading);
            Add("UIAnimation",    BuildUIAnimation);
            Add("UIWindow",       BuildUIWindow);
            Add("UIPopup",        BuildUIPopup);
            Add("UIDialog",       BuildUIDialog);
        }

        private void Add(string name, Action onSelect) => _entries.Add(new Entry { name = name, onSelect = onSelect });

        // ═══════════════════════════════════════════
        //  选择 / 清理 / 加载
        // ═══════════════════════════════════════════

        private void BuildListButtons()
        {
            var accent = new Color(0.07f, 0.66f, 0.90f, 1f);
            var normal = new Color(0.22f, 0.22f, 0.28f, 1f);
            for (int i = 0; i < _entries.Count; i++)
            {
                int idx = i;
                var go = NewGO("Item_" + _entries[i].name, _listAreaContent, typeof(RectTransform), typeof(Image), typeof(Button));
                go.GetComponent<Image>().color = normal;
                go.AddComponent<LayoutElement>().preferredHeight = 32;
                go.GetComponent<Button>().onClick.AddListener(() => Select(idx));
                var lbl = NewGO("L", go.transform, typeof(RectTransform), typeof(TextMeshProUGUI));
                Stretch((RectTransform)lbl.transform);
                var t = lbl.GetComponent<TextMeshProUGUI>();
                t.text = _entries[i].name; t.fontSize = 14; t.alignment = TextAlignmentOptions.Left;
                t.color = Color.white; t.raycastTarget = false; t.margin = new Vector4(10, 0, 0, 0);
                _listButtons.Add(go);
            }
        }

        private void Select(int i)
        {
            if (i < 0 || i >= _entries.Count) return;
            _selected = i;
            var accent = new Color(0.07f, 0.66f, 0.90f, 1f);
            var normal = new Color(0.22f, 0.22f, 0.28f, 1f);
            for (int k = 0; k < _listButtons.Count; k++)
            {
                var img = _listButtons[k].GetComponent<Image>();
                if (img != null) img.color = (k == i) ? accent : normal;
            }
            try { _entries[i].onSelect(); }
            catch (Exception e) { Debug.LogException(e); Log("[错误] " + e.Message); }
        }

        private void ClearStage()
        {
            // 清控制按钮
            if (_ctrlBar != null)
                for (int i = _ctrlBar.childCount - 1; i >= 0; i--)
                    Destroy(_ctrlBar.GetChild(i).gameObject);
            // 清 Stage（先解线再销毁）
            if (_stage != null)
            {
                if (_currentInstance != null) ReleaseComponents(_currentInstance);
                for (int i = _stage.childCount - 1; i >= 0; i--)
                    Destroy(_stage.GetChild(i).gameObject);
            }
            _currentInstance = null;
            _animTarget = null;
            if (_currentHandle != null) { _currentHandle.Release(); _currentHandle = null; }
        }

        // 加载预制体到 Stage 并手动 OnInit 包装组件；返回实例（失败返回 null）
        private GameObject LoadToStage(string location)
        {
            ClearStage();
            var handle = ResourceManager.Instance.LoadAssetSync<GameObject>(location);
            if (handle == null || handle.AssetObject == null)
            {
                Log("未找到预制体: " + location + "（先运行菜单 Tools/ZEngine/生成全组件测试Prefab）");
                if (handle != null) handle.Release();
                return null;
            }
            _currentHandle = handle;
            _currentInstance = Instantiate((GameObject)handle.AssetObject, _stage, false);
            var rt = (RectTransform)_currentInstance.transform;
            rt.anchoredPosition = Vector2.zero;
            InitComponents(_currentInstance);
            return _currentInstance;
        }

        // 手动对实例化层级的所有 UIComponentBase 调 OnInit（弥补 BuildChildCache 不再触发的缺口）
        private void InitComponents(GameObject root)
        {
            foreach (var c in root.GetComponentsInChildren<UIComponentBase>(true))
            {
                try { c.OnInit(); } catch (Exception e) { Debug.LogException(e); }
            }
        }

        private void ReleaseComponents(GameObject root)
        {
            foreach (var c in root.GetComponentsInChildren<UIComponentBase>(true))
            {
                try { c.OnRelease(); } catch (Exception e) { Debug.LogException(e); }
            }
        }

        private void LoadCellPrefab()
        {
            var handle = ResourceManager.Instance.LoadAssetSync<GameObject>("Prefabs/UI/Test/Test_Cell");
            if (handle != null && handle.AssetObject != null)
            {
                _cellHandle = handle;
                _cellPrefab = (GameObject)handle.AssetObject;
            }
            else
            {
                if (handle != null) handle.Release();
                Log("Test_Cell 预制体未找到，UIGrid/UIListView 将无法填充（先运行生成菜单）");
            }
        }

        // ═══════════════════════════════════════════
        //  基础控件
        // ═══════════════════════════════════════════

        private void BuildUIButton()
        {
            var go = LoadToStage("Prefabs/UI/Test/Test_UIButton");
            if (go == null) return;
            var b = go.GetComponent<UIButton>();
            if (b != null) b.OnClick += () => Log("[UIButton] OnClick 触发 ✓");
            Log("[UIButton] 已加载，点击右侧按钮测试 OnClick 事件");
        }

        private void BuildUIText()
        {
            var go = LoadToStage("Prefabs/UI/Test/Test_UIText");
            if (go == null) return;
            var t = go.GetComponent<UIText>();
            AddCtrl("改文本", () => { if (t != null) t.SetText("文本已变更 " + UnityEngine.Random.Range(0, 100)); });
            AddCtrl("变绿", () => { if (t != null) t.SetColor(Color.green); });
            AddCtrl("变白", () => { if (t != null) t.SetColor(Color.white); });
            Log("[UIText] 已加载，用上方按钮测试 SetText/SetColor（当前文本: " + (t != null ? t.GetText() : "null") + "）");
        }

        private void BuildUIImage()
        {
            var go = LoadToStage("Prefabs/UI/Test/Test_UIImage");
            if (go == null) return;
            var im = go.GetComponent<UIImage>();
            // 演示 SetFillAmount 需 Image 为 Filled 类型
            var raw = go.GetComponent<Image>();
            if (raw != null) { raw.type = Image.Type.Filled; raw.fillMethod = Image.FillMethod.Horizontal; }
            AddCtrl("填充 50%", () => { if (im != null) im.SetFillAmount(0.5f); });
            AddCtrl("填充 100%", () => { if (im != null) im.SetFillAmount(1f); });
            AddCtrl("随机变色", () => { if (im != null) im.SetColor(UnityEngine.Random.ColorHSV()); });
            Log("[UIImage] 已加载（已切 Filled 模式），按钮测试 SetFillAmount/SetColor");
        }

        private void BuildUIInput()
        {
            var go = LoadToStage("Prefabs/UI/Test/Test_UIInput");
            if (go == null) return;
            var inp = go.GetComponent<UIInput>();
            if (inp != null)
            {
                inp.OnValueChanged += s => Log("[UIInput] 值变化: " + s);
                inp.OnEndEdit += s => Log("[UIInput] 输入完成: " + s);
            }
            AddCtrl("程序设值", () => { if (inp != null) inp.SetValue("预设内容 " + UnityEngine.Random.Range(0, 100)); });
            Log("[UIInput] 已加载，在输入框输入文字并回车（OnEndEdit），或点程序设值");
        }

        private void BuildUIToggle()
        {
            var go = LoadToStage("Prefabs/UI/Test/Test_UIToggle");
            if (go == null) return;
            var tg = go.GetComponent<UIToggle>();
            if (tg != null) tg.OnValueChanged += v => Log("[UIToggle] isOn = " + v);
            Log("[UIToggle] 已加载，点击勾选/取消测试 OnValueChanged");
        }

        private void BuildUISlider()
        {
            var go = LoadToStage("Prefabs/UI/Test/Test_UISlider");
            if (go == null) return;
            var sl = go.GetComponent<UISlider>();
            if (sl != null) sl.OnValueChanged += v => Log("[UISlider] value = " + v.ToString("F2"));
            Log("[UISlider] 已加载，拖动滑块测试 OnValueChanged");
        }

        private void BuildUIDropdown()
        {
            var go = LoadToStage("Prefabs/UI/Test/Test_UIDropdown");
            if (go == null) return;
            var dd = go.GetComponent<UIDropdown>();
            if (dd != null) dd.OnValueChanged += i => Log("[UIDropdown] index = " + i + " text = " + dd.GetSelectedText());
            Log("[UIDropdown] 已加载，展开选择选项测试 OnValueChanged");
        }

        // ═══════════════════════════════════════════
        //  容器/列表
        // ═══════════════════════════════════════════

        private void BuildUIGrid()
        {
            var go = LoadToStage("Prefabs/UI/Test/Test_UIGrid");
            if (go == null) return;
            var grid = go.GetComponent<UIGrid>();
            if (grid == null) { Log("[UIGrid] 未取到 UIGrid 组件"); return; }
            if (_cellPrefab == null) { Log("[UIGrid] Test_Cell 未加载，无法填充"); return; }
            var data = new List<string>();
            for (int i = 0; i < 8; i++) data.Add("G" + i);
            grid.SetData(data, _cellPrefab, (i, v, t) =>
            {
                var tmp = t.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = v;
            });
            Log("[UIGrid] SetData 填充 " + grid.Count + " 个 cell（数据驱动 GridLayoutGroup）");
        }

        private class TreeNode
        {
            public string Name;
            public List<TreeNode> Children = new List<TreeNode>();
        }

        private void BuildUITree()
        {
            var go = LoadToStage("Prefabs/UI/Test/Test_UITree");
            if (go == null) return;
            var tree = go.GetComponent<UITree>();
            if (tree == null) { Log("[UITree] 未取到 UITree 组件"); return; }
            var rowPrefab = go.transform.Find("RowTemplate")?.gameObject;
            if (rowPrefab == null) { Log("[UITree] 未找到 RowTemplate 子节点"); return; }
            var root = new TreeNode { Name = "根节点" };
            var a = new TreeNode { Name = "子节点 A" };
            a.Children.Add(new TreeNode { Name = "孙节点 A1" });
            a.Children.Add(new TreeNode { Name = "孙节点 A2" });
            root.Children.Add(a);
            root.Children.Add(new TreeNode { Name = "子节点 B" });

            tree.OnNodeSelected += n => Log("[UITree] 选中节点: " + (n != null ? ((TreeNode)n).Name : "null"));
            tree.SetData(root, n => n.Children, rowPrefab, (depth, node, t) =>
            {
                var lbl = t.Find("Label")?.GetComponent<TextMeshProUGUI>();
                if (lbl != null) lbl.text = new string(' ', depth * 2) + node.Name;
                var exp = t.Find("Expand")?.GetComponent<Button>();
                if (exp != null)
                {
                    exp.onClick.RemoveAllListeners();
                    exp.onClick.AddListener(() => tree.Toggle(node));
                    var icon = t.Find("Expand/Icon")?.GetComponent<TextMeshProUGUI>();
                    if (icon != null) icon.text = tree.IsExpanded(node) ? "v" : ">";
                }
            });
            Log("[UITree] 树已加载，点行首 > 展开子节点（再点折叠）");
        }

        private void BuildUITab()
        {
            var go = LoadToStage("Prefabs/UI/Test/Test_UITab");
            if (go == null) return;
            var tab = go.GetComponent<UITab>();
            if (tab == null) { Log("[UITab] 未取到 UITab 组件"); return; }
            tab.OnTabChanged += i => Log("[UITab] 切到 Tab " + i);
            tab.AutoBind();   // 按约定扫描 TabStrip/Pages 自动配对
            tab.Select(0, false);
            Log("[UITab] AutoBind 配对 " + tab.Count + " 个 Tab，点顶部 Tab0/Tab1 切换");
        }

        private void BuildUIListView()
        {
            var go = LoadToStage("Prefabs/UI/Test/Test_UIListView");
            if (go == null) return;
            var list = go.GetComponent<UIListView>();
            if (list == null) { Log("[UIListView] 未取到 UIListView 组件"); return; }
            if (_cellPrefab == null) { Log("[UIListView] Test_Cell 未加载，无法填充"); return; }
            var data = new List<string>();
            for (int i = 0; i < 30; i++) data.Add("Item " + i);
            list.SetData(data, _cellPrefab, (i, v, t) =>
            {
                var tmp = t.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = v;
            });
            AddCtrl("滚到第15项", () => list.ScrollToCell(15, 10, 0, UIListView.UIScrollMode.ToCenter));
            AddCtrl("刷新数据", () => list.Refresh());
            Log("[UIListView] SetData " + list.TotalCount + " 项（虚拟化，需 PlayMode 真填充；RefillCells 在 !isPlaying 早返回）");
        }

        // ═══════════════════════════════════════════
        //  拓展组件
        // ═══════════════════════════════════════════

        private void BuildUIProgressBar()
        {
            var go = LoadToStage("Prefabs/UI/Test/Test_UIProgressBar");
            if (go == null) return;
            var pb = go.GetComponent<UIProgressBar>();
            if (pb == null) { Log("[UIProgressBar] 未取到组件"); return; }
            pb.SetProgress(0.3f);
            AddCtrl("25%", () => { if (pb != null) pb.SetProgress(0.25f); Log("[UIProgressBar] Progress = " + pb.Progress); });
            AddCtrl("50%", () => { if (pb != null) pb.SetProgress(0.5f); Log("[UIProgressBar] Progress = " + pb.Progress); });
            AddCtrl("80%", () => { if (pb != null) pb.SetProgress(0.8f); Log("[UIProgressBar] Progress = " + pb.Progress); });
            AddCtrl("动画到100%", () => { if (pb != null) pb.AnimateTo(1f, 1f); });
            Log("[UIProgressBar] 已加载，按钮测试 SetProgress / AnimateTo（当前 " + pb.Progress + "）");
        }

        private void BuildUIRadioGroup()
        {
            var go = LoadToStage("Prefabs/UI/Test/Test_UIRadioGroup");
            if (go == null) return;
            var rg = go.GetComponent<UIRadioGroup>();
            if (rg == null) { Log("[UIRadioGroup] 未取到组件"); return; }
            // 在容器内放 3 个 UIToggle，先建好再统一 OnInit（避免重复订阅）
            var hlg = go.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) { hlg = go.AddComponent<HorizontalLayoutGroup>(); hlg.spacing = 16; hlg.childAlignment = TextAnchor.MiddleLeft; }
            var toggles = new UIToggle[3];
            for (int i = 0; i < 3; i++)
            {
                var tg = MakeRadioToggle(go.transform, "Opt_" + i);
                toggles[i] = tg;
            }
            InitComponents(go); // 现在才 OnInit 各 UIToggle（Toggle.onValueChanged → OnValueChanged）
            for (int i = 0; i < 3; i++) rg.Add(toggles[i]);
            rg.OnValueChanged += t => Log("[UIRadioGroup] 选中: " + rg.Value + "（互斥单选）");
            rg.SelectByIndex(0, false);
            Log("[UIRadioGroup] 已加入 3 个互斥选项，点击切换测试 OnValueChanged");
        }

        private void BuildUIScrollbar()
        {
            var go = LoadToStage("Prefabs/UI/Test/Test_UIScrollbar");
            if (go == null) return;
            var sb = go.GetComponent<UIScrollbar>();
            if (sb != null) sb.OnValueChanged += v => Log("[UIScrollbar] value = " + v.ToString("F2"));
            Log("[UIScrollbar] 已加载，拖动 Handle 测试 OnValueChanged");
        }

        private void BuildUICountdown()
        {
            var go = LoadToStage("Prefabs/UI/Test/Test_UICountdown");
            if (go == null) return;
            var cd = go.GetComponent<UICountdown>();
            if (cd == null) { Log("[UICountdown] 未取到组件"); return; }
            cd.OnEnd += () => Log("[UICountdown] 倒计时结束 ✓");
            AddCtrl("开始 30s", () => { if (cd != null) cd.SetCountdown(30).Begin(); Log("[UICountdown] Begin 30s"); });
            AddCtrl("暂停", () => { if (cd != null) cd.Pause(); Log("[UICountdown] Pause"); });
            AddCtrl("停止", () => { if (cd != null) cd.Stop(); Log("[UICountdown] Stop"); });
            Log("[UICountdown] 已加载，点按钮开始/暂停/停止倒计时（mm:ss）");
        }

        private void BuildUIPagination()
        {
            var go = LoadToStage("Prefabs/UI/Test/Test_UIPagination");
            if (go == null) return;
            var pg = go.GetComponent<UIPagination>();
            if (pg == null) { Log("[UIPagination] 未取到组件"); return; }
            pg.SetPage(1, 5);
            pg.OnPageChanged += p => Log("[UIPagination] 当前页 = " + p + " / " + pg.TotalPages);
            Log("[UIPagination] 已加载（1/5），点 < > 翻页测试 OnPageChanged");
        }

        private void BuildUIDraggable()
        {
            var go = LoadToStage("Prefabs/UI/Test/Test_UIDraggable");
            if (go == null) return;
            var dg = go.GetComponent<UIDraggable>();
            if (dg == null) { Log("[UIDraggable] 未取到组件"); return; }
            dg.OnDragging += d => Log("[UIDraggable] 拖拽中 delta = " + d);
            dg.OnDragEnd += d => Log("[UIDraggable] 拖拽结束");
            AddCtrl("还原位置", () => { if (dg != null) dg.RestorePosition(); Log("[UIDraggable] 已还原"); });
            Log("[UIDraggable] 已加载，拖拽右侧方块测试拖拽事件");
        }

        private void BuildUIRawImage()
        {
            var go = LoadToStage("Prefabs/UI/Test/Test_UIRawImage");
            if (go == null) return;
            var ri = go.GetComponent<UIRawImage>();
            if (ri != null) ri.SetColor(new Color(0.2f, 0.6f, 0.9f, 1f));
            AddCtrl("随机变色", () => { if (ri != null) ri.SetColor(UnityEngine.Random.ColorHSV()); Log("[UIRawImage] SetColor"); });
            Log("[UIRawImage] 已加载（无贴图时用 SetColor 着色；有贴图用 SetTexture）");
        }

        private void BuildUITooltip()
        {
            var go = LoadToStage("Prefabs/UI/Test/Test_UITooltip");
            if (go == null) return;
            var tt = go.GetComponent<UITooltip>();
            if (tt == null) { Log("[UITooltip] 未取到组件"); return; }
            tt.Hide();
            AddCtrl("显示提示", () => { if (tt != null) tt.Show("这是一段提示文字 ✓"); Log("[UITooltip] Show"); });
            AddCtrl("隐藏", () => { if (tt != null) tt.Hide(); Log("[UITooltip] Hide"); });
            Log("[UITooltip] 已加载（初始隐藏），点按钮测试 Show/Hide");
        }

        // ═══════════════════════════════════════════
        //  通用视图（静态门面，有过程化 fallback）
        // ═══════════════════════════════════════════

        private void BuildUIToast()
        {
            ClearStage();
            AddCtrl("显示 Toast", () => { UIToast.Show("测试 Toast ✓", 3f); Log("[UIToast] Show（Max 层，3s 自消）"); });
            Log("[UIToast] 静态门面，点按钮显示（有过程化 fallback，无需预制体）");
        }

        private void BuildUILoading()
        {
            ClearStage();
            AddCtrl("显示 Loading", () => { UILoading.Show("加载中...请稍候"); Log("[UILoading] Show（全屏）"); });
            AddCtrl("隐藏 Loading", () => { UILoading.Hide(); Log("[UILoading] Hide"); });
            Log("[UILoading] 静态门面，点按钮显示/隐藏（有过程化 fallback）");
        }

        private void BuildUIAnimation()
        {
            ClearStage();
            // 在 Stage 内放一个演示方块
            var target = NewGO("AnimTarget", _stage, typeof(RectTransform), typeof(Image));
            var trt = (RectTransform)target.transform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f); trt.pivot = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = new Vector2(120, 120);
            target.GetComponent<Image>().color = new Color(0.07f, 0.66f, 0.90f, 1f);
            _animTarget = trt;
            AddCtrl("PopIn", () => { if (_animTarget != null) UIAnimation.PopIn(_animTarget, null, 0.35f); Log("[UIAnimation] PopIn"); });
            AddCtrl("Punch", () => { if (_animTarget != null) UIAnimation.Punch(_animTarget, 0.85f, 0.3f); Log("[UIAnimation] Punch"); });
            AddCtrl("Shake", () => { if (_animTarget != null) UIAnimation.Shake(_animTarget, 0.4f, 12f, 8); Log("[UIAnimation] Shake"); });
            AddCtrl("Breath(循环)", () => { if (_animTarget != null) { UIAnimation.Kill(_animTarget); UIAnimation.Breath(_animTarget); } Log("[UIAnimation] Breath"); });
            AddCtrl("Kill", () => { if (_animTarget != null) UIAnimation.Kill(_animTarget); Log("[UIAnimation] Kill"); });
            Log("[UIAnimation] DOTween 预设，点按钮对右侧方块播放（需 PlayMode 才有帧更新）");
        }

        // ═══════════════════════════════════════════
        //  窗口族（经 UUIManager 打开到 Window 层）
        // ═══════════════════════════════════════════

        private void BuildUIWindow()
        {
            ClearStage();
            UUIManager.Instance.OpenViewSync<TestUIWindowView>();
            Log("[UIWindow] 已请求 UUIManager 打开（见 Window 层，点 X 带动画关闭）");
        }

        private void BuildUIPopup()
        {
            ClearStage();
            UUIManager.Instance.OpenViewSync<TestUIPopupView>();
            Log("[UIPopup] 已请求 UUIManager 打开（点遮罩或 X 关闭，模态拦截下层）");
        }

        private void BuildUIDialog()
        {
            ClearStage();
            var d = UUIManager.Instance.OpenViewSync<TestUIDialogView>();
            if (d != null)
                d.Set("测试标题", "这是 UIDialog 正文。确认/取消都会关闭并打印回调。", () => Log("[UIDialog] 确认回调 ✓"), () => Log("[UIDialog] 取消回调 ✓"));
            Log("[UIDialog] 已请求 UUIManager 打开并 Set 内容/回调");
        }

        // ═══════════════════════════════════════════
        //  Helpers
        // ═══════════════════════════════════════════

        private void Log(string s)
        {
            Debug.Log("[ComponentTest] " + s);
            _logLines.Add(s);
            while (_logLines.Count > 8) _logLines.RemoveAt(0);
            if (_logTmp != null) _logTmp.text = string.Join("\n", _logLines);
        }

        // 控制条按钮（用原生 Button，不走 UIButton 生命周期，避免重复 OnInit）
        private void AddCtrl(string label, Action cb)
        {
            var go = NewGO("Ctrl_" + label, _ctrlBar, typeof(RectTransform), typeof(Image), typeof(Button));
            go.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.36f, 1f);
            var le = go.AddComponent<LayoutElement>(); le.preferredWidth = 120; le.preferredHeight = 30;
            go.GetComponent<Button>().onClick.AddListener(() => cb());
            var lbl = NewGO("L", go.transform, typeof(RectTransform), typeof(TextMeshProUGUI));
            Stretch((RectTransform)lbl.transform);
            var t = lbl.GetComponent<TextMeshProUGUI>();
            t.text = label; t.fontSize = 13; t.alignment = TextAlignmentOptions.Center; t.color = Color.white; t.raycastTarget = false;
        }

        // 单选组的单个 Toggle（Image+Toggle+UIToggle + Checkmark）
        private UIToggle MakeRadioToggle(Transform parent, string name)
        {
            var go = NewGO(name, parent, typeof(RectTransform), typeof(Image), typeof(Toggle), typeof(UIToggle));
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(28, 28);
            var bg = go.GetComponent<Image>(); bg.color = Color.white;
            var tg = go.GetComponent<Toggle>(); tg.targetGraphic = bg;
            var ck = NewGO("Checkmark", go.transform, typeof(RectTransform), typeof(Image));
            Stretch((RectTransform)ck.transform);
            var ckImg = ck.GetComponent<Image>(); ckImg.color = Color.black; ckImg.raycastTarget = false;
            tg.graphic = ckImg;
            return go.GetComponent<UIToggle>();
        }

        private static GameObject NewGO(string name, Transform parent, params Type[] components)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            foreach (var t in components) go.AddComponent(t);
            return go;
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
                                    Vector2 offsetMin, Vector2 offsetMax, Vector2? pivot = null)
        {
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            if (pivot.HasValue) rt.pivot = pivot.Value;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
