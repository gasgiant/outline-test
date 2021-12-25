Shader "Outline/Outline"
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
            float _Width;
            float _Softness;

            float4 frag (Varyings input) : SV_Target
            {
                int2 uvInt = int2(input.positionHCS.xy);

                float2 pos = _MainTex.Load(int3(uvInt, 0)).rg;

                if (pos.x == -1)
                    return 0;

                float distance = length(pos - input.positionHCS.xy + 0.5);

                float alpha = saturate((_Width - distance) * (1 - _Softness)) * (distance > 0);

                return float4(1, 0, 0, alpha);
            }
            ENDHLSL
        }
    }
}