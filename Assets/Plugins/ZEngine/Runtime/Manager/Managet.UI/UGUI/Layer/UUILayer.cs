using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZEngine.Manager.UI.UGUI
{
    public static class UILayer
    {
        public static GameObject UIRoot;
        public static GameObject Background_Compt;
        public static GameObject Bottom_Compt;
        public static GameObject Middle_Compt;
        public static GameObject Top_Compt;
        public static GameObject Window_Compt;
        public static GameObject Guide_Compt;
        public static GameObject Max_Compt;

        public static Dictionary<UUILayer, GameObject> LayerDic = new Dictionary<UUILayer, GameObject>();

        // 手动初始化UIRoot
        public static void Initialize(GameObject root)
        {
            // Bug 4: 重复调用时清空旧状态，避免 LayerDic.Add 抛 ArgumentException
            LayerDic.Clear();

            UIRoot = new GameObject("UIRoot");
            if (root != null)
            {
                UIRoot.transform.SetParent(root.transform, false);
            }

            // Canvas 使用 ScreenSpaceOverlay，确保屏幕 UI 正确渲染
            var canvas = UIRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = UIRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(750, 1334);
            scaler.matchWidthOrHeight = 0.5f;
            UIRoot.AddComponent<GraphicRaycaster>();

            // 创建各层级容器（带 RectTransform，Stretch 填满父级）
            Background_Compt = CreateLayerContainer("Background", UIRoot.transform);
            Bottom_Compt = CreateLayerContainer("Bottom", UIRoot.transform);
            Middle_Compt = CreateLayerContainer("Middle", UIRoot.transform);
            Top_Compt = CreateLayerContainer("Top", UIRoot.transform);
            Window_Compt = CreateLayerContainer("Window", UIRoot.transform);
            Guide_Compt = CreateLayerContainer("Guide", UIRoot.transform);
            Max_Compt = CreateLayerContainer("Max", UIRoot.transform);

            LayerDic.Add(UUILayer.Background_Layer, Background_Compt);
            LayerDic.Add(UUILayer.Bottom_Layer, Bottom_Compt);
            LayerDic.Add(UUILayer.Middle_Layer, Middle_Compt);
            LayerDic.Add(UUILayer.Top_Layer, Top_Compt);
            LayerDic.Add(UUILayer.Window_Layer, Window_Compt);
            LayerDic.Add(UUILayer.Guide_Layer, Guide_Compt);
            LayerDic.Add(UUILayer.Max_Layer, Max_Compt);

            // 代码创建的 Canvas 不会自动生成 EventSystem，需手动补充，否则 UI 事件无法响应
            if (EventSystem.current == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.transform.SetParent(root != null ? root.transform : null);
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }
        }

        private static GameObject CreateLayerContainer(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }
    }


    /// <summary>
    /// UI层级枚举
    /// </summary>
    public enum UUILayer
    {
        Background_Layer = 1000,
        Bottom_Layer = 2000,
        Middle_Layer = 3000,
        Top_Layer = 4000,
        Window_Layer = 5000,
        Guide_Layer = 6000,
        Max_Layer = 7000,
    }
}
