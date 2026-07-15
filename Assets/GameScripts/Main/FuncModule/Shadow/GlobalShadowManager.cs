using System.Collections.Generic;
using Main.Core;
using UnityEngine;

namespace Main.FuncModule.Shadow
{
    /// <summary>
    /// 全局阴影管理器：维护 ShadowRenderer 注册表，提供太阳方向计算工具。
    /// </summary>
    public class GlobalShadowManager : Singleton<GlobalShadowManager>
    {
        private readonly List<ShadowRenderer> _renderers = new List<ShadowRenderer>();

        public void Register(ShadowRenderer renderer)
        {
            if (renderer != null && !_renderers.Contains(renderer))
                _renderers.Add(renderer);
        }

        public void Unregister(ShadowRenderer renderer)
        {
            _renderers.Remove(renderer);
        }

        /// <summary>
        /// 根据游戏小时计算太阳方向单位向量（X: 东西，Y: 南北）。
        /// 6:00 → 太阳在东(+X)，12:00 → 太阳偏南(-Y)，18:00 → 太阳在西(-X)。
        /// </summary>
        public static Vector2 CalcSunDir(float hour)
        {
            float t     = Mathf.Clamp01((hour - 6f) / 12f);
            float angle = t * Mathf.PI;
            float x     =  Mathf.Cos(angle);          // +1(东) → 0 → -1(西)
            float y     = -Mathf.Sin(angle) * 0.3f;   // 0 → -0.3(偏南) → 0
            return new Vector2(x, y).normalized;
        }

        /// <summary>
        /// 游戏时间是否处于白天（白天可投射阴影，夜晚晴天才可投射阴影）
        /// </summary>
        public static bool IsDaytime(float hour)
        {
            float h = hour % 24f;
            return h >= 6f && h <= 18f;
        }

        protected override void DestroySingleton()
        {
            _renderers.Clear();
            base.DestroySingleton();
        }
    }
}
