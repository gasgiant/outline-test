Shader "Outline/Silhouette"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
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

            float frag (Varyings input) : SV_Target
            {
                return 1;
            }
            ENDHLSL
        }
    }
}
