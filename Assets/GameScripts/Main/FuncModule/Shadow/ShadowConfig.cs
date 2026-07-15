using UnityEngine;

namespace Main.FuncModule.Shadow
{
    [CreateAssetMenu(fileName = "ShadowConfig", menuName = "Game/Shadow/ShadowConfig")]
    public class ShadowConfig : ScriptableObject
    {
        [Header("投影参数")]
        [Range(0.05f, 0.5f)]
        public float shadowScaleY = 0.25f;  // Y 轴压扁比

        [Range(0f, 5f)]
        public float shadowLengthMin = 0.3f;

        [Range(0f, 5f)]
        public float shadowLengthMax = 2.0f;

        [Header("外观")]
        public Color shadowColor = new Color(0f, 0f, 0f, 0.45f);

        [Range(0f, 1f)]
        [Tooltip("低于此 Alpha 值的像素不参与阴影渲染，消除透明区域渗入问题")]
        public float alphaCutoff = 0.1f;

        [Header("时间曲线")]
        [Tooltip("X: 游戏小时 [0,24]，Y: 归一化值 [0,1]，值越大对应 shadowLengthMax，正午应为最大值（最短投影）")]
        public AnimationCurve lengthCurve = DefaultCurve();

        private static AnimationCurve DefaultCurve()
        {
            var curve = new AnimationCurve();
            curve.AddKey(new Keyframe(0f,  0f));
            curve.AddKey(new Keyframe(6f,  0f));
            curve.AddKey(new Keyframe(12f, 1f));
            curve.AddKey(new Keyframe(18f, 0f));
            curve.AddKey(new Keyframe(24f, 0f));
            return curve;
        }
    }
}
