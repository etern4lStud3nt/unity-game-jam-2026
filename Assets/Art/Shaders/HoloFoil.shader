// Holographic foil for UI Images and sprites, in the spirit of a holo trading card.
//
// The card is flat, so the "viewing angle" is faked with a tilt vector: it wobbles on its own (Auto Tilt)
// and a script can add to it through _Tilt (for example from the mouse position, range about -1..1).
// Four layers react to that tilt:
//   1. Prism     - two crossing spectral gradients, warped by noise so they are not ruler straight.
//   2. Streaks   - fine foil lines that modulate the prism brightness.
//   3. Glare     - wide soft light bands sliding across the surface.
//   4. Sparkles  - glitter flakes, each facing its own direction, flashing when the tilt lines up with it.
//
// Opacity fades the image itself so whatever is behind shows through. The shine is added as light on top;
// Shine Through Opacity decides how much of it survives when Opacity is low.
//
// Pattern Size is the length of one pattern unit in the mesh's local units: canvas pixels for UI (about 100),
// world units for a SpriteRenderer (about 1). Pixel Snap > 0 quantises the effect to blocks of that size.
Shader "UI/Holo Foil"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Transparency)]
        _Opacity ("Opacity", Range(0, 1)) = 1
        _ShineThrough ("Shine Through Opacity", Range(0, 1)) = 0.6

        [Header(Pattern space)]
        _PatternSize ("Pattern Size (local units)", Float) = 100
        _PixelSnap ("Pixel Snap (local units, 0 = off)", Float) = 0

        [Header(Tilt)]
        _Tilt ("Tilt (xy, set from script)", Vector) = (0,0,0,0)
        _AutoTilt ("Auto Tilt Amount", Range(0, 2)) = 1
        _AutoSpeed ("Auto Tilt Speed", Float) = 0.8

        [Header(Prism)]
        _PrismStrength ("Prism Light", Range(0, 2)) = 0.7
        _PrismTint ("Prism Tint On Image", Range(0, 1)) = 0.45
        _PrismScale ("Prism Scale", Float) = 1.4
        _PrismAngle ("Prism Angle (degrees)", Float) = 35
        _PrismWarp ("Prism Warp", Range(0, 2)) = 0.5
        _PrismSaturation ("Prism Saturation", Range(0, 1)) = 0.9

        [Header(Streaks)]
        _StreakDensity ("Streak Density", Float) = 28
        _StreakStrength ("Streak Strength", Range(0, 1)) = 0.5

        [Header(Glare)]
        _GlareStrength ("Glare Strength", Range(0, 2)) = 0.6
        _GlareScale ("Glare Scale", Float) = 0.45
        _GlareSharpness ("Glare Sharpness", Range(1, 32)) = 6

        [Header(Sparkles)]
        _SparkleStrength ("Sparkle Strength", Range(0, 3)) = 1.4
        _SparkleDensity ("Sparkle Density", Float) = 9
        _SparkleAmount ("Sparkle Amount", Range(0, 1)) = 0.55
        _SparkleSize ("Sparkle Size", Range(0.02, 0.25)) = 0.11

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha // premultiplied: lets the shine be added on top of a see-through image
        ColorMask [_ColorMask]

        Pass
        {
            Name "HoloFoil"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 localPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            float _Opacity, _ShineThrough;
            float _PatternSize, _PixelSnap;
            float4 _Tilt;
            float _AutoTilt, _AutoSpeed;
            float _PrismStrength, _PrismTint, _PrismScale, _PrismAngle, _PrismWarp, _PrismSaturation;
            float _StreakDensity, _StreakStrength;
            float _GlareStrength, _GlareScale, _GlareSharpness;
            float _SparkleStrength, _SparkleDensity, _SparkleAmount, _SparkleSize;

            static const float TAU = 6.28318530718;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.localPos = v.vertex;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float2 Hash22(float2 p)
            {
                float n = Hash21(p);
                return float2(n, Hash21(p + n + 17.17));
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Smooth spectral colour for a phase; one full rainbow per unit.
            float3 Spectrum(float phase)
            {
                return 0.5 + 0.5 * cos(TAU * (phase + float3(0.0, 0.33, 0.67)));
            }

            // One layer of glitter flakes. Returns brightness in x and a per-flake random in y (for colour).
            float2 Sparkles(float2 p, float density, float2 tilt, float time)
            {
                float2 g = p * density;
                float2 id = floor(g);
                float2 f = frac(g) - 0.5;

                float2 rnd = Hash22(id);
                float2 d = f - (rnd - 0.5) * 0.3;
                float dist = length(d);

                // Round core plus a thin four-point flare.
                float core = smoothstep(_SparkleSize, 0.0, dist);
                float flare = smoothstep(_SparkleSize * 0.2, 0.0, min(abs(d.x), abs(d.y)))
                            * smoothstep(_SparkleSize * 2.6, 0.0, dist);
                float shape = max(core, flare);

                // Each flake faces its own direction and only flashes when the tilt lines up with it.
                float facing = rnd.x * TAU;
                float phase = dot(tilt, float2(cos(facing), sin(facing))) * 7.0 + rnd.y * TAU + time * 0.6;
                float flash = pow(saturate(sin(phase)), 14.0);

                float exists = step(Hash21(id + 31.7), _SparkleAmount);
                return float2(shape * flash * exists, rnd.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 baseColor = (tex2D(_MainTex, i.uv) + _TextureSampleAdd) * i.color;

                // Pattern coordinate in local space, so atlases and sliced sprites do not distort it.
                float2 lp = i.localPos.xy;
                float snap = max(_PixelSnap, 1e-4);
                lp = _PixelSnap > 0.0 ? (floor(lp / snap) + 0.5) * snap : lp;
                float2 p = lp / max(_PatternSize, 1e-4);

                // Fake viewing angle.
                float time = _Time.y * _AutoSpeed;
                float2 tilt = _Tilt.xy + _AutoTilt * float2(sin(time), cos(time * 0.73 + 1.3));

                float rad = radians(_PrismAngle);
                float2 dir = float2(cos(rad), sin(rad));
                float2 across = float2(-dir.y, dir.x);

                // 1. Prism: two crossing spectral gradients, bent by slow noise.
                float warp = (ValueNoise(p * 2.3 + tilt * 0.4) - 0.5) * _PrismWarp;
                float phaseA = dot(p, dir) * _PrismScale + dot(tilt, dir) * 1.3 + warp;
                float phaseB = dot(p, across) * _PrismScale * 0.55 - dot(tilt, across) * 0.9 - warp * 0.7;
                float3 prism = Spectrum(phaseA) * 0.65 + Spectrum(phaseB) * 0.35;
                float prismLuma = dot(prism, float3(0.299, 0.587, 0.114));
                prism = lerp(prismLuma.xxx, prism, _PrismSaturation * 1.4);
                prism = saturate(prism);

                // 2. Streaks: fine foil lines, slightly irregular, shifting with tilt.
                float lineCoord = dot(p, across) * _StreakDensity + ValueNoise(p * 5.0) * 1.5 + dot(tilt, dir) * 2.0;
                float streak = pow(0.5 + 0.5 * sin(lineCoord * TAU), 3.0);
                float foil = lerp(1.0, 0.45 + streak * 1.1, _StreakStrength);

                // 3. Glare: wide bands of light sliding over the surface.
                float glareCoord = dot(p, dir) * _GlareScale - dot(tilt, dir) * 0.9 + warp * 0.25;
                float glare = pow(0.5 + 0.5 * cos(glareCoord * TAU), _GlareSharpness);

                // 4. Sparkles: two flake sizes.
                float2 sparkA = Sparkles(p, _SparkleDensity, tilt, time);
                float2 sparkB = Sparkles(p + 13.7, _SparkleDensity * 2.3, tilt * 1.4, time * 1.3);
                float3 sparkle = sparkA.x * lerp(float3(1, 1, 1), Spectrum(sparkA.y + phaseA), 0.45)
                               + sparkB.x * lerp(float3(1, 1, 1), Spectrum(sparkB.y - phaseB), 0.45) * 0.7;

                // The image itself picks up the prism colour...
                float3 body = baseColor.rgb * lerp(float3(1, 1, 1), prism * 1.7, _PrismTint * foil);

                // ...and the foil throws light on top of it.
                float3 light = prism * foil * _PrismStrength * (0.3 + 0.7 * glare)
                             + lerp(float3(1, 1, 1), prism, 0.35) * glare * _GlareStrength * 0.6
                             + sparkle * _SparkleStrength;

                float bodyAlpha = baseColor.a * _Opacity;
                float lightAlpha = baseColor.a * lerp(_Opacity, 1.0, _ShineThrough);

                float clipFactor = 1.0;
                #ifdef UNITY_UI_CLIP_RECT
                clipFactor = UnityGet2DClipping(i.localPos.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(baseColor.a * clipFactor - 0.001);
                #endif

                // Premultiplied output: body covers the background by its alpha, light is purely added.
                fixed4 result;
                result.rgb = (body * bodyAlpha + light * lightAlpha) * clipFactor;
                result.a = bodyAlpha * clipFactor;
                return result;
            }
            ENDCG
        }
    }
}
