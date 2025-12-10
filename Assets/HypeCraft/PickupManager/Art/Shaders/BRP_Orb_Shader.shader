Shader "Custom/BRP_Orb_Shader"
{
    Properties
    {
        // Base appearance
        _MainColor       ("Main Color", Color) = (0.2, 0.6, 1, 1)
        [NoScaleOffset] _TwirlTex ("Twirl Texture", 2D) = "white" {}

        // Twirl motion controls
        _TwirlSpeed      ("Twirl Speed", Range(0, 10)) = 1.0
        _TwirlStrength   ("Twirl Strength", Range(0, 10)) = 6.0
        _RotateDir       ("Rotation Direction", Range(-1,1)) = 1.0

        // Emission (glow)
        _EmissionColor   ("Emission Color", Color) = (1, 0.8, 0.5, 1)
        [PowerSlider(2.0)] _EmissionStrength ("Emission Strength", Range(0, 10)) = 2.0

        // Fresnel rim effect
        [PowerSlider(3.0)] _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 3.0
        _FresnelBias     ("Fresnel Bias", Range(0, 1)) = 0.0

        // Opacity fade
        [PowerSlider(2.0)] _EdgeOpacity   ("Edge Opacity", Range(0, 1)) = 1.0
        [PowerSlider(2.0)] _CenterOpacity ("Center Opacity", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Vertex input
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
            };

            // Vertex to fragment data
            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float3 normal   : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            // Uniforms
            sampler2D _TwirlTex;
            float4 _MainColor;
            float4 _EmissionColor;
            float _EmissionStrength;
            float _TwirlSpeed;
            float _TwirlStrength;
            float _RotateDir;
            float _FresnelPower;
            float _FresnelBias;
            float _EdgeOpacity;
            float _CenterOpacity;

            // Vertex program
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            // Fragment program
            float4 frag (v2f i) : SV_Target
            {
                // --- Twirl UVs around the center over time ---
                float2 center = float2(0.5, 0.5);
                float2 d = i.uv - center;
                float radius = length(d);
                float angle = atan2(d.y, d.x);
                float spin = _RotateDir * _Time.y * _TwirlSpeed;
                float twistAngle = angle + spin * _TwirlStrength;
                float2 twistedUV = center + float2(cos(twistAngle), sin(twistAngle)) * radius;
                float4 texCol = tex2D(_TwirlTex, twistedUV);

                // --- Fresnel rim mask ---
                float3 n = normalize(i.normal);
                float3 v = normalize(_WorldSpaceCameraPos - i.worldPos);
                float fresnel = pow(1.0 - saturate(dot(n, v)), _FresnelPower) + _FresnelBias;
                float edgeMask = saturate((fresnel - _FresnelBias) / max(1e-5, (1.0 - _FresnelBias)));

                // --- Base color and emission ---
                float3 baseColor = _MainColor.rgb * texCol.rgb;
                float3 emission = _EmissionColor.rgb * texCol.rgb * _EmissionStrength * edgeMask;

                // --- Opacity fade ---
                float alpha = lerp(_CenterOpacity, _EdgeOpacity, edgeMask);

                return float4(baseColor + emission, alpha);
            }
            ENDCG
        }
    }

    FallBack "Unlit/Color"
}
