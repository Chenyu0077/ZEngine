//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;

namespace Hotfix.Main.Utils
{
    public static class ExternalTypeUtil
    {
        public static Vector2 NewVector2(cfg.vector2 v)
        {
            return new Vector2(v.X, v.Y);
        }

        public static Vector3 NewVector3(cfg.vector3 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public static Vector4 NewVector4(cfg.vector4 v)
        {
            return new Vector4(v.X, v.Y, v.Z, v.W);
        }
    }
}
