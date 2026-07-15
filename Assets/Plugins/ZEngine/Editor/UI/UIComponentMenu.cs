//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using ZEngine.Manager.UI.UGUI;
using ZEngine.Manager.UI.UGUI.Components;

namespace ZEngine.Editor.UI
{
    /// <summary>
    /// UGUI 组件工厂：提供 public static 工厂方法创建完整子层级的 ZEngine 包装组件，
    /// 同时保留 GameObject/UI/ZEngine/<Component> 右键菜单。
    ///
    /// 工厂方法签名：GameObject CreateXXX(RectTransform parent, string name, Vector2 pos, ...)
    ///   返回的 GameObject 已挂好所有底层 UGUI + 包装组件 + 完整子层级 + 接线，直接 parent 下置放即可。
    ///   可被 execute_code / AI 生成 Prefab 流 直接调用，避免每次手动构建子节点。
    ///
    /// 右键菜单方法签名：static void CreateXXX(MenuCommand mc) —— 一行委托给 CreateXXXInternal。
    /// </summary>
    public static class UIComponentMenu
    {
        private const int PRIORITY = 2151;
        private const int DEFAULT_FONT = 24;

        #region ── 公共工厂（供 AI / execute_code 调用）──

        public static GameObject CreateUIButton(RectTransform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = NewRoot(name, size);
            var img = AddImage(go, new Color(1, 1, 1, 0.85f));
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            go.AddComponent<UIButton>();
            NewTMP(go.transform, "Label", "Button", DEFAULT_FONT, TextAlignmentOptions.Center, Color.black);
            return Attach(go, parent, pos);
        }

        public static GameObject CreateUIText(RectTransform parent, string name, Vector2 pos, Vector2 size, string text = "Text")
        {
            var go = NewRoot(name, size);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = DEFAULT_FONT;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            go.AddComponent<UIText>();
            return Attach(go, parent, pos);
        }

        public static GameObject CreateUIImage(RectTransform parent, string name, Vector2 pos, Vector2 size, Color? color = null)
        {
            var go = NewRoot(name, size);
            AddImage(go, color ?? new Color(1, 1, 1, 1));
            go.AddComponent<UIImage>();
            return Attach(go, parent, pos);
        }

        public static GameObject CreateUIInput(RectTransform parent, string name, Vector2 pos, Vector2 size,
            string placeholderText = "Enter text...")
        {
            var go = NewRoot(name, size);
            AddImage(go, new Color(1, 1, 1, 0.9f));
            var textArea = NewChild(go.transform, "Text Area");
            SetStretch(textArea.GetComponent<RectTransform>(), 10, 6, 10, 6);
            NewTMP(textArea.transform, "Placeholder", placeholderText, DEFAULT_FONT, TextAlignmentOptions.Left, new Color(0.5f, 0.5f, 0.5f, 1f));
            var text = NewTMP(textArea.transform, "Text", "", DEFAULT_FONT, TextAlignmentOptions.Left, Color.black);
            var input = go.AddComponent<TMP_InputField>();
            input.textViewport = textArea.GetComponent<RectTransform>();
            input.textComponent = text;
            input.placeholder = textArea.transform.Find("Placeholder")?.GetComponent<TextMeshProUGUI>();
            go.AddComponent<UIInput>();
            return Attach(go, parent, pos);
        }

        public static GameObject CreateUIToggle(RectTransform parent, string name, Vector2 pos, Vector2 size, bool isOn = true)
        {
            var go = NewRoot(name, size);
            var bg = AddImage(go, new Color(1, 1, 1, 0.85f));
            var check = NewChild(go.transform, "Checkmark");
            var checkImg = AddImage(check, new Color(0, 0, 0, 1));
            checkImg.raycastTarget = false;
            Stretch(checkImg.GetComponent<RectTransform>());
            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = bg;
            toggle.graphic = checkImg;
            toggle.isOn = isOn;
            go.AddComponent<UIToggle>();
            return Attach(go, parent, pos);
        }

