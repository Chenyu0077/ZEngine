//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;

namespace ZEngine.Extension
{
    public static class Vector3Extension
    {
        public static Vector2 V3ToXY(this Vector3 v)
        {
            return new Vector2(v.x, v.y);
        }

        public static Vector2 V3ToXZ(this Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }

        public static Vector2 V3ToYZ(this Vector3 v)
        {
            return new Vector2(v.y, v.z);
        }
    }
}
