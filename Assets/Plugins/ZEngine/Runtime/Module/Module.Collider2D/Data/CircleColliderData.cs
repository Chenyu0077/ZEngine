//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using UnityEngine;

namespace ZEngine.Module.Collider2D
{
    [Serializable]
    public struct CircleColliderData
    {
        public Vector2 Center;
        public float Radius;
        public int Segments;

        public CircleColliderData(Vector2 center, float radius, int segments = 30)
        {
            Center = center;
            Radius = radius;
            Segments = segments;
        }

        public Vector2[] GetWorldVectors()
        {
            Vector2[] vector2s = new Vector2[Segments];

            float angleUnit = 2 * Mathf.PI / Segments;
            for (int i = 0; i < Segments; i++)
            {
                float angle = angleUnit * i;
                Vector2 vec = Center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Radius;
                vector2s[i] = vec;
            }

            return vector2s;
        }
    }
}
