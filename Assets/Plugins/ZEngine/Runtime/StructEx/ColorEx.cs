using UnityEngine;

namespace ZEngine.StructEx
{
    public struct ColorEx
    {
        public float r;
        public float g;
        public float b;
        public float a;
        
        public ColorEx(float r, float g, float b, float a = 1f)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }
        
        public Color ConvertToColor()
        {
            return new Color(r, g, b, a);
        }

        public static ColorEx ConvertToColorEx(Color color)
        {
            return new ColorEx(color.r, color.g, color.b, color.a);
        }
    }   
}
