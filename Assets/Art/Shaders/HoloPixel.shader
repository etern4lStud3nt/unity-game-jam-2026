// Pixel art holographic shine for UI images, like a holo trading card but snapped to the game's art pixels.
// A rainbow band sweeps diagonally across the image in a few hard colour steps, a white glint passes now and then,
// and single pixels twinkle. Works on any UI Image (the sprite's alpha is the shape); supports UI masks.
// Lower Opacity to lay it over something as a see-through foil.
Shader "UI/Holo Pixel"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Pixel grid)]
        _ArtHeight ("Art Pixels Per Screen Height", Float) = 270

        [Header(Overlay)]
        _Opacity ("Opacity (glint and twinkles show stronger)", Range(0, 1)) = 1

        [Header(Rainbow)]
        _HoloStrength ("Rainbow Strength", Range(0, 1)) = 0.75
        _BandSize ("Band Size (art pixels)", Float) = 24
        _Steps ("Colour Steps", Range(2, 12)) = 6
        _Speed ("Sweep Speed", Float) = 0.6
        _Angle ("Sweep Angle (degrees)", Float) = 35
        _Saturation ("Saturation", Range(0, 1)) = 0.65
        _Brightness ("Brightness", Range(0, 2)) = 1.15

        [Header(Glint)]
        _GlintWidth ("Glint Width (art pixels)", Float) = 3
        _GlintEvery ("Glint Every (seconds)", Float) = 2.5

        [Header(Twinkle)]
        _Twinkle ("Twinkle Amount", Range(0, 1)) = 0.06
        _TwinkleSpeed ("Twinkle Speed", Float) = 6

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
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float _ArtHeight;
            float _Opacity;
            float _HoloStrength, _BandSize, _Steps, _Speed, _Angle, _Saturation, _Brightness;
            float _GlintWidth, _GlintEvery;
            float _Twinkle, _TwinkleSpeed;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            float3 HueToRgb(float h)
            {
                float3 rgb = saturate(abs(frac(h + float3(0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0) - 1.0);
                return rgb;
            }

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 baseColor = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;

                // Art pixel this fragment belongs to (resolution independent).
                float artPixel = _ScreenParams.y / max(_ArtHeight, 1.0);
                float2 cell = floor(i.vertex.xy / artPixel);

                // Diagonal coordinate along the sweep, in art pixels.
                float rad = radians(_Angle);
                float along = cell.x * cos(rad) + cell.y * sin(rad);

                // Rainbow in a few hard steps, sliding over time.
                float hue = frac(along / max(_BandSize, 1.0) - _Time.y * _Speed * 0.25);
                hue = floor(hue * _Steps) / _Steps;
                float3 rainbow = lerp(float3(1, 1, 1), HueToRgb(hue), _Saturation) * _Brightness;

                // White glint: crosses the whole screen in the first 40% of each period, then waits.
                float k = frac(_Time.y / max(_GlintEvery, 0.1));
                float glintPos = lerp(-60.0, 620.0, saturate(k / 0.4));
                float glint = step(k, 0.4) * step(abs(along - glintPos), _GlintWidth);

                // Single pixels twinkling.
                float tick = floor(_Time.y * _TwinkleSpeed);
                float twinkle = step(1.0 - _Twinkle, Hash(cell + tick));

                float shine = saturate(glint * 0.85 + twinkle * 0.9);
                float3 holo = lerp(baseColor.rgb, baseColor.rgb * rainbow * 1.3, _HoloStrength);
                holo = lerp(holo, float3(1, 1, 1), shine);

                // See-through overlay: the rainbow lets what is underneath show; glint and twinkles shine stronger.
                fixed4 color = fixed4(holo, baseColor.a * lerp(_Opacity, 1.0, shine));

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
