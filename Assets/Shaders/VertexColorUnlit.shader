Shader "Vortex/VertexColorUnlit"
{
    Properties
    {
        _Tint ("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            Blend Off
            ZWrite On
            ZTest LEqual
            ColorMask RGBA

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "UnityCG.cginc"

            float4 _Tint;

            struct Attributes
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.vertex);
                output.color = input.color * _Tint;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                return float4(input.color.rgb, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
