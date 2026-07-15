//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------


using System;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace ZEngine.Module.Collider2D
{
    [Serializable]
    public struct PolygonColliderData
    {
        public Vector2 Center;
        //多边形顶点，顺时针
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

        public PolygonColliderData(Vector2 center, Vector2[] points)
        {
            Center = center;
            Points = points;
        }

        public bool IsValid()
        {
            if (Points == null || Points.Length < 3)
                return false;

            return true;
        }

        public Vector2[] GetWorldVectors()
        {
            return worldPoints;
        }
    }
}
