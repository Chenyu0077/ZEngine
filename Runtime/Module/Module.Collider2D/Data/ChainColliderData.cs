//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using UnityEngine;

namespace ZEngine.Module.Collider2D
{
    [Serializable]
    public struct ChainColliderData
    {
        public Vector2 Center;
        public Vector2[] Points;
        public Vector2[] worldPoints
        {
            get
            {
                Vector2[] newPoints = new Vector2[Points.Length];
                for (int i = 0; i < Points.Length; i++)
                    newPoints[i] = Center + Points[i];
                return newPoints;
            }
        }

        public ChainColliderData(Vector2 position, Vector2[] points)
        {
            Center = position;
            Points = points;
        }

        public Vector2[] GetWorldVectors()
        {
            return worldPoints;
        }
    }
}
