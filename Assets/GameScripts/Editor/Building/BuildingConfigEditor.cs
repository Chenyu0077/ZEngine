using Main.FuncModule.Building;
using UnityEditor;
using UnityEngine;

namespace GameScripts.Editor
{
    [CustomEditor(typeof(PlacedObjConfig))]
    public class BuildingConfigEditor : UnityEditor.Editor
    {

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("自动计算视觉尺寸", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"每格像素：{PlacedObjConfig.PixelsPerCell}px", EditorStyles.miniLabel);

            var cfg = (PlacedObjConfig)target;
            var sr  = cfg.GetComponentInChildren<SpriteRenderer>();

            if (sr == null || sr.sprite == null)
            {
                EditorGUILayout.HelpBox("未找到 SpriteRenderer（含子物体），无法自动计算。", MessageType.Info);
                return;
            }

            var texRect = sr.sprite.textureRect;
            int ppc   = PlacedObjConfig.PixelsPerCell;
            int autoX = Mathf.Max(1, Mathf.CeilToInt(texRect.width  / ppc));
            int autoY = Mathf.Max(1, Mathf.CeilToInt(texRect.height / ppc));

            EditorGUILayout.LabelField(
                $"Sprite 像素: {(int)texRect.width} × {(int)texRect.height}  →  {autoX} × {autoY} 格",
                EditorStyles.miniLabel);

            if (GUILayout.Button($"应用到 SizeX/SizeY  ({autoX} × {autoY})"))
            {
                Undo.RecordObject(cfg, "Auto Calc PlacedObjConfig Size");
                cfg.SizeX = autoX;
                cfg.SizeY = autoY;
                EditorUtility.SetDirty(cfg);
            }
        }
    }
}
