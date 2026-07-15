using Main.FuncModule;
using UnityEditor;
using UnityEngine;

namespace GameScripts.Editor
{
    public class LayerManagerPanel
    {
        public void Draw(MapEditorCore core)
        {
            if (core?.Config == null) return;

            EditorGUILayout.LabelField("图层管理器", EditorStyles.boldLabel);

            // ScrollView 由外层左侧面板统一管理，此处直接渲染
            var sorted = new System.Collections.Generic.List<LayerDefinition>(core.Config.layers);
            sorted.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));

            foreach (var layerDef in sorted)
                DrawLayerRow(core, layerDef);
        }

        private void DrawLayerRow(MapEditorCore core, LayerDefinition layerDef)
        {
            var state    = core.GetLayerState(layerDef.id);
            bool isActive = core.ActiveLayerId == layerDef.id;

            Color bgOld = GUI.backgroundColor;
            GUI.backgroundColor = isActive
                ? new Color(0.4f, 0.7f, 1f, 1f)
                : new Color(0.25f, 0.25f, 0.25f, 1f);

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(24));
            GUI.backgroundColor = bgOld;

            // ── 颜色色块 ──
            Rect colorRect = GUILayoutUtility.GetRect(12, 24, GUILayout.Width(12));
            EditorGUI.DrawRect(new Rect(colorRect.x, colorRect.y + 4, 10, 16), layerDef.debugColor);

            // ── 图层名（点击激活）──
            if (GUILayout.Button(layerDef.displayName, isActive ? EditorStyles.boldLabel : EditorStyles.label, GUILayout.ExpandWidth(true)))
            {
                core.ActiveLayerId = layerDef.id;
            }

            // ── 可见性 ──
            bool newVisible = GUILayout.Toggle(state.visible, state.visible ? "可见" : "不可见", GUILayout.Width(55));
            if (newVisible != state.visible) state.visible = newVisible;

            // ── 锁定 ──
            Color lockBg = GUI.backgroundColor;
            GUI.backgroundColor = state.locked ? new Color(1f, 0.7f, 0.3f) : bgOld;
            bool newLocked = GUILayout.Toggle(state.locked, state.locked ? "锁" : "不锁", GUILayout.Width(50));
            GUI.backgroundColor = lockBg;
            if (newLocked != state.locked) state.locked = newLocked;

            // ── 透明度 ──
            float newOpacity = GUILayout.HorizontalSlider(state.opacity, 0f, 1f, GUILayout.Width(60));
            if (!Mathf.Approximately(newOpacity, state.opacity)) state.opacity = newOpacity;

            EditorGUILayout.EndHorizontal();
        }
    }
}
