Shader "Outline/Silhouette"
{
    Properties
    {
        _MainTex("Source", 2D) = "" {}
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
                float4 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float4 uv : TEXCOORD0;
            };

            Varyings vert (MeshData input)
            {
                Varyings output;
                output.positionHCS = UnityObjectToClipPos(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            float _ObjectID;
            sampler2D _MainTex;

            float frag (Varyings input) : SV_Target
            {
                if (tex2D(_MainTex, input.uv).a < 0.5) discard;
                return _ObjectID;
            }
            ENDHLSL
        }
    }
}
