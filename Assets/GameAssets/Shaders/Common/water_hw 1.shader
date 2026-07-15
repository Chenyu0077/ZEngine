Shader "Game/SpriteXRay"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PlayerPos ("Player Pos", Vector) = (0,0,0,0)
        _Radius ("Radius", Float) = 1
        _Softness ("Softness", Float) = 0.3
        _TransparentAlpha ("Transparent Alpha", Range(0,1)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;

            float4 _PlayerPos;
            float _Radius;
            float _Softness;
            float _TransparentAlpha;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                float d = distance(i.worldPos.xy, _PlayerPos.xy);

                float fade = smoothstep(
                    _Radius,
                    _Radius - _Softness,
                    d);

                col.a *= lerp(_TransparentAlpha, 1.0, fade);

                return col;
            }

            ENDHLSL
        }
    }
}