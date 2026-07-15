// シンプルな三色グラデーションのSkybox。
// 内蔵のProcedural Skyboxは大気散乱で地平線が勝手に暖色へ転ぶため、
// テーマから色を完全制御できるようにこれを使う。
Shader "Rail/SkyboxGradient"
{
    Properties
    {
        _TopColor("Top Color", Color) = (0.36, 0.58, 0.88, 1)
        _HorizonColor("Horizon Color", Color) = (0.85, 0.90, 0.96, 1)
        _BottomColor("Bottom Color", Color) = (0.42, 0.46, 0.44, 1)
        _HorizonSharpness("Horizon Sharpness", Range(0.5, 8)) = 2.5
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            half4 _TopColor;
            half4 _HorizonColor;
            half4 _BottomColor;
            half _HorizonSharpness;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 viewDir : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.viewDir = v.vertex.xyz;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float y = normalize(i.viewDir).y;

                // 上半分: 地平線色→天頂色 / 下半分: 地平線色→地面色
                float up = pow(saturate(y), 1.0 / _HorizonSharpness);
                float down = pow(saturate(-y), 0.6);

                half4 sky = lerp(_HorizonColor, _TopColor, up);
                half4 result = lerp(sky, _BottomColor, down);
                return result;
            }
            ENDCG
        }
    }

    Fallback Off
}
