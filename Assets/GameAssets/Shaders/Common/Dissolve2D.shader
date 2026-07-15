Shader "Game/Dissolve2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _DissolveTex ("Dissolve Texture", 2D) = "white" {}
        _DissolveThreshold ("Dissolve Threshold", Range(0, 1)) = 0
        _DissolveEdgeWidth ("Edge Width", Range(0, 0.3)) = 0.05
        _EdgeColor1 ("Edge Color 1", Color) = (1, 0.3, 0, 1)
        _EdgeColor2 ("Edge Color 2", Color) = (1, 1, 0.3, 1)
        _AlphaCutoff ("Alpha Cutoff", Range(0.001, 1)) = 0.1
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
                float4 color  : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float2 uvDissolve : TEXCOORD1;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _Color;
            sampler2D _DissolveTex;
            float4    _DissolveTex_ST;
            float     _DissolveThreshold;
            float     _DissolveEdgeWidth;
            float4    _EdgeColor1;
            float4    _EdgeColor2;
            float     _AlphaCutoff;

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos       = UnityObjectToClipPos(v.vertex);
                o.uv        = TRANSFORM_TEX(v.uv, _MainTex);
                o.uvDissolve = TRANSFORM_TEX(v.uv, _DissolveTex);
                o.color     = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                if (col.a <= _AlphaCutoff)
                    discard;

                float noise = tex2D(_DissolveTex, i.uvDissolve).r;
                float threshold = _DissolveThreshold;

                if (noise <= threshold)
                    discard;

                float edgeDist = noise - threshold;

                if (edgeDist < _DissolveEdgeWidth)
                {
                    float t = edgeDist / _DissolveEdgeWidth;
                    fixed4 edge = lerp(_EdgeColor1, _EdgeColor2, t);
                    return fixed4(edge.rgb, edge.a * col.a);
                }

                return col * i.color;
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}