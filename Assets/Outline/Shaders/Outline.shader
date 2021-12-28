Shader "Hidden/Outline/Outline"
{
    Properties
    {
        _MainTex ("Source", 2D) = "" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #define COLORS_COUNT 32

            struct MeshData
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
            };

            Varyings vert (MeshData input)
            {
                Varyings output;
                output.positionHCS = UnityObjectToClipPos(input.positionOS);
                return output;
            }

            Texture2D _MainTex;
            float4 _MainTex_TexelSize;
            float _OutlinePixelWidth;
            float _OutlineSoftness;
            float4 _OutlineColors[COLORS_COUNT];

            float4 frag (Varyings input) : SV_Target
            {
                int2 uvInt = int2(input.positionHCS.xy);

                float4 data = _MainTex.Load(int3(uvInt, 0));
                float2 pos = data.xy;
                clip(pos.x);

                float distance = length(pos - input.positionHCS.xy + 0.5);

                float alpha = saturate((_OutlinePixelWidth - distance) / (50 * _OutlineSoftness + 0.05)) * (distance > 0);
                float4 c = _OutlineColors[round(data.a * COLORS_COUNT)];
                c.a *= alpha;

                return c;
            }
            ENDHLSL
        }
    }
}
