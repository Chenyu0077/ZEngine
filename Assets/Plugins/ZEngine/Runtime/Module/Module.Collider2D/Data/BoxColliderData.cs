//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using UnityEngine;

namespace ZEngine.Module.Collider2D
{
    [Serializable]
    public struct BoxColliderData
    {
        public Vector2 Center;
        public Vector2 Size;

        public float xMin { get { return Center.x - Size.x / 2; } }
        public float xMax { get { return Center.x + Size.x / 2; } }
        public float yMin { get { return Center.y - Size.y / 2; } }
        public float yMax { get { return Center.y + Size.y / 2; } }

        public BoxColliderData(Vector2 position, Vector2 size)
        {
            Center = position;
            Size = size;
        }

        public Vector2[] GetWorldVectors()
        {
            return new Vector2[]
            {
                new Vector2(xMin, yMin),
                new Vector2(xMin, yMax),
                new Vector2(xMax, yMax),
                new Vector2(xMax, yMin),
            };
        }
    }
}
