Shader "Game/Shadow2D"
{
    Properties
    {
        _MainTex             ("Sprite Texture", 2D)            = "white" {}
        _ShadowColor         ("Shadow Color", Color)           = (0, 0, 0, 0.45)
        _SunDir              ("Sun Direction", Vector)         = (1, 0, 0, 0)
        _ShadowLength        ("Shadow Length", Float)          = 1.0
        _ShadowScaleY        ("Shadow Scale Y", Range(0.05, 1.0)) = 0.25
        _AlphaCutoff         ("Alpha Cutoff",   Range(0, 1))   = 0.1

        // 由 ShadowRenderer 写入：xy = 锚点局部坐标，zw = 高度方向单位向量
        _AnchorAndHeightDir  ("Anchor And Height Dir", Vector) = (0, 0, 0, 1)
        _TotalHeight         ("Total Height", Float)           = 1.0

        // 物体 Z 轴旋转的 sin/cos，用于将世界空间太阳位移转回局部空间
        _SinZ                ("Sin Z Rotation", Float)         = 0.0
        _CosZ                ("Cos Z Rotation", Float)         = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent-1"
            "RenderType"        = "Transparent"
            "IgnoreProjector"   = "True"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4    _ShadowColor;
            float4    _SunDir;
            float     _ShadowLength;
            float     _ShadowScaleY;
            float     _AlphaCutoff;
            float4    _AnchorAndHeightDir;
            float     _TotalHeight;
            float     _SinZ;
            float     _CosZ;

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float2 anchor    = _AnchorAndHeightDir.xy;   // 地面锚点（局部坐标）
                float2 heightDir = _AnchorAndHeightDir.zw;   // 高度增大方向（归一化）

                // ① 当前顶点相对于锚点的"高度"（沿 heightDir 的投影）
                float h      = dot(v.vertex.xy - anchor, heightDir);
                float hRatio = h / max(_TotalHeight, 0.001);

                // ② 压扁：沿 heightDir 将顶点向锚点方向压缩
                //    scaleY < 1 时 (scaleY-1) < 0，squishDisp 与 heightDir 反向 → 向锚点收缩
                float2 squishDisp = heightDir * (h * (_ShadowScaleY - 1.0));

                // ③ 太阳方向剪切：在世界空间计算位移，再逆旋转回局部空间
                //    太阳方向是世界坐标，但顶点在局部坐标，必须转换才能正确叠加。
                //    逆 Z 旋转公式（旋转矩阵的转置）：
                //      local.x =  world.x * cosZ + world.y * sinZ
                //      local.y = -world.x * sinZ + world.y * cosZ
                float2 sunWorld;
                sunWorld.x = -_SunDir.x * _ShadowLength * hRatio;
                sunWorld.y = -_SunDir.y * _ShadowLength * hRatio;
                float2 sunDisp;
                sunDisp.x =  sunWorld.x * _CosZ + sunWorld.y * _SinZ;
                sunDisp.y = -sunWorld.x * _SinZ + sunWorld.y * _CosZ;

                v.vertex.xy += squishDisp + sunDisp;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                clip(tex.a - _AlphaCutoff);
                return fixed4(_ShadowColor.rgb, _ShadowColor.a * tex.a);
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
