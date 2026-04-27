Shader "Vortex/DynamicMoonSurface"
{
    Properties
    {
        _MoonNoiseTex ("Moon Noise", 2D) = "white" {}
        _CraterRayTex ("Crater Ray", 2D) = "white" {}
        _FlatSurfaceTex ("Flat Surface", 2D) = "gray" {}
        _SteepSurfaceTex ("Steep Surface", 2D) = "gray" {}
        _MoonNoiseScale ("Moon Noise Scale", Float) = 0.012
        _FlatSurfaceScale ("Flat Surface Scale", Float) = 0.03
        _SteepSurfaceScale ("Steep Surface Scale", Float) = 0.045
        _TextureBlendStrength ("Texture Blend", Range(0,1)) = 0.28
        _EjectaBrightness ("Ejecta Brightness", Range(0,2)) = 0.14
        _SteepDarkening ("Steep Darkening", Range(0,1)) = 0.16
        _Glossiness ("Smoothness", Range(0,1)) = 0.2
        _Metallic ("Metallic", Range(0,1)) = 0.02
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _MoonNoiseTex;
        sampler2D _CraterRayTex;
        sampler2D _FlatSurfaceTex;
        sampler2D _SteepSurfaceTex;

        half _MoonNoiseScale;
        half _FlatSurfaceScale;
        half _SteepSurfaceScale;
        half _TextureBlendStrength;
        half _EjectaBrightness;
        half _SteepDarkening;
        half _Glossiness;
        half _Metallic;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            float3 localPos;
            float4 dataA;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.localPos = v.vertex.xyz;
            o.dataA = v.texcoord;
        }

        float3 TriplanarWeights(float3 n)
        {
            float3 w = abs(normalize(n));
            w = pow(w, 4.0);
            return w / max(w.x + w.y + w.z, 0.0001);
        }

        float4 SampleTriplanar(sampler2D tex, float3 pos, float3 normal, float scale)
        {
            float3 weights = TriplanarWeights(normal);
            float2 uvX = pos.yz * scale;
            float2 uvY = pos.xz * scale;
            float2 uvZ = pos.xy * scale;
            return tex2D(tex, uvX) * weights.x + tex2D(tex, uvY) * weights.y + tex2D(tex, uvZ) * weights.z;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 worldNormal = normalize(IN.worldNormal);
            float3 samplePos = IN.localPos;
            float3 radial = normalize(IN.localPos);
            // Use a mostly radial sampling normal so triplanar mapping follows the whole body,
            // not the faceted marching-cubes triangles.
            float3 textureNormal = normalize(lerp(radial, worldNormal, 0.15));
            float slope = saturate(1.0 - dot(worldNormal, radial));

            float4 moonNoise = SampleTriplanar(_MoonNoiseTex, samplePos, textureNormal, _MoonNoiseScale);
            float4 flatSurface = SampleTriplanar(_FlatSurfaceTex, samplePos, textureNormal, _FlatSurfaceScale);
            float4 steepSurface = SampleTriplanar(_SteepSurfaceTex, samplePos, textureNormal, _SteepSurfaceScale);
            float4 craterRay = SampleTriplanar(_CraterRayTex, samplePos, textureNormal, max(_MoonNoiseScale * 0.12, 0.0005));

            float ejecta = saturate(IN.dataA.y);
            float biome = saturate(IN.dataA.x * 0.5 + 0.5);
            float height01 = saturate(IN.dataA.w);

            float flatLuma = dot(flatSurface.rgb, float3(0.3333, 0.3333, 0.3333));
            float steepLuma = dot(steepSurface.rgb, float3(0.3333, 0.3333, 0.3333));
            float noiseLuma = dot(moonNoise.rgb, float3(0.3333, 0.3333, 0.3333));

            float mariaMask = saturate((moonNoise.g - 0.42) * 2.4 + (biome - 0.5) * 0.35);
            float highlandMask = saturate((noiseLuma - 0.46) * 2.0 + height01 * 0.25);
            float steepMask = saturate(slope * 1.7 - 0.18);
            float craterMask = smoothstep(0.58, 0.9, ejecta) * saturate(craterRay.r * 1.35);

            float3 mariaColor = float3(0.23, 0.23, 0.24);
            float3 midColor = float3(0.47, 0.47, 0.48);
            float3 highColor = float3(0.78, 0.78, 0.76);

            float3 baseColor = lerp(mariaColor, midColor, highlandMask);
            baseColor = lerp(baseColor, highColor, highlandMask * 0.75);
            baseColor = lerp(baseColor, mariaColor, mariaMask * 0.75);

            float surfacePattern = lerp(flatLuma, steepLuma, steepMask);
            baseColor *= lerp(0.82, 1.18, surfacePattern);
            baseColor *= lerp(0.9, 1.08, noiseLuma);
            baseColor = lerp(baseColor, baseColor * (1.0 - _SteepDarkening), steepMask);
            baseColor += craterMask * _EjectaBrightness * float3(0.65, 0.63, 0.6);
            baseColor *= lerp(0.96, 1.04, height01);

            o.Albedo = saturate(baseColor);
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Occlusion = saturate(0.72 + noiseLuma * 0.2 - steepMask * 0.18 - craterMask * 0.08);
        }
        ENDCG
    }

    FallBack "Standard"
}
