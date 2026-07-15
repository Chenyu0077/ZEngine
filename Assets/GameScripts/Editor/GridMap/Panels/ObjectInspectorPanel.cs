using Hotfix.FuncModule;
using Main.FuncModule;
using UnityEditor;
using UnityEngine;

namespace GameScripts.Editor
{
    /// <summary>
    /// Schema 驱动的放置对象属性检查器。
    /// 在 MapEditorWindow 右侧面板调用 Draw()，展示并编辑 SelectedObject.props。
    /// </summary>
    public class ObjectInspectorPanel
    {
        private Vector2 _scroll;
        
        public void Draw(MapEditorCore core)
        {
            if (core?.Config == null) return;

            EditorGUILayout.LabelField("对象属性", EditorStyles.boldLabel);

            var obj = core.SelectedObject;
            if (obj == null)
            {
                EditorGUILayout.HelpBox("在 Scene View 中单击已放置的对象以编辑属性。", MessageType.None);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"{obj.prefabId}  [{obj.objectType}]", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"坐标 ({obj.x}, {obj.y})  实例 {obj.instanceId}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            if (core.Config.objectPropertySchema.Count == 0)
            {
                EditorGUILayout.HelpBox("Config 中尚未定义 objectPropertySchema，请在 MapEditorConfig 中添加。", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            
            foreach (var def in core.Config.objectPropertySchema)
                DrawPropertyField(core, obj, def);
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawPropertyField(MapEditorCore core, ObjectSaveData obj, ObjectPropertyDef def)
        {
            string current = core.GetObjProp(obj, def.key);
            EditorGUILayout.BeginHorizontal();

            switch (def.valueType)
            {
                case PropType.Bool:
                {
                    bool cur  = string.Equals(current, "true", System.StringComparison.OrdinalIgnoreCase);
                    bool next = EditorGUILayout.Toggle(def.displayName, cur);
                    if (next != cur)
                        core.SetObjProp(obj, def.key, next ? "true" : "false");
                    break;
                }
                case PropType.Int:
                {
                    int.TryParse(current, out int cur);
                    int next = EditorGUILayout.IntField(def.displayName, cur);
                    if (next != cur)
                        core.SetObjProp(obj, def.key, next.ToString());
                    break;
                }
                case PropType.Float:
                {
                    float.TryParse(current, out float cur);
                    float next = EditorGUILayout.FloatField(def.displayName, cur);
                    if (!Mathf.Approximately(next, cur))
                        core.SetObjProp(obj, def.key, next.ToString("F3"));
                    break;
                }
                case PropType.String:
                {
                    string next = EditorGUILayout.TextField(def.displayName, current);
                    if (next != current)
                        core.SetObjProp(obj, def.key, next);
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
                        core.SetObjProp(obj, def.key, ids[nextIdx]);
                    break;
                }
            }

            // 重置按钮（非默认值时显示）
            bool isDefault = string.Equals(current, def.defaultValue ?? "", System.StringComparison.OrdinalIgnoreCase);
            if (!isDefault)
            {
                Color bg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.7f, 0.5f);
                if (GUILayout.Button("↺", GUILayout.Width(22)))
                    core.SetObjProp(obj, def.key, def.defaultValue ?? "");
                GUI.backgroundColor = bg;
            }
            else
            {
                GUILayout.Space(26);
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
