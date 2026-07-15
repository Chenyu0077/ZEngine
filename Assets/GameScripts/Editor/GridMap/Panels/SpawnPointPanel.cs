using Hotfix.FuncModule;
using UnityEditor;
using UnityEngine;

namespace GameScripts.Editor
{
    public class SpawnPointPanel
    {
        private SpawnPointSaveData _editing;

        public void Draw(MapEditorCore core)
        {
            if (core?.MapData == null || core.Config == null) return;

            EditorGUILayout.LabelField("出生点", EditorStyles.boldLabel);

            // 添加模式按钮
            Color bg = GUI.backgroundColor;
            bool isSpawnMode = core.EditMode == EditMode.SpawnEdit;
            GUI.backgroundColor = isSpawnMode ? new Color(0.4f, 0.8f, 1f) : bg;
            if (GUILayout.Button(isSpawnMode ? "✓ 点击地图添加出生点" : "进入出生点编辑模式", GUILayout.Height(26)))
                core.EditMode = isSpawnMode ? EditMode.None : EditMode.SpawnEdit;
            GUI.backgroundColor = bg;

            if (isSpawnMode)
            {
                EditorGUILayout.HelpBox("在 Scene View 中点击格子放置出生点。", MessageType.Info);
                DrawPendingConfig(core);
            }

            EditorGUILayout.Space(4);

            // 出生点列表（ScrollView 由外层统一管理）
            for (int i = 0; i < core.MapData.spawnPoints.Count; i++)
            {
                var sp = core.MapData.spawnPoints[i];
                DrawSpawnRow(core, sp, i);
            }
        }

        private void DrawPendingConfig(MapEditorCore core)
        {
            if (core.PendingSpawnPoint == null)
                core.PendingSpawnPoint = new SpawnPointSaveData { type = "npc", facing = "down" };

            var pending = core.PendingSpawnPoint;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("新出生点配置", EditorStyles.miniLabel);

            // 类型下拉
            string[] typeIds      = new string[core.Config.spawnPointTypes.Count];
            string[] typeDisplays = new string[core.Config.spawnPointTypes.Count];
            for (int i = 0; i < core.Config.spawnPointTypes.Count; i++)
            {
                typeIds[i]      = core.Config.spawnPointTypes[i].id;
                typeDisplays[i] = core.Config.spawnPointTypes[i].displayName;
            }
            int curTypeIdx = System.Array.IndexOf(typeIds, pending.type);
            if (curTypeIdx < 0) curTypeIdx = 0;
            int newTypeIdx = EditorGUILayout.Popup("类型", curTypeIdx, typeDisplays);
            if (newTypeIdx >= 0 && newTypeIdx < typeIds.Length)
                pending.type = typeIds[newTypeIdx];

            pending.npcId  = EditorGUILayout.TextField("NPC ID",    pending.npcId  ?? "");
            pending.facing = EditorGUILayout.TextField("朝向",       pending.facing ?? "down");

            EditorGUILayout.EndVertical();
        }

        private void DrawSpawnRow(MapEditorCore core, SpawnPointSaveData sp, int index)
        {
            bool isEditing = _editing == sp;

            Color bg = GUI.backgroundColor;
            GUI.backgroundColor = isEditing ? new Color(0.5f, 0.8f, 1f) : new Color(0.22f, 0.22f, 0.22f);
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUI.backgroundColor = bg;

            // 颜色圆点
            Color gizmoColor = Color.blue;
            foreach (var typeDef in core.Config.spawnPointTypes)
                if (typeDef.id == sp.type) { gizmoColor = typeDef.gizmoColor; break; }

            Rect dotRect = GUILayoutUtility.GetRect(12, 20, GUILayout.Width(12));
            EditorGUI.DrawRect(new Rect(dotRect.x, dotRect.y + 4, 10, 10), gizmoColor);

            // 标签
            GUILayout.Label($"[{sp.type}] ({sp.x},{sp.y})", EditorStyles.miniLabel, GUILayout.ExpandWidth(true));

            // 编辑按钮
            if (GUILayout.Button("✏", GUILayout.Width(24)))
                _editing = isEditing ? null : sp;

            // 删除按钮
            Color deleteBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("✕", GUILayout.Width(24)))
                core.RemoveSpawnPoint(sp);
            GUI.backgroundColor = deleteBg;

            EditorGUILayout.EndHorizontal();

            if (isEditing)
                DrawSpawnEditInline(core, sp);
        }

        private void DrawSpawnEditInline(MapEditorCore core, SpawnPointSaveData sp)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            string[] typeIds      = new string[core.Config.spawnPointTypes.Count];
            string[] typeDisplays = new string[core.Config.spawnPointTypes.Count];
            for (int i = 0; i < core.Config.spawnPointTypes.Count; i++)
            {
                typeIds[i]      = core.Config.spawnPointTypes[i].id;
                typeDisplays[i] = core.Config.spawnPointTypes[i].displayName;
            }
            int curTypeIdx = System.Array.IndexOf(typeIds, sp.type);
            if (curTypeIdx < 0) curTypeIdx = 0;
            int newTypeIdx = EditorGUILayout.Popup("类型", curTypeIdx, typeDisplays);
            if (newTypeIdx >= 0) sp.type = typeIds[newTypeIdx];

            sp.npcId  = EditorGUILayout.TextField("NPC ID", sp.npcId  ?? "");
            sp.facing = EditorGUILayout.TextField("朝向",    sp.facing ?? "down");
            sp.x      = EditorGUILayout.IntField("X",       sp.x);
            sp.y      = EditorGUILayout.IntField("Y",       sp.y);

            EditorGUILayout.EndVertical();
        }
    }
}
