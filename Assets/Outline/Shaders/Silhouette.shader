Shader "Hidden/Outline/Silhouette"
{
    Properties
    {
        _MainTex("Albedo", 2D) = "" {}
        _Cutoff("Cutoff", Float) = 0.5
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
            float _ColorID;
            sampler2D _MainTex;
            float _Cutoff;

            float2 frag (Varyings input) : SV_Target
            {
                if (tex2D(_MainTex, input.uv).a < _Cutoff) discard;
                return float2(_ObjectID, _ColorID);
            }
            ENDHLSL
        }
    }
}