        public static GameObject CreateUISlider(RectTransform parent, string name, Vector2 pos, Vector2 size, float value = 1f)
        {
            var go = NewRoot(name, size);
            var bg = NewChild(go.transform, "Background");
            AddImage(bg, new Color(0.8f, 0.8f, 0.8f, 1f));
            SetStretch(bg.GetComponent<RectTransform>(), 0, 0, 0, 0);
            var fillArea = NewChild(go.transform, "Fill Area");
            SetStretch(fillArea.GetComponent<RectTransform>(), 10, 0, 10, 0);
            var fill = NewChild(fillArea.transform, "Fill");
            var fillImg = AddImage(fill, new Color(0.3f, 0.6f, 1f, 1f));
            SetStretch(fillImg.GetComponent<RectTransform>(), 0, 0, 0, 0);
            var handleArea = NewChild(go.transform, "Handle Area");
            SetStretch(handleArea.GetComponent<RectTransform>(), 10, 0, 10, 0);
            var handle = NewChild(handleArea.transform, "Handle");
            var handleImg = AddImage(handle, new Color(1, 1, 1, 1));
            var handleRt = handleImg.GetComponent<RectTransform>();
            handleRt.anchorMin = handleRt.anchorMax = new Vector2(0.5f, 0.5f);
            handleRt.sizeDelta = new Vector2(20, 20);
            var slider = go.AddComponent<Slider>();
            slider.targetGraphic = handleImg;
            slider.fillRect = fillImg.GetComponent<RectTransform>();
            slider.handleRect = handleRt;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f; slider.maxValue = 1f; slider.value = value;
            go.AddComponent<UISlider>();
            return Attach(go, parent, pos);
        }

