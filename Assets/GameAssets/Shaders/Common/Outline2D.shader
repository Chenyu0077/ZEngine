Shader "Game/Outline2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth ("Outline Width", Range(0, 16)) = 1
        _AlphaCutoff ("Alpha Cutoff", Range(0.001, 1)) = 0.1
        [Toggle] _OutlineEnabled ("Enable Outline", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Stencil
        {
            Ref 1
            Comp Always
            Pass Replace
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            #define OUTLINE_MAX_RADIUS 16

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4    _MainTex_TexelSize;
            float4    _Color;
            float4    _OutlineColor;
            float     _OutlineWidth;
            float     _AlphaCutoff;
            float     _OutlineEnabled;

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            bool SampleAlpha(float2 uv, float2 dir, float radius, float2 texelSize)
            {
                return tex2D(_MainTex, uv + dir * radius * texelSize).a > _AlphaCutoff;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                if (col.a > _AlphaCutoff)
                    return col * i.color;

                // 如果 outline 被禁用，直接返回透明
                if (_OutlineEnabled < 0.5)
                    return fixed4(0, 0, 0, 0);

                // 一像素对应的uv尺寸
                float2 ts = _MainTex_TexelSize.xy;

                // 每个像素进行八方向搜索
                [loop]
                for (int r = 1; r <= OUTLINE_MAX_RADIUS; r++)
                {
                    if (r > _OutlineWidth) break;

                    if (SampleAlpha(i.uv, float2( 1,  0), r, ts)) return _OutlineColor;
                    if (SampleAlpha(i.uv, float2(-1,  0), r, ts)) return _OutlineColor;
                    if (SampleAlpha(i.uv, float2( 0,  1), r, ts)) return _OutlineColor;
                    if (SampleAlpha(i.uv, float2( 0, -1), r, ts)) return _OutlineColor;
                    if (SampleAlpha(i.uv, float2( 1,  1), r, ts)) return _OutlineColor;
                    if (SampleAlpha(i.uv, float2(-1,  1), r, ts)) return _OutlineColor;
                    if (SampleAlpha(i.uv, float2( 1, -1), r, ts)) return _OutlineColor;
                    if (SampleAlpha(i.uv, float2(-1, -1), r, ts)) return _OutlineColor;
                }

                return fixed4(0, 0, 0, 0);
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}