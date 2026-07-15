using UnityEngine;

namespace Main.FuncModule.Building
{
    /// <summary>
    /// 放置物的配置
    /// </summary>
    public class PlacedObjConfig : MonoBehaviour
    {
        public static int PixelsPerCell = 32;

        [Header("对象类型")]
        public MapObjectType ObjectType = MapObjectType.Other;

        [Header("Sprite实际占地")]
        [Min(1)] public int SizeX = 1;
        [Min(1)] public int SizeY = 1;

        [Header("碰撞占地（底部实际占格，0 = 与 Size 相同）")]
        [Min(0)] public int FootprintX = 0;
        [Min(0)] public int FootprintY = 0;

        public int ActualFootprintX => FootprintX > 0 ? FootprintX : SizeX;
        public int ActualFootprintY => FootprintY > 0 ? FootprintY : SizeY;
    }
}
