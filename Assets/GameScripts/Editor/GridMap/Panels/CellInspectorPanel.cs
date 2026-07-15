using System.Collections.Generic;
using Main.FuncModule;
using UnityEditor;
using UnityEngine;

namespace GameScripts.Editor
{
    /// <summary>
    /// Schema 驱动的格子属性检查器（单格编辑 + 批量修改）。
    /// </summary>
    public class CellInspectorPanel
    {
        private Vector2 _scroll;

        // 批量修改时每个属性的待设定值
        private readonly Dictionary<string, string> _batchValues = new Dictionary<string, string>();

        public void Draw(MapEditorCore core)
        {
            if (core?.Config == null) return;

            var cell = core.SelectedCell;
            bool hasSelection = cell.x >= 0 && core.IsValidCell(cell.x, cell.y);

            EditorGUILayout.LabelField("格子属性", EditorStyles.boldLabel);

            if (!hasSelection)
            {
                EditorGUILayout.HelpBox("在 Scene View 中点击格子（C 模式）以编辑属性。", MessageType.None);
                return;
            }

            EditorGUILayout.LabelField($"坐标 ({cell.x}, {cell.y})", EditorStyles.miniLabel);
            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (var def in core.Config.cellPropertySchema)
            {
                DrawPropertyField(core, cell.x, cell.y, def);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawPropertyField(MapEditorCore core, int x, int y, CellPropertyDef def)
        {
            string current = core.GetCellProp(x, y, def.key);
            EditorGUILayout.BeginHorizontal();

            switch (def.valueType)
            {
                case PropType.Bool:
                {
                    bool cur = string.Equals(current, "true", System.StringComparison.OrdinalIgnoreCase);
                    bool next = EditorGUILayout.Toggle(def.displayName, cur);
                    if (next != cur)
                        core.SetCellProp(x, y, def.key, next ? "true" : "false");
                    break;
                }
                case PropType.Int:
                {
                    int.TryParse(current, out int cur);
                    int next = EditorGUILayout.IntField(def.displayName, cur);
                    if (next != cur)
                        core.SetCellProp(x, y, def.key, next.ToString());
                    break;
                }
                case PropType.Float:
                {
                    float.TryParse(current, out float cur);
                    float next = EditorGUILayout.FloatField(def.displayName, cur);
                    if (!Mathf.Approximately(next, cur))
                        core.SetCellProp(x, y, def.key, next.ToString("F3"));
                    break;
                }
                case PropType.String:
                {
                    string next = EditorGUILayout.TextField(def.displayName, current);
                    if (next != current)
                        core.SetCellProp(x, y, def.key, next);
                    break;
                }
                case PropType.Enum:
                {
                    string[] ids      = core.Config.GetEnumOptions(def.enumOptionsRef);
                    string[] displays = core.Config.GetEnumDisplayNames(def.enumOptionsRef);
                    if (ids.Length == 0) break;

                    int curIdx  = System.Array.IndexOf(ids, current);
                    if (curIdx < 0) curIdx = 0;
                    int nextIdx = EditorGUILayout.Popup(def.displayName, curIdx, displays);
                    if (nextIdx != curIdx)
                        core.SetCellProp(x, y, def.key, ids[nextIdx]);
                    break;
                }
            }

            // 重置按钮（仅在非默认值时显示）
            bool isDefault = string.Equals(current, def.defaultValue ?? "", System.StringComparison.OrdinalIgnoreCase);
            if (!isDefault)
            {
                Color bg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.7f, 0.5f);
                if (GUILayout.Button("↺", GUILayout.Width(22)))
                    core.SetCellProp(x, y, def.key, def.defaultValue ?? "");
                GUI.backgroundColor = bg;
            }
            else
            {
                GUILayout.Space(26);
            }

            EditorGUILayout.EndHorizontal();
        }

        // ── 批量修改面板（Select 模式有选区时使用）────────────────────────
        public void DrawBatchEdit(MapEditorCore core)
        {
            if (core?.Config == null || !core.HasSelection) return;

            EditorGUILayout.LabelField("批量修改属性", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"已选中 {core.SelectionCount} 格（{core.SelectionW} × {core.SelectionH}）",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(4);

            foreach (var def in core.Config.cellPropertySchema)
            {
                // 初始化待设定值为该属性默认值
                if (!_batchValues.ContainsKey(def.key))
                    _batchValues[def.key] = def.defaultValue ?? "";

                string cur = _batchValues[def.key];
                string next = cur;

                EditorGUILayout.BeginHorizontal();

                switch (def.valueType)
                {
                    case PropType.Bool:
                        bool b = string.Equals(cur, "true", System.StringComparison.OrdinalIgnoreCase);
                        bool nb = EditorGUILayout.Toggle(def.displayName, b);
                        if (nb != b) next = nb ? "true" : "false";
                        break;
                    case PropType.Int:
                        int.TryParse(cur, out int iv);
                        int niv = EditorGUILayout.IntField(def.displayName, iv);
                        if (niv != iv) next = niv.ToString();
                        break;
                    case PropType.Float:
                        float.TryParse(cur, out float fv);
                        float nfv = EditorGUILayout.FloatField(def.displayName, fv);
                        if (!Mathf.Approximately(nfv, fv)) next = nfv.ToString("F3");
                        break;
                    case PropType.String:
                        string ns = EditorGUILayout.TextField(def.displayName, cur);
                        if (ns != cur) next = ns;
                        break;
                    case PropType.Enum:
                        string[] ids      = core.Config.GetEnumOptions(def.enumOptionsRef);
                        string[] displays = core.Config.GetEnumDisplayNames(def.enumOptionsRef);
                        if (ids.Length > 0)
                        {
                            int idx  = System.Array.IndexOf(ids, cur);
                            if (idx < 0) idx = 0;
                            int nIdx = EditorGUILayout.Popup(def.displayName, idx, displays);
                            if (nIdx != idx) next = ids[nIdx];
                        }
                        break;
                }

                _batchValues[def.key] = next;

                Color bg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
                if (GUILayout.Button("应用", GUILayout.Width(36)))
                {
                    core.BatchSetCellProp(def.key, next);
                    SceneView.RepaintAll();
                }
                GUI.backgroundColor = bg;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);
            Color bg2 = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.6f, 0.3f);
            if (GUILayout.Button("清除选区", GUILayout.Height(22)))
                core.ClearSelection();
            GUI.backgroundColor = bg2;
        }

        public void DrawOverlayToggles(MapEditorCore core)
        {
            if (core?.Config == null) return;
            EditorGUILayout.LabelField("叠加层显示", EditorStyles.boldLabel);
            foreach (var def in core.Config.cellPropertySchema)
            {
                if (!def.enableOverlay) continue;
                bool cur  = core.IsOverlayEnabled(def.key);
                bool next = EditorGUILayout.Toggle(def.displayName, cur);
                if (next != cur) core.SetOverlayEnabled(def.key, next);
            }
        }

        // ── 空格子批量属性修改 ──────────────────────────────────────────
        private readonly Dictionary<string, string> _emptyBatchValues = new Dictionary<string, string>();
        private bool _emptyBatchFoldout = false;

        public void DrawEmptyCellBatchEdit(MapEditorCore core)
        {
            if (core?.Config == null || core.MapData == null) return;
            if (core.Config.cellPropertySchema.Count == 0) return;

            int emptyCount = core.CountEmptyCells();
            if (emptyCount == 0)
            {
                EditorGUILayout.HelpBox("地图中没有空格子。", MessageType.Info);
                return;
            }

            _emptyBatchFoldout = EditorGUILayout.Foldout(_emptyBatchFoldout,
                $"空格子批量修改  [{emptyCount} 格]", true);
            if (!_emptyBatchFoldout) return;

            EditorGUILayout.Space(2);

            foreach (var def in core.Config.cellPropertySchema)
            {
                if (!_emptyBatchValues.ContainsKey(def.key))
                    _emptyBatchValues[def.key] = def.defaultValue ?? "";

                string cur = _emptyBatchValues[def.key];
                string next = cur;

                EditorGUILayout.BeginHorizontal();

                switch (def.valueType)
                {
                    case PropType.Bool:
                        bool b = string.Equals(cur, "true", System.StringComparison.OrdinalIgnoreCase);
                        bool nb = EditorGUILayout.Toggle(def.displayName, b);
                        if (nb != b) next = nb ? "true" : "false";
                        break;
                    case PropType.Int:
                        int.TryParse(cur, out int iv);
                        int niv = EditorGUILayout.IntField(def.displayName, iv);
                        if (niv != iv) next = niv.ToString();
                        break;
                    case PropType.Float:
                        float.TryParse(cur, out float fv);
                        float nfv = EditorGUILayout.FloatField(def.displayName, fv);
                        if (!Mathf.Approximately(nfv, fv)) next = nfv.ToString("F3");
                        break;
                    case PropType.String:
                        string ns = EditorGUILayout.TextField(def.displayName, cur);
                        if (ns != cur) next = ns;
                        break;
                    case PropType.Enum:
                        string[] ids      = core.Config.GetEnumOptions(def.enumOptionsRef);
                        string[] displays = core.Config.GetEnumDisplayNames(def.enumOptionsRef);
                        if (ids.Length > 0)
                        {
                            int idx  = System.Array.IndexOf(ids, cur);
                            if (idx < 0) idx = 0;
                            int nIdx = EditorGUILayout.Popup(def.displayName, idx, displays);
                            if (nIdx != idx) next = ids[nIdx];
                        }
                        break;
                }

                _emptyBatchValues[def.key] = next;

                Color bg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
                if (GUILayout.Button("应用", GUILayout.Width(36)))
                {
                    core.BatchSetEmptyCellProp(def.key, next);
                    SceneView.RepaintAll();
                }
                GUI.backgroundColor = bg;

                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
