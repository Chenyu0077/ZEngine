//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System.Collections.Generic;
using FairyGUI;

namespace ZEngine.Manager.UI
{
    public static class UILayer
    {
        public static GComponent Background_Compt;
        public static GComponent Bottom_Compt;
        public static GComponent Middle_Compt;
        public static GComponent Top_Compt;
        public static GComponent Window_Compt;
        public static GComponent Guide_Compt;
        public static GComponent Max_Compt;

        public static Dictionary<EUILayer, GComponent> LayerDic = new Dictionary<EUILayer, GComponent>();

        public static void Initialize()
        {
            Background_Compt = new GComponent() { gameObjectName = "Background" };
            Bottom_Compt = new GComponent() { gameObjectName = "Bottom_Compt" };
            Middle_Compt = new GComponent() { gameObjectName = "Middle_Compt" };
            Top_Compt = new GComponent() { gameObjectName = "Top_Compt" };
            Window_Compt = new GComponent() { gameObjectName = "Window_Compt" };
            Guide_Compt = new GComponent() { gameObjectName = "Guide_Compt" };
            Max_Compt = new GComponent() { gameObjectName = "Max_Compt" };

            LayerDic.Add(EUILayer.Background_Layer, Background_Compt);
            LayerDic.Add(EUILayer.Bottom_Layer, Bottom_Compt);
            LayerDic.Add(EUILayer.Middle_Layer, Middle_Compt);
            LayerDic.Add(EUILayer.Top_Layer, Top_Compt);
            LayerDic.Add(EUILayer.Window_Layer, Window_Compt);
            LayerDic.Add(EUILayer.Guide_Layer, Guide_Compt);
            LayerDic.Add(EUILayer.Max_Layer, Max_Compt);

            var gRoot = GRoot.inst;
            gRoot.AddChild(Background_Compt);
            gRoot.AddChild(Bottom_Compt);
            gRoot.AddChild(Middle_Compt);
            gRoot.AddChild(Top_Compt);
            gRoot.AddChild(Window_Compt);
            gRoot.AddChild(Guide_Compt);
            gRoot.AddChild(Max_Compt);
        }
    }


    /// <summary>
    /// UI层级枚举
    /// </summary>
    public enum EUILayer
    {
        /// <summary>
        /// 背景层
        /// </summary>
        Background_Layer = 1000,
        /// <summary>
        /// view底层
        /// </summary>
        Bottom_Layer = 2000,
        /// <summary>
        /// view中层
        /// </summary>
        Middle_Layer = 3000,
        /// <summary>
        /// view上层
        /// </summary>
        Top_Layer = 4000,
        /// <summary>
        /// 弹窗层类型
        /// </summary>
        Window_Layer = 5000,
        /// <summary>
        /// 引导层类型
        /// </summary>
        Guide_Layer = 6000,
        /// <summary>
        /// 最外层类型
        /// </summary>
        Max_Layer = 7000,
    }
}


