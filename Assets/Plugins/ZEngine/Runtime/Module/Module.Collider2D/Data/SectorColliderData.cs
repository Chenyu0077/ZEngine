//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------


using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace ZEngine.Module.Collider2D
{
    [Serializable]
    public struct SectorColliderData
    {
        [SerializeField, Header("扇心")]
        public Vector2 Center;
        [SerializeField, Header("半径"), MinValue(0.01f), MaxValue(25f)]
        public float Radius;
        [SerializeField, Header("角度"), MinValue(1f), MaxValue(360f)]
        public float Angle;
        [SerializeField, Header("扇形起始角度")]
        public float StartAngle;
        public int Segments;

        public SectorColliderData(Vector2 center, float radius, float angle, float startAngle = 0, int segments = 30)
        {
            Center = center;
            Radius = radius;
            Angle = angle;
            StartAngle = startAngle;
            Segments = segments;
        }

        public Vector2[] GetWorldVectors()
        {
            List<Vector2> vector2s = new List<Vector2>();

            float angleUnit = (Angle * Mathf.PI) / (180 * Segments);//分割弧线对应的带π角度
            float angleStart = StartAngle * Mathf.PI / 180;//开始角度对应的带π角度
            for (int i = 0; i < Segments; i++)
            {
                float tempAngle = angleStart + angleUnit * i;
                Vector2 vec = Center + new Vector2(Mathf.Cos(tempAngle), Mathf.Sin(tempAngle)) * Radius;
                vector2s.Add(vec);
            }
            vector2s.Add(Center);

            return vector2s.ToArray();
        }
    }
}
