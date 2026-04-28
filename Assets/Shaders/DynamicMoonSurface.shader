Shader "Vortex/DynamicMoonSurface"
{
    Properties
    {
        _MoonNoiseTex ("Moon Noise", 2D) = "white" {}
        _CraterRayTex ("Crater Ray", 2D) = "white" {}
        _FlatSurfaceTex ("Flat Surface", 2D) = "gray" {}
        _SteepSurfaceTex ("Steep Surface", 2D) = "gray" {}
        _FlatNormalTex ("Flat Normal", 2D) = "bump" {}
        _SteepNormalTex ("Steep Normal", 2D) = "bump" {}
        _MoonNoiseScale ("Moon Noise Scale", Float) = 0.012
        _FlatSurfaceScale ("Flat Surface Scale", Float) = 0.03
        _SteepSurfaceScale ("Steep Surface Scale", Float) = 0.045
        _MicroDetailScale ("Micro Detail Scale", Float) = 0.032
        _TextureBlendStrength ("Texture Blend", Range(0,1)) = 0.28
        _NormalBlendStrength ("Normal Blend", Range(0,1)) = 0.52
        _MicroDetailStrength ("Micro Detail", Range(0,1)) = 0.35
        _FlatContrast ("Flat Contrast", Range(0.5,2.5)) = 1.12
        _SteepContrast ("Steep Contrast", Range(0.5,2.5)) = 1.06
        _AlbedoSaturation ("Albedo Saturation", Range(0,2)) = 0.95
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
        sampler2D _FlatNormalTex;
        sampler2D _SteepNormalTex;

        half _MoonNoiseScale;
        half _FlatSurfaceScale;
        half _SteepSurfaceScale;
        half _MicroDetailScale;
        half _TextureBlendStrength;
        half _NormalBlendStrength;
        half _MicroDetailStrength;
        half _FlatContrast;
        half _SteepContrast;
        half _AlbedoSaturation;
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

        float3 TriplanarNormalStrength(sampler2D tex, float3 pos, float3 blendNormal, float scale)
        {
            float3 weights = TriplanarWeights(blendNormal);
            float2 uvX = pos.yz * scale;
            float2 uvY = pos.xz * scale;
            float2 uvZ = pos.xy * scale;

            float3 nx = UnpackNormal(tex2D(tex, uvX));
            float3 ny = UnpackNormal(tex2D(tex, uvY));
            float3 nz = UnpackNormal(tex2D(tex, uvZ));

            // Axis-aligned tangent bases for each planar projection.
            float3 blended = nx * weights.x + ny * weights.y + nz * weights.z;
            // We use normal maps here as a high-frequency texture driver, avoiding
            // tangent-space writes that can break on marching-cubes meshes.
            return abs(normalize(blended));
        }

        float3 ApplySaturation(float3 c, float s)
        {
            float l = dot(c, float3(0.299, 0.587, 0.114));
            return lerp(float3(l, l, l), c, s);
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
            float4 microDetail = SampleTriplanar(_MoonNoiseTex, samplePos, textureNormal, _MicroDetailScale);
            float4 craterRay = SampleTriplanar(_CraterRayTex, samplePos, textureNormal, max(_MoonNoiseScale * 0.12, 0.0005));

            float ejecta = saturate(IN.dataA.y);
            float biome = saturate(IN.dataA.x * 0.5 + 0.5);
            float height01 = saturate(IN.dataA.w);

            float3 flatColorTex = pow(saturate(flatSurface.rgb), _FlatContrast.xxx);
            float3 steepColorTex = pow(saturate(steepSurface.rgb), _SteepContrast.xxx);
            flatColorTex = ApplySaturation(flatColorTex, _AlbedoSaturation);
            steepColorTex = ApplySaturation(steepColorTex, _AlbedoSaturation);

            float flatLuma = dot(flatColorTex, float3(0.3333, 0.3333, 0.3333));
            float steepLuma = dot(steepColorTex, float3(0.3333, 0.3333, 0.3333));
            float noiseLuma = dot(moonNoise.rgb, float3(0.3333, 0.3333, 0.3333));
            float microLuma = dot(microDetail.rgb, float3(0.3333, 0.3333, 0.3333));

            float mariaMask = saturate((moonNoise.g - 0.42) * 2.4 + (biome - 0.5) * 0.35);
            float highlandMask = saturate((noiseLuma - 0.46) * 2.0 + height01 * 0.25);
            float steepMask = smoothstep(0.16, 0.46, slope);
            float craterMask = smoothstep(0.58, 0.9, ejecta) * saturate(craterRay.r * 1.35);

            float3 flatNormalDetail = TriplanarNormalStrength(_FlatNormalTex, samplePos, textureNormal, _FlatSurfaceScale);
            float3 steepNormalDetail = TriplanarNormalStrength(_SteepNormalTex, samplePos, textureNormal, _SteepSurfaceScale);
            float3 normalDetail = lerp(flatNormalDetail, steepNormalDetail, steepMask);
            float detailInfluence = saturate(_NormalBlendStrength) * saturate(0.4 + 0.6 * _TextureBlendStrength);
            float normalLuma = dot(normalDetail, float3(0.3333, 0.3333, 0.3333));

            float3 mariaColor = float3(0.23, 0.23, 0.24);
            float3 midColor = float3(0.47, 0.47, 0.48);
            float3 highColor = float3(0.78, 0.78, 0.76);

            float3 baseColor = lerp(mariaColor, midColor, highlandMask);
            baseColor = lerp(baseColor, highColor, highlandMask * 0.75);
            baseColor = lerp(baseColor, mariaColor, mariaMask * 0.75);

            float3 surfColor = lerp(flatColorTex, steepColorTex, steepMask);
            float flatPattern = lerp(1.0, lerp(0.72, 1.28, flatLuma), _TextureBlendStrength);
            float steepPattern = lerp(1.0, lerp(0.78, 1.18, steepLuma), _TextureBlendStrength * 0.75);
            float surfacePattern = lerp(flatPattern, steepPattern, steepMask);
            baseColor *= surfacePattern;
            baseColor *= lerp(float3(1.0, 1.0, 1.0), surfColor * 1.35, _TextureBlendStrength);
            baseColor *= lerp(1.0, lerp(0.86, 1.14, microLuma), _MicroDetailStrength);
            baseColor *= lerp(1.0, lerp(0.92, 1.08, normalLuma), detailInfluence);
            baseColor *= lerp(0.9, 1.08, noiseLuma);
            baseColor *= lerp(0.96, 1.06, saturate(IN.dataA.z * 0.5 + 0.5));
            baseColor = lerp(baseColor, baseColor * (1.0 - _SteepDarkening), steepMask);
            baseColor += craterMask * _EjectaBrightness * float3(0.65, 0.63, 0.6);
            baseColor *= lerp(0.96, 1.04, height01);
            // Neutralize slight blue cast from source textures for a more lunar grayscale.
            baseColor *= float3(1.02, 1.0, 0.94);

            o.Albedo = saturate(baseColor);
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Occlusion = saturate(0.72 + noiseLuma * 0.2 - steepMask * 0.18 - craterMask * 0.08);
        }
        ENDCG
    }

    FallBack "Standard"
}
