Shader "FX/Procedural Portal Effect"
{
    Properties
    {
        [Header(Mesh)]
        _MeshExpand("Mesh Expand", Range(1, 6)) = 1

        [Header(Shape)]
        _Size("Size (X,Y)", Vector) = (0.65, 1.0, 0, 0)
        _Rotation("Rotation", Range(0, 360)) = 0
        _EdgeSoftness("Edge Softness", Range(0.001, 0.25)) = 0.035
        _OuterFrameWidth("Outer Frame Width", Range(0.01, 0.5)) = 0.18
        _InnerFrameOffset("Inner Frame Offset", Range(0, 0.6)) = 0.075
        _InnerFrameWidth("Inner Frame Width", Range(0.001, 0.35)) = 0.05
        _OuterHaloWidth("Outer Halo Width", Range(0.0, 0.75)) = 0.22

        [Header(Colors)]
        _OuterColor("Outer Color", Color) = (0.65, 0.95, 1.0, 1)
        _OuterGlowColor("Outer Glow Color", Color) = (0.45, 0.85, 1.0, 1)
        _InnerFrameColor("Inner Frame Color", Color) = (0.75, 1.0, 1.0, 1)
        _InnerColorA("Inner Color A", Color) = (0.03, 0.12, 0.22, 1)
        _InnerColorB("Inner Color B", Color) = (0.08, 0.55, 1.0, 1)
        _HoleColor("Hole Color", Color) = (0.0, 0.0, 0.0, 1)

        [Header(Intensity)]
        _OuterIntensity("Outer Intensity", Range(0, 10)) = 3.2
        _InnerIntensity("Inner Intensity", Range(0, 10)) = 1.6
        _InnerFrameIntensity("Inner Frame Intensity", Range(0, 10)) = 2.2
        _HaloIntensity("Halo Intensity", Range(0, 10)) = 1.6
        _Opacity("Opacity", Range(0, 1)) = 1.0

        [Header(Swirl)]
        _SwirlStrength("Swirl Strength", Range(0, 8)) = 2.6
        _SwirlSpeed("Swirl Speed", Range(0, 10)) = 1.8
        _NoiseScale("Noise Scale", Range(0.5, 16)) = 3.5
        _NoiseSpeed("Noise Speed", Range(0, 8)) = 0.9
        _BandCount("Band Count", Range(1, 32)) = 10
        _BandSpeed("Band Speed", Range(0, 12)) = 3.0
        _Distortion("Distortion", Range(0, 0.25)) = 0.07
        _CenterFade("Center Fade", Range(0, 6)) = 2.6

        [Header(Edge Animation)]
        _EdgeNoiseScale("Edge Noise Scale", Range(0.5, 30)) = 10
        _EdgeNoiseSpeed("Edge Noise Speed", Range(0, 15)) = 2.2
        _EdgeFlicker("Edge Flicker", Range(0, 2)) = 0.7
        _EdgeWobble("Edge Wobble", Range(0, 0.15)) = 0.035

        [Header(Sparks)]
        _SparkColor("Spark Color", Color) = (0.9, 1.0, 1.0, 1)
        _SparkDensity("Spark Density", Range(1, 256)) = 72
        _SparkSize("Spark Size", Range(0.001, 0.2)) = 0.03
        _SparkSpeed("Spark Speed", Range(0, 12)) = 2.5
        _SparkIntensity("Spark Intensity", Range(0, 10)) = 2.2
        _SparkRingBias("Spark Ring Bias", Range(0, 1)) = 0.7
    }

        SubShader
        {
            Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
            Cull Off
            ZWrite Off
            Lighting Off
            Fog { Mode Off }

            CGINCLUDE
            #include "UnityCG.cginc"

            float _MeshExpand;

            float4 _Size;
            float _Rotation;
            float _EdgeSoftness;
            float _OuterFrameWidth;
            float _InnerFrameOffset;
            float _InnerFrameWidth;
            float _OuterHaloWidth;

            fixed4 _OuterColor;
            fixed4 _OuterGlowColor;
            fixed4 _InnerFrameColor;
            fixed4 _InnerColorA;
            fixed4 _InnerColorB;
            fixed4 _HoleColor;

            float _OuterIntensity;
            float _InnerIntensity;
            float _InnerFrameIntensity;
            float _HaloIntensity;
            float _Opacity;

            float _SwirlStrength;
            float _SwirlSpeed;
            float _NoiseScale;
            float _NoiseSpeed;
            float _BandCount;
            float _BandSpeed;
            float _Distortion;
            float _CenterFade;

            float _EdgeNoiseScale;
            float _EdgeNoiseSpeed;
            float _EdgeFlicker;
            float _EdgeWobble;

            fixed4 _SparkColor;
            float _SparkDensity;
            float _SparkSize;
            float _SparkSpeed;
            float _SparkIntensity;
            float _SparkRingBias;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float2 Rotate2(float2 p, float a)
            {
                float s = sin(a);
                float c = cos(a);
                return float2(c * p.x - s * p.y, s * p.x + c * p.y);
            }

            v2f vert(appdata v)
            {
                v2f o;

                float haloR = 1.0 + _OuterHaloWidth + _EdgeSoftness;
                float maxSize = max(_Size.x, _Size.y);
                float autoExpand = max(1.0, haloR * maxSize);

                float expand = max(_MeshExpand, 1.0) * autoExpand;

                float4 pos = v.vertex;
                pos.xy *= expand;

                o.pos = UnityObjectToClipPos(pos);
                o.uv = v.uv;
                return o;
            }

            float Hash11(float p)
            {
                return frac(sin(p * 127.1) * 43758.5453123);
            }

            float Hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float Noise2(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i + float2(0.0, 0.0));
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm2(float2 p)
            {
                float v = 0.0;
                float a = 0.5;

                for (int i = 0; i < 4; i++)
                {
                    v += Noise2(p) * a;
                    p *= 2.0;
                    a *= 0.5;
                }
                return v;
            }

            float Ring(float r, float center, float width, float softness)
            {
                float halfW = width * 0.5;
                float a = smoothstep(center - halfW - softness, center - halfW, r);
                float b = 1.0 - smoothstep(center + halfW, center + halfW + softness, r);
                return saturate(a * b);
            }

            float SparkField(float a01, float r, float t, float outerR, float haloR)
            {
                float cells = max(_SparkDensity, 1.0);
                float x = a01 * cells + t * _SparkSpeed;
                float id = floor(x);
                float f = frac(x);

                float rndA = Hash11(id + 2.17);
                float rndB = Hash11(id + 7.31);
                float rndC = Hash11(id + 13.9);

                float centerA = rndA;
                float ringMin = outerR - _OuterFrameWidth * lerp(0.15, 0.55, _SparkRingBias);
                float ringMax = haloR - lerp(0.02, 0.08, rndC);
                float centerR = lerp(ringMin, ringMax, rndB);

                float da = abs(f - centerA);
                da = min(da, 1.0 - da);

                float dr = abs(r - centerR);
                float d = sqrt(da * da + dr * dr);

                float s = smoothstep(_SparkSize, 0.0, d);
                float flicker = 0.45 + 0.55 * sin((t + rndB) * 12.0 + rndA * 6.2831853);
                return saturate(s * flicker);
            }

            void EvaluatePortal(float2 uv, out float3 centerCol, out float centerA, out float3 glowCol, out float glowA)
            {
                float t = _Time.y;

                float2 p = uv * 2.0 - 1.0;
                p = float2(p.x / max(_Size.x, 1e-4), p.y / max(_Size.y, 1e-4));

                float rot = radians(_Rotation);
                p = Rotate2(p, rot);

                float edgeN = Fbm2(p * _EdgeNoiseScale + t * _EdgeNoiseSpeed);
                float wobble = (edgeN - 0.5) * _EdgeWobble;

                float r = length(p) + wobble;
                float a = atan2(p.y, p.x);
                float a01 = (a + UNITY_PI) / (2.0 * UNITY_PI);

                float outerR = 1.0;
                float haloR = 1.0 + _OuterHaloWidth;

                float clipMask = 1.0 - smoothstep(haloR, haloR + _EdgeSoftness, r);
                float portalMask = 1.0 - smoothstep(outerR, outerR + _EdgeSoftness, r);

                float outerFrame = smoothstep(outerR - _OuterFrameWidth, outerR, r) * portalMask;
                float innerArea = (1.0 - smoothstep(outerR - _OuterFrameWidth, outerR, r)) * portalMask;

                float innerFrameCenter = outerR - _OuterFrameWidth - _InnerFrameOffset;
                float innerFrame = Ring(r, innerFrameCenter, _InnerFrameWidth, _EdgeSoftness) * portalMask;

                float halo = 1.0 - smoothstep(outerR, haloR, r);
                halo *= clipMask;
                float haloN = Fbm2(p * (_EdgeNoiseScale * 0.6) + t * (_EdgeNoiseSpeed * 0.35));
                halo *= lerp(0.65, 1.15, haloN);

                float n1 = Fbm2(p * _NoiseScale + t * _NoiseSpeed);
                float n2 = Fbm2(p * (_NoiseScale * 1.37) + t * (_NoiseSpeed * 1.15) + 19.31);
                float2 warp = float2(n1, n2) - 0.5;
                float2 q = p + warp * _Distortion;

                float rq = length(q);
                float aq = atan2(q.y, q.x);

                float swirl = (1.0 - saturate(rq)) * _SwirlStrength;
                float swirlTerm = sin(t * _SwirlSpeed + rq * 6.0 + n1 * 2.0);
                aq += swirl * (0.65 + 0.35 * swirlTerm);

                float2 w = float2(cos(aq), sin(aq)) * rq;
                float n = Fbm2(w * _NoiseScale + t * _NoiseSpeed);

                float bands = 0.5 + 0.5 * sin(aq * _BandCount + t * _BandSpeed + n * 6.0);
                float centerFade = pow(saturate(1.0 - rq), max(_CenterFade, 1e-3));

                float3 innerMix = lerp(_InnerColorA.rgb, _InnerColorB.rgb, saturate(n));
                float3 swirlCol = innerMix * (0.35 + 0.85 * bands) * (0.35 + 0.65 * centerFade);

                float3 holeCol = _HoleColor.rgb;
                float3 innerCol = holeCol + swirlCol * _InnerIntensity;

                float flicker = 1.0 + (edgeN - 0.5) * _EdgeFlicker;
                flicker *= 0.88 + 0.12 * sin(t * 9.0 + edgeN * 6.2831853);

                float outerPow = pow(saturate(outerFrame), 0.35);
                float3 outerCol = (_OuterColor.rgb * 0.75 + _OuterGlowColor.rgb * 0.55) * outerPow * (_OuterIntensity * flicker);

                float innerFramePow = pow(saturate(innerFrame), 0.45);
                float3 innerFrameCol = _InnerFrameColor.rgb * innerFramePow * (_InnerFrameIntensity * (0.9 + 0.1 * sin(t * 7.5 + n * 4.0)));

                float spark = SparkField(a01, r, t, outerR, haloR);
                float3 sparkCol = _SparkColor.rgb * spark * _SparkIntensity;

                float3 haloCol = _OuterGlowColor.rgb * pow(saturate(halo), 1.35) * (_HaloIntensity * (0.8 + 0.2 * haloN));

                centerCol = saturate(innerCol);
                centerA = saturate(innerArea) * _Opacity;

                glowCol = saturate(outerCol + innerFrameCol + haloCol + sparkCol);
                glowA = saturate(clipMask) * _Opacity;
            }

            fixed4 frag_center(v2f i) : SV_Target
            {
                float3 c;
                float a;
                float3 g;
                float ga;

                EvaluatePortal(i.uv, c, a, g, ga);

                float2 p = i.uv * 2.0 - 1.0;
                p = float2(p.x / max(_Size.x, 1e-4), p.y / max(_Size.y, 1e-4));
                p = Rotate2(p, radians(_Rotation));
                float edgeN = Fbm2(p * _EdgeNoiseScale + _Time.y * _EdgeNoiseSpeed);
                float wobble = (edgeN - 0.5) * _EdgeWobble;
                float r = length(p) + wobble;
                float haloR = 1.0 + _OuterHaloWidth;
                float clipMask = 1.0 - smoothstep(haloR, haloR + _EdgeSoftness, r);
                clip(clipMask - 0.001);

                return fixed4(c, a);
            }

            fixed4 frag_glow(v2f i) : SV_Target
            {
                float3 c;
                float a;
                float3 g;
                float ga;

                EvaluatePortal(i.uv, c, a, g, ga);

                float2 p = i.uv * 2.0 - 1.0;
                p = float2(p.x / max(_Size.x, 1e-4), p.y / max(_Size.y, 1e-4));
                p = Rotate2(p, radians(_Rotation));
                float edgeN = Fbm2(p * _EdgeNoiseScale + _Time.y * _EdgeNoiseSpeed);
                float wobble = (edgeN - 0.5) * _EdgeWobble;
                float r = length(p) + wobble;
                float haloR = 1.0 + _OuterHaloWidth;
                float clipMask = 1.0 - smoothstep(haloR, haloR + _EdgeSoftness, r);
                clip(clipMask - 0.001);

                return fixed4(g * ga, 1.0);
            }
            ENDCG

            Pass
            {
                Tags { "LightMode" = "Always" }
                Blend SrcAlpha OneMinusSrcAlpha
                CGPROGRAM
                #pragma target 3.0
                #pragma vertex vert
                #pragma fragment frag_center
                ENDCG
            }

            Pass
            {
                Tags { "LightMode" = "Always" }
                Blend One One
                CGPROGRAM
                #pragma target 3.0
                #pragma vertex vert
                #pragma fragment frag_glow
                ENDCG
            }
        }

            FallBack Off
}
