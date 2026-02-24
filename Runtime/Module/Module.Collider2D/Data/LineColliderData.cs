//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;

namespace ZEngine.Module.Collider2D
{
    public struct LineColliderData
    {
        public Vector2 Center;
        public Vector2 V1;
        public Vector2 V2;

        public Vector2 worldV1 => Center + V1;
        public Vector2 worldV2 => Center + V2;

        public LineColliderData(Vector2 center, Vector2 v1, Vector2 v2)
        {
            Center = center;
            V1 = v1;
            V2 = v2;
        }

        public Vector2[] GetWorldVectors()
        {
            return new Vector2[] { worldV1, worldV2 };
        }
    }
}