        public static GameObject CreateUIDropdown(RectTransform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = NewRoot(name, size);
            AddImage(go, new Color(1, 1, 1, 0.9f));
            var label = NewTMP(go.transform, "Label", "", DEFAULT_FONT, TextAlignmentOptions.Left, Color.black);
            var labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0, 0); labelRt.anchorMax = new Vector2(0.8f, 1);
            labelRt.offsetMin = new Vector2(10, 0); labelRt.offsetMax = Vector2.zero;
            var arrow = NewTMP(go.transform, "Arrow", "V", DEFAULT_FONT, TextAlignmentOptions.Center, Color.black);
            var arrowRt = arrow.GetComponent<RectTransform>();
            arrowRt.anchorMin = new Vector2(0.8f, 0); arrowRt.anchorMax = new Vector2(1, 1);
            arrowRt.offsetMin = Vector2.zero; arrowRt.offsetMax = Vector2.zero;
            var template = NewChild(go.transform, "Template");
            AddImage(template, new Color(1, 1, 1, 1));
            var templateRt = template.GetComponent<RectTransform>();
            templateRt.anchorMin = new Vector2(0, 0); templateRt.anchorMax = new Vector2(1, 0);
            templateRt.pivot = new Vector2(0.5f, 1f); templateRt.sizeDelta = new Vector2(0, 150);
            template.SetActive(false);
            var viewport = NewChild(template.transform, "Viewport");
            AddImage(viewport, new Color(1, 1, 1, 0));
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            SetStretch(viewport.GetComponent<RectTransform>(), 0, 0, 0, 0);
            var content = NewChild(viewport.transform, "Content");
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = Vector2.one;
            contentRt.pivot = new Vector2(0.5f, 1f); contentRt.sizeDelta = new Vector2(0, 28);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            // Item 模板
            var item = NewChild(content.transform, "Item");
            AddImage(item, new Color(0.9f, 0.9f, 0.9f, 1f));
            var itemToggle = item.AddComponent<Toggle>();
            itemToggle.targetGraphic = item.GetComponent<Image>();
            var itemRt = item.GetComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0, 0.5f); itemRt.anchorMax = new Vector2(1, 0.5f);
            itemRt.pivot = new Vector2(0.5f, 0.5f); itemRt.sizeDelta = new Vector2(0, 28);
            var check = NewChild(item.transform, "Checkmark");
            var checkImg = AddImage(check, new Color(0, 0, 0, 1)); checkImg.raycastTarget = false;
            var checkRt = check.GetComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0, 0.5f); checkRt.anchorMax = new Vector2(0, 0.5f);
            checkRt.sizeDelta = new Vector2(20, 20); checkRt.anchoredPosition = new Vector2(10, 0);
            itemToggle.graphic = checkImg; itemToggle.isOn = false;
            var itemLabel = NewTMP(item.transform, "Item Label", "", DEFAULT_FONT, TextAlignmentOptions.Left, Color.black);
            var ilRt = itemLabel.GetComponent<RectTransform>();
            ilRt.anchorMin = new Vector2(0, 0); ilRt.anchorMax = new Vector2(1, 1);
            ilRt.offsetMin = new Vector2(28, 0); ilRt.offsetMax = new Vector2(-5, 0);
            var dd = go.AddComponent<TMP_Dropdown>();
            dd.captionText = label;
            dd.template = template.GetComponent<RectTransform>();
            dd.itemText = itemLabel;
            dd.captionText.text = "Option A";
            dd.AddOptions(new System.Collections.Generic.List<string> { "Option A", "Option B", "Option C" });
            go.AddComponent<UIDropdown>();
            return Attach(go, parent, pos);
        }

        public static GameObject CreateUIGrid(RectTransform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = NewRoot(name, size);
            var layout = go.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(100, 100);
            layout.spacing = new Vector2(4, 4);
            go.AddComponent<UIGrid>();
            return Attach(go, parent, pos);
        }

        public static GameObject CreateUITree(RectTransform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = NewRoot(name, size);
            go.AddComponent<VerticalLayoutGroup>();
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            go.AddComponent<UITree>();
            // RowTemplate
            var row = NewChild(go.transform, "RowTemplate");
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0, 1); rowRt.anchorMax = Vector2.one;
            rowRt.pivot = new Vector2(0.5f, 1f); rowRt.sizeDelta = new Vector2(0, 40);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true; hlg.childForceExpandWidth = false; hlg.childAlignment = TextAnchor.MiddleLeft;
            row.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.MinSize;
            var indent = NewChild(row.transform, "Indent");
            indent.AddComponent<LayoutElement>().preferredWidth = 0;
            var expand = NewChild(row.transform, "Expand");
            var expandImg = AddImage(expand, new Color(0.6f, 0.6f, 0.6f, 1f));
            expandImg.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 40);
            expand.AddComponent<Button>().targetGraphic = expandImg;
            expand.AddComponent<UIButton>();
            NewTMP(expand.transform, "Icon", ">", DEFAULT_FONT, TextAlignmentOptions.Center, Color.white);
            var rowLabel = NewTMP(row.transform, "Label", "Node", DEFAULT_FONT, TextAlignmentOptions.Left, Color.white);
            var rlRt = rowLabel.GetComponent<RectTransform>();
            rlRt.anchorMin = new Vector2(0, 0); rlRt.anchorMax = Vector2.one;
            rlRt.offsetMin = new Vector2(8, 0); rlRt.offsetMax = Vector2.zero;
            rowLabel.gameObject.AddComponent<UIText>();
            row.SetActive(false);
            return Attach(go, parent, pos);
        }

        public static GameObject CreateUITabScaffold(RectTransform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = NewRoot(name, size);
            AddImage(go, new Color(0.18f, 0.18f, 0.2f, 1f));
            go.AddComponent<UITab>();
            var strip = NewChild(go.transform, "TabStrip");
            var stripRt = strip.GetComponent<RectTransform>();
            stripRt.anchorMin = new Vector2(0, 1); stripRt.anchorMax = Vector2.one;
            stripRt.pivot = new Vector2(0.5f, 1f); stripRt.sizeDelta = new Vector2(0, 56);
            var slg = strip.AddComponent<HorizontalLayoutGroup>();
            slg.childControlWidth = true; slg.childForceExpandWidth = true;
            slg.childControlHeight = true; slg.childForceExpandHeight = true;
            slg.spacing = 2; slg.padding = new RectOffset(2, 2, 2, 2);
            AddTabToggle(strip.transform, "Tab0", "Tab0");
            AddTabToggle(strip.transform, "Tab1", "Tab1");
            var pages = NewChild(go.transform, "Pages");
            var pagesRt = pages.GetComponent<RectTransform>();
            pagesRt.anchorMin = Vector2.zero; pagesRt.anchorMax = Vector2.one;
            pagesRt.offsetMin = new Vector2(0, 0); pagesRt.offsetMax = new Vector2(0, -56);
            AddPage(pages.transform, "Page0", "Page 0", new Color(0.25f, 0.25f, 0.3f, 1f));
            AddPage(pages.transform, "Page1", "Page 1", new Color(0.2f, 0.3f, 0.25f, 1f));
            return Attach(go, parent, pos);
        }

        public static GameObject CreateUIListView(RectTransform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = NewRoot(name, size);
            AddImage(go, new Color(0.15f, 0.15f, 0.15f, 1f));
            var scroll = go.AddComponent<LoopVerticalScrollRect>();
            scroll.vertical = true; scroll.horizontal = false;
            var viewport = NewChild(go.transform, "Viewport");
            AddImage(viewport, new Color(1, 1, 1, 0));
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            SetStretch(viewport.GetComponent<RectTransform>(), 0, 0, 0, 0);
            var content = NewChild(viewport.transform, "Content");
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = Vector2.one;
            contentRt.pivot = new Vector2(0.5f, 1f); contentRt.sizeDelta = Vector2.zero;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRt;
            go.AddComponent<UIListView>();
            return Attach(go, parent, pos);
        }

        public static GameObject CreateUIWindow(RectTransform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = NewRoot(name, size);
            AddImage(go, new Color(0.2f, 0.2f, 0.25f, 0.95f));
            var cb = NewChild(go.transform, "CloseBtn");
            var cbRt = cb.GetComponent<RectTransform>();
            cbRt.anchorMin = cbRt.anchorMax = new Vector2(1, 1);
            cbRt.pivot = new Vector2(1, 1); cbRt.sizeDelta = new Vector2(40, 40);
            var cbImg = AddImage(cb, new Color(0.8f, 0.2f, 0.2f, 1f));
            cb.AddComponent<Button>().targetGraphic = cbImg;
            cb.AddComponent<UIButton>();
            NewTMP(cb.transform, "X", "X", DEFAULT_FONT, TextAlignmentOptions.Center, Color.white);
            go.AddComponent<UIWindow>();
            return Attach(go, parent, pos);
        }

        public static GameObject CreateUIPopup(RectTransform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = NewRoot(name, size);
            var mask = NewChild(go.transform, "Mask");
            var maskImg = AddImage(mask, new Color(0, 0, 0, 0.6f));
            mask.AddComponent<Button>().targetGraphic = maskImg;
            mask.AddComponent<UIImage>();
            mask.AddComponent<UIButton>();
            SetStretch(mask.GetComponent<RectTransform>(), 0, 0, 0, 0);
            var body = NewChild(go.transform, "Body");
            var bodyRt = body.GetComponent<RectTransform>();
            bodyRt.anchorMin = bodyRt.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRt.sizeDelta = new Vector2(400, 260);
            AddImage(body, new Color(0.2f, 0.2f, 0.25f, 0.95f));
            // CloseBtn 放根级（非 Body 下），路径 "CloseBtn" 匹配 UIWindow 的 [UIBind("CloseBtn")]
            var cb = NewChild(go.transform, "CloseBtn");
            var cbRt2 = cb.GetComponent<RectTransform>();
            cbRt2.anchorMin = cbRt2.anchorMax = new Vector2(1, 1);
            cbRt2.pivot = new Vector2(1, 1); cbRt2.sizeDelta = new Vector2(40, 40);
            var cbImg2 = AddImage(cb, new Color(0.8f, 0.2f, 0.2f, 1f));
            cb.AddComponent<Button>().targetGraphic = cbImg2;
            cb.AddComponent<UIButton>();
            go.AddComponent<UIPopup>();
            return Attach(go, parent, pos);
        }

        public static GameObject CreateUIDialog(RectTransform parent, string name, Vector2 pos, Vector2 size)
        {
            // 基于 UIPopup 结构（Mask + Body + 根级 CloseBtn）+ Dialog 专属内容节点
            var go = NewRoot(name, size);
            // Mask（点击关闭，全屏遮罩）
            var mask = NewChild(go.transform, "Mask");
            var maskImg = AddImage(mask, new Color(0, 0, 0, 0.6f));
            mask.AddComponent<Button>().targetGraphic = maskImg;
            mask.AddComponent<UIImage>();
            mask.AddComponent<UIButton>();
            SetStretch(mask.GetComponent<RectTransform>(), 0, 0, 0, 0);
            // Body（弹窗主体）
            var body = NewChild(go.transform, "Body");
            var bodyRt = body.GetComponent<RectTransform>();
            bodyRt.anchorMin = bodyRt.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRt.sizeDelta = new Vector2(400, 220);
            AddImage(body, new Color(0.2f, 0.2f, 0.25f, 0.95f));
            // 根级 CloseBtn（匹配 UIWindow [UIBind("CloseBtn")]）
            var cb = NewChild(go.transform, "CloseBtn");
            var cbRt = cb.GetComponent<RectTransform>();
            cbRt.anchorMin = cbRt.anchorMax = new Vector2(1, 1);
            cbRt.pivot = new Vector2(1, 1); cbRt.sizeDelta = new Vector2(36, 36);
            var cbImg = AddImage(cb, new Color(0.8f, 0.2f, 0.2f, 1f));
            cb.AddComponent<Button>().targetGraphic = cbImg;
            cb.AddComponent<UIButton>();
            NewTMP(cb.transform, "X", "X", DEFAULT_FONT, TextAlignmentOptions.Center, Color.white);
            // Txt_Title（Body 顶部）
            var title = NewTMP(body.transform, "Txt_Title", "Title", DEFAULT_FONT, TextAlignmentOptions.Center, Color.white);
            var tRt = title.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1); tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1f); tRt.sizeDelta = new Vector2(0, 32);
            tRt.anchoredPosition = new Vector2(0, -10);
            title.gameObject.AddComponent<UIText>();
            // Txt_Message（Body 中部）
            var msg = NewTMP(body.transform, "Txt_Message", "Message", DEFAULT_FONT, TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.85f, 1f));
            var mRt = msg.GetComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0, 0.3f); mRt.anchorMax = new Vector2(1, 0.85f);
            mRt.offsetMin = new Vector2(12, 0); mRt.offsetMax = new Vector2(-12, 0);
            msg.gameObject.AddComponent<UIText>();
            // Btn_Cancel / Btn_Confirm（Body 底部）
            var footer = NewChild(body.transform, "Footer");
            var fRt = footer.GetComponent<RectTransform>();
            fRt.anchorMin = new Vector2(0, 0); fRt.anchorMax = new Vector2(1, 0);
            fRt.pivot = new Vector2(0.5f, 0f); fRt.sizeDelta = new Vector2(0, 44);
            fRt.anchoredPosition = new Vector2(0, 10);
            var fHlg = footer.AddComponent<HorizontalLayoutGroup>();
            fHlg.childControlWidth = true; fHlg.childForceExpandWidth = true;
            fHlg.childControlHeight = true; fHlg.childForceExpandHeight = true;
            fHlg.spacing = 12; fHlg.padding = new RectOffset(12, 12, 4, 4);
            fHlg.childAlignment = TextAnchor.MiddleCenter;
            var cancel = NewChild(footer.transform, "Btn_Cancel");
            AddImage(cancel, new Color(0.6f, 0.6f, 0.6f, 1f));
            cancel.AddComponent<Button>().targetGraphic = cancel.GetComponent<Image>();
            cancel.AddComponent<UIButton>();
            NewTMP(cancel.transform, "Label", "Cancel", DEFAULT_FONT, TextAlignmentOptions.Center, Color.white);
            var confirm = NewChild(footer.transform, "Btn_Confirm");
            AddImage(confirm, new Color(0, 0.72f, 0.47f, 1f));
            confirm.AddComponent<Button>().targetGraphic = confirm.GetComponent<Image>();
            confirm.AddComponent<UIButton>();
            NewTMP(confirm.transform, "Label", "Confirm", DEFAULT_FONT, TextAlignmentOptions.Center, Color.white);
            go.AddComponent<UIDialog>();
            return Attach(go, parent, pos);
        }

        public static GameObject CreateUIToast(RectTransform parent, string name, Vector2 pos, Vector2 size, string text = "Toast")
        {
            var go = NewRoot(name, size);
            AddImage(go, new Color(0, 0, 0, 0.75f));
            var t = NewTMP(go.transform, "Text", text, 38, TextAlignmentOptions.Center, Color.white);
            t.gameObject.AddComponent<UIText>();
            go.AddComponent<UIToastView>();
            return Attach(go, parent, pos);
        }

        public static GameObject CreateUILoading(RectTransform parent, string name, Vector2 pos, Vector2 size, string tip = "Loading...")
        {
            var go = NewRoot(name, size);
            SetStretch(go.GetComponent<RectTransform>(), 0, 0, 0, 0);
            AddImage(go, new Color(0, 0, 0, 0.6f));
            var t = NewTMP(go.transform, "Tip", tip, 40, TextAlignmentOptions.Center, Color.white);
            var tipRt = t.GetComponent<RectTransform>();
            tipRt.anchorMin = tipRt.anchorMax = new Vector2(0.5f, 0.5f);
            tipRt.sizeDelta = new Vector2(600, 80);
            t.gameObject.AddComponent<UIText>();
            go.AddComponent<UILoading>();
            return Attach(go, parent, pos);
        }

        #endregion

        #region ── 拓展组件工厂（Extensions/）──

        public static GameObject CreateUIProgressBar(RectTransform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = NewRoot(name, size);
            var bg = NewChild(go.transform, "Background");
            AddImage(bg, new Color(0.2f, 0.2f, 0.2f, 1f));
            SetStretch(bg.GetComponent<RectTransform>(), 0, 0, 0, 0);
            var fill = NewChild(go.transform, "Fill");
            var fillImg = AddImage(fill, new Color(0.07f, 0.66f, 0.90f, 1f));
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.fillAmount = 1f;
            SetStretch(fillImg.GetComponent<RectTransform>(), 0, 0, 0, 0);
            var txt = NewTMP(go.transform, "Text", "100%", DEFAULT_FONT, TextAlignmentOptions.Center, Color.white);
            var txtRt = txt.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;
            go.AddComponent<UIProgressBar>();
            return Attach(go, parent, pos);
        }

        public static GameObject CreateUIRadioGroup(RectTransform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = NewRoot(name, size);
            go.AddComponent<UIRadioGroup>();
            return Attach(go, parent, pos);
        }

        public static GameObject CreateUITooltip(RectTransform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = NewRoot(name, size);
            var bg = NewChild(go.transform, "Background");
            AddImage(bg, new Color(0, 0, 0, 0.85f));
            SetStretch(bg.GetComponent<RectTransform>(), 0, 0, 0, 0);
            var lbl = NewTMP(go.transform, "Label", "Tooltip", DEFAULT_FONT, TextAlignmentOptions.Center, Color.white);
            var lRt = lbl.GetComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
            lRt.offsetMin = new Vector2(8, 4); lRt.offsetMax = new Vector2(-8, -4);
            go.AddComponent<UITooltip>();
            // 创建即可见，便于编辑器调整；运行时由调用方按需 Hide()
            return Attach(go, parent, pos);
        }

        public static GameObject CreateUIScrollbar(RectTransform parent, string name, Vector2 pos, Vector2 size, Scrollbar.Direction dir = Scrollbar.Direction.LeftToRight)
        {
            var go = NewRoot(name, size);
            // 根节点=轨道背景（之前缺失，导致只有孤立 Handle 可见）
            AddImage(go, new Color(0.78f, 0.78f, 0.80f, 1f));
            // Sliding Area：留 4px 内边，让 Handle 不贴边
            var sa = NewChild(go.transform, "Sliding Area");
            SetStretch(sa.GetComponent<RectTransform>(), 4, 4, 4, 4);
            // Handle：拉伸填充（Scrollbar 组件运行时按 value/size 控制其位置与尺寸）
            var handle = NewChild(sa.transform, "Handle");
            var handleImg = AddImage(handle, new Color(0.07f, 0.66f, 0.90f, 1f));
            var hRt = handleImg.GetComponent<RectTransform>();
            hRt.anchorMin = Vector2.zero; hRt.anchorMax = Vector2.one;
            hRt.offsetMin = hRt.offsetMax = Vector2.zero;
            var sb = go.AddComponent<Scrollbar>();
            sb.direction = dir;
            sb.handleRect = hRt;
            sb.targetGraphic = handleImg;
            sb.size = 0.2f;          // Handle 占轨道 20%，更像滑动条手柄
            sb.value = 1f;
            go.AddComponent<UIScrollbar>();
            return Attach(go, parent, pos);
        }

        public static GameObject CreateUIRawImage(RectTransform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = NewRoot(name, size);
            go.AddComponent<RawImage>();
            go.AddComponent<UIRawImage>();
            return Attach(go, parent, pos);
        }

        public static GameObject CreateUICountdown(RectTransform parent, string name, Vector2 pos, Vector2 size, float seconds = 60f)
        {
            var go = NewRoot(name, size);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "01:00"; tmp.fontSize = DEFAULT_FONT;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
            go.AddComponent<UICountdown>().SetCountdown(seconds);
            return Attach(go, parent, pos);
        }

        public static GameObject CreateUIDraggable(RectTransform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = NewRoot(name, size);
            AddImage(go, new Color(1, 1, 1, 0.8f));
            go.AddComponent<UIDraggable>();
            return Attach(go, parent, pos);
        }

        public static GameObject CreateUIPagination(RectTransform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = NewRoot(name, size);
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true; hlg.childForceExpandWidth = false;
            hlg.childControlHeight = true; hlg.childForceExpandHeight = true;
            hlg.spacing = 8; hlg.childAlignment = TextAnchor.MiddleCenter;
            var prev = NewChild(go.transform, "Btn_Prev");
            AddImage(prev, new Color(0.9f, 0.9f, 0.9f, 1f));
            prev.AddComponent<Button>().targetGraphic = prev.GetComponent<Image>();
            prev.AddComponent<UIButton>();
            NewTMP(prev.transform, "Text", "<", DEFAULT_FONT, TextAlignmentOptions.Center, Color.black);
            prev.AddComponent<LayoutElement>().preferredWidth = 36;
            prev.AddComponent<LayoutElement>().preferredHeight = 36;
            var page = NewTMP(go.transform, "Txt_Page", "1/5", DEFAULT_FONT, TextAlignmentOptions.Center, Color.black);
            page.gameObject.AddComponent<LayoutElement>().preferredWidth = 60;
            var next = NewChild(go.transform, "Btn_Next");
            AddImage(next, new Color(0.9f, 0.9f, 0.9f, 1f));
            next.AddComponent<Button>().targetGraphic = next.GetComponent<Image>();
            next.AddComponent<UIButton>();
            NewTMP(next.transform, "Text", ">", DEFAULT_FONT, TextAlignmentOptions.Center, Color.black);
            next.AddComponent<LayoutElement>().preferredWidth = 36;
            next.AddComponent<LayoutElement>().preferredHeight = 36;
            go.AddComponent<UIPagination>();
            return Attach(go, parent, pos);
        }

        #endregion

        #region ── 右键菜单（MenuCommand + Undo + Select）──

        [MenuItem("GameObject/UI/ZEngine/UIButton", false, PRIORITY)]
        static void DoUIButton(MenuCommand mc) => Place(CreateUIButton(null, "UIButton", Vector2.zero, new Vector2(160, 48)), mc);
        [MenuItem("GameObject/UI/ZEngine/UIText", false, PRIORITY)]
        static void DoUIText(MenuCommand mc) => Place(CreateUIText(null, "UIText", Vector2.zero, new Vector2(200, 60)), mc);
        [MenuItem("GameObject/UI/ZEngine/UIImage", false, PRIORITY)]
        static void DoUIImage(MenuCommand mc) => Place(CreateUIImage(null, "UIImage", Vector2.zero, new Vector2(100, 100)), mc);
        [MenuItem("GameObject/UI/ZEngine/UIInput", false, PRIORITY)]
        static void DoUIInput(MenuCommand mc) => Place(CreateUIInput(null, "UIInput", Vector2.zero, new Vector2(240, 40)), mc);
        [MenuItem("GameObject/UI/ZEngine/UIToggle", false, PRIORITY)]
        static void DoUIToggle(MenuCommand mc) => Place(CreateUIToggle(null, "UIToggle", Vector2.zero, new Vector2(24, 24)), mc);
        [MenuItem("GameObject/UI/ZEngine/UISlider", false, PRIORITY)]
        static void DoUISlider(MenuCommand mc) => Place(CreateUISlider(null, "UISlider", Vector2.zero, new Vector2(160, 20)), mc);
        [MenuItem("GameObject/UI/ZEngine/UIDropdown", false, PRIORITY)]
        static void DoUIDropdown(MenuCommand mc) => Place(CreateUIDropdown(null, "UIDropdown", Vector2.zero, new Vector2(160, 32)), mc);
        [MenuItem("GameObject/UI/ZEngine/UIGrid", false, PRIORITY)]
        static void DoUIGrid(MenuCommand mc) => Place(CreateUIGrid(null, "UIGrid", Vector2.zero, new Vector2(400, 400)), mc);
        [MenuItem("GameObject/UI/ZEngine/UITree", false, PRIORITY)]
        static void DoUITree(MenuCommand mc) => Place(CreateUITree(null, "UITree", Vector2.zero, new Vector2(300, 400)), mc);
        [MenuItem("GameObject/UI/ZEngine/UITab", false, PRIORITY)]
        static void DoUITab(MenuCommand mc) => Place(CreateUITabScaffold(null, "UITab", Vector2.zero, new Vector2(800, 600)), mc);
        [MenuItem("GameObject/UI/ZEngine/UIListView", false, PRIORITY)]
        static void DoUIListView(MenuCommand mc) => Place(CreateUIListView(null, "UIListView", Vector2.zero, new Vector2(400, 600)), mc);
        [MenuItem("GameObject/UI/ZEngine/UIWindow", false, PRIORITY)]
        static void DoUIWindow(MenuCommand mc) => Place(CreateUIWindow(null, "UIWindow", Vector2.zero, new Vector2(600, 400)), mc);
        [MenuItem("GameObject/UI/ZEngine/UIPopup", false, PRIORITY)]
        static void DoUIPopup(MenuCommand mc) => Place(CreateUIPopup(null, "UIPopup", Vector2.zero, new Vector2(600, 400)), mc);
        [MenuItem("GameObject/UI/ZEngine/UIToast", false, PRIORITY)]
        static void DoUIToast(MenuCommand mc) => Place(CreateUIToast(null, "UIToast", Vector2.zero, new Vector2(640, 140)), mc);
        [MenuItem("GameObject/UI/ZEngine/UILoading", false, PRIORITY)]
        static void DoUILoading(MenuCommand mc) => Place(CreateUILoading(null, "UILoading", Vector2.zero, Vector2.zero), mc);
        [MenuItem("GameObject/UI/ZEngine/Extensions/UIProgressBar", false, PRIORITY)]
        static void DoUIProgressBar(MenuCommand mc) => Place(CreateUIProgressBar(null, "UIProgressBar", Vector2.zero, new Vector2(200, 24)), mc);
        [MenuItem("GameObject/UI/ZEngine/Extensions/UIRadioGroup", false, PRIORITY)]
        static void DoUIRadioGroup(MenuCommand mc) => Place(CreateUIRadioGroup(null, "UIRadioGroup", Vector2.zero, new Vector2(200, 100)), mc);
        [MenuItem("GameObject/UI/ZEngine/Extensions/UITooltip", false, PRIORITY)]
        static void DoUITooltip(MenuCommand mc) => Place(CreateUITooltip(null, "UITooltip", Vector2.zero, new Vector2(160, 40)), mc);
        [MenuItem("GameObject/UI/ZEngine/Extensions/UIScrollbar", false, PRIORITY)]
        static void DoUIScrollbar(MenuCommand mc) => Place(CreateUIScrollbar(null, "UIScrollbar", Vector2.zero, new Vector2(160, 20)), mc);
        [MenuItem("GameObject/UI/ZEngine/Extensions/UIRawImage", false, PRIORITY)]
        static void DoUIRawImage(MenuCommand mc) => Place(CreateUIRawImage(null, "UIRawImage", Vector2.zero, new Vector2(100, 100)), mc);
        [MenuItem("GameObject/UI/ZEngine/Extensions/UICountdown", false, PRIORITY)]
        static void DoUICountdown(MenuCommand mc) => Place(CreateUICountdown(null, "UICountdown", Vector2.zero, new Vector2(120, 36)), mc);
        [MenuItem("GameObject/UI/ZEngine/Extensions/UIDraggable", false, PRIORITY)]
        static void DoUIDraggable(MenuCommand mc) => Place(CreateUIDraggable(null, "UIDraggable", Vector2.zero, new Vector2(80, 80)), mc);
        [MenuItem("GameObject/UI/ZEngine/Extensions/UIPagination", false, PRIORITY)]
        static void DoUIPagination(MenuCommand mc) => Place(CreateUIPagination(null, "UIPagination", Vector2.zero, new Vector2(200, 36)), mc);
        [MenuItem("GameObject/UI/ZEngine/Extensions/UIDialog", false, PRIORITY)]
        static void DoUIDialog(MenuCommand mc) => Place(CreateUIDialog(null, "UIDialog", Vector2.zero, new Vector2(420, 260)), mc);

        #endregion

        #region ── 内部 helper ──

        static GameObject NewRoot(string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            return go;
        }

        static GameObject Attach(GameObject go, RectTransform parent, Vector2 anchoredPos)
        {
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
                var tr = (RectTransform)go.transform;
                tr.anchoredPosition = anchoredPos;
            }
            return go;
        }

        static GameObject NewChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static Image AddImage(GameObject go, Color color)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        static TextMeshProUGUI NewTMP(Transform parent, string name, string text, int fontSize, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        static void Stretch(RectTransform rt) => SetStretch(rt, 0, 0, 0, 0);

        static void SetStretch(RectTransform rt, float left, float bottom, float right, float top)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        static void AddTabToggle(Transform parent, string name, string label)
        {
            var go = NewChild(parent, name);
            var img = AddImage(go, new Color(0.35f, 0.35f, 0.4f, 1f));
            Stretch(go.GetComponent<RectTransform>());
            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = img;
            toggle.isOn = name == "Tab0";
            go.AddComponent<UIToggle>();
            var lbl = NewTMP(go.transform, "Label", label, DEFAULT_FONT, TextAlignmentOptions.Center, Color.white);
            Stretch(lbl.GetComponent<RectTransform>());
        }

        static void AddPage(Transform parent, string name, string text, Color bg)
        {
            var go = NewChild(parent, name);
            AddImage(go, bg);
            Stretch(go.GetComponent<RectTransform>());
            var lbl = NewTMP(go.transform, "Label", text, DEFAULT_FONT, TextAlignmentOptions.Center, Color.white);
            Stretch(lbl.GetComponent<RectTransform>());
        }

        static void Place(GameObject go, MenuCommand mc)
        {
            GameObject parent = mc.context as GameObject;
            if (parent == null || parent.GetComponent<RectTransform>() == null)
            {
                var canvas = Object.FindObjectOfType<Canvas>();
                parent = canvas != null ? canvas.gameObject : null;
            }
            string undo = "Create " + go.name;
            Undo.RegisterCreatedObjectUndo(go, undo);
            if (parent != null)
                GameObjectUtility.SetParentAndAlign(go, parent);
            Selection.activeGameObject = go;
        }

        #endregion
    }
}
