using Main.FuncModule;
using UnityEditor;
using UnityEngine;

namespace GameScripts.Editor
{
    [CustomEditor(typeof(TileSetData))]
    public class TileSetDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 沿用默认 Inspector（保留完整的 tiles 列表增删改能力）
            DrawDefaultInspector();

            // 在有任意 TileEntry 启用随机变体时，显示索引顺序警告
            var tsd = (TileSetData)target;
            bool anyRandom = false;
            foreach (var entry in tsd.tiles)
            {
                if (entry.useRandom)
                {
                    anyRandom = true;
                    break;
                }
            }

            if (anyRandom)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "提示：useRandom 启用时，由 seed ^ 格子坐标 确定每格外观。\n" +
                    "修改 seed 会使整张地图的随机结果全部重新计算。",
                    MessageType.Info);
            }
        }
    }
}
