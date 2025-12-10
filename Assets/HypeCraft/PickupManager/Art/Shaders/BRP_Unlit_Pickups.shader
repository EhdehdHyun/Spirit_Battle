Shader "Custom/BRP_Unlit_Pickups"
{
    Properties
    {
        [NoScaleOffset]_MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Shades ("Shades", Float) = -0.2
        _Min ("Min", Float) = 1.3
        _Max ("Max", Float) = 1.5
        [Toggle]_Emission ("Emission", Float) = 0
        [HDR]_EmissionColor ("Emission Color", Color) = (1,1,0,1)
        _Emission_Power ("Emission Power", Float) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off
        ZWrite On
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Shades;
            float _Min;
            float _Max;
            float _Emission;
            float4 _EmissionColor;
            float _Emission_Power;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = normalize(mul(v.normal, (float3x3)unity_WorldToObject));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed3 n = normalize(i.normal);

                // Simple "toon" shade banding using the normal
                fixed intensity = dot(n, float3(0,0,-1));
                intensity = (intensity * 0.5 + 0.5) / _Shades;
                intensity = round(intensity) * (_Max - _Min) + _Min;

                fixed4 tex = tex2D(_MainTex, i.uv) * _Color;
                fixed3 col = tex.rgb * intensity;

                // Emission is additive and uses a separate color
                if (_Emission != 0)
                    col += _EmissionColor.rgb * _Emission_Power;

                return fixed4(col, 1);
            }
            ENDCG
        }
    }

    FallBack Off
}
