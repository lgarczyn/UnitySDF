Shader "Sprites/SDFDisplay" {
    Properties {
        [Header(States)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("SrcBlend", Float) = 5 // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("DestBlend", Float) = 10 // OneMinusSrcAlpha

        [Header(Textures)]
        _MainTex ("SDF Texture", 2D) = "white" {}
        _BackgroundTex("Texture", 2D) = "white" {}

        [Header(Colors)]
        [HDR]_FaceColor("Face Color", Color) = (1,1,1,1)
        _FaceDilate("Face Dilate", Range(-1,1)) = 0

        [Header(Emboss)]
        [Toggle(EMBOSS_ON)] _EnableEmboss("Enable Emboss", Float) = 0
        [HDR]_EmbossLight("Emboss Light", Color) = (1,1,1,1)
        [HDR]_EmbossDark("Emboss Dark", Color) = (0,0,0,1)
        _EmbossWidth("Emboss Thickness", Range(0,1)) = 0.5
        _EmbossSoftness("Emboss Softness", Range(0,1)) = 1.0

        [Header(Outline)]
        [Toggle(OUTLINE_ON)] _EnableOutline("Enable Outline", Float) = 0
        [HDR]_OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth("Outline Thickness", Range(0,1)) = 0
        _OutlineSoftness("Outline Softness", Range(0,1)) = 0

        [Header(Underlay)]
        [KeywordEnum(Off, On, Bevel)] UNDERLAY("Underlay", Float) = 0
        [HDR]_UnderlayColor("Border Color", Color) = (0,0,0,.5)
        _UnderlayOffsetX("Border OffsetX", Range(-10,10)) = 0
        _UnderlayOffsetY("Border OffsetY", Range(-10,10)) = 0
        _UnderlayDilate("Border Dilate", Range(-1,1)) = 0
        _UnderlaySoftness("Border Softness", Range(0,1)) = 0

        [Header(Loop)]
        [KeywordEnum(Off, On)] LOOP("Loop", Float) = 0
        _LoopSpeed("Loop Speed Y", Float) = 1
        _Margin("Margin", Float) = 0
        _AspectRatio("AspectRatio", Float) = 0
        _Samples("Samples", Int) = 1

        [Header(Texturing)]
        [KeywordEnum(Off, Face, All)] TEXTURING("Texturing", Float) = 0

        [Toggle(UNIFORM_GRADIENT)] _UniformGradient("Force Uniform Gradient", Float) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
      
        ZTest[unity_GUIZTestMode]
        ColorMask [_ColorMask]
        Blend[_SrcBlend][_DstBlend]

        Pass {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

			      #include "UnityCG.cginc"
			      #include "UnityUI.cginc"

            #pragma shader_feature __ OUTLINE_ON
            #pragma shader_feature __ EMBOSS_ON
            #pragma shader_feature UNDERLAY_OFF UNDERLAY_ON UNDERLAY_BEVEL
            #pragma shader_feature TEXTURING_OFF TEXTURING_FACE TEXTURING_ALL
            #pragma shader_feature LOOP_OFF LOOP_ON
            #pragma shader_feature __ UNIFORM_GRADIENT
            #pragma multi_compile __ UNITY_UI_CLIP_RECT

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
#if UNITY_UI_CLIP_RECT
                half2 worldPosition : TEXCOORD1;
#endif
                float2 suv: TEXCOORD2;
            };


CBUFFER_START(UnityPerMaterial)
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            sampler2D _BackgroundTex;

            half4 _FaceColor;
            half _FaceDilate;

//#if UNITY_UI_CLIP_RECT
            uniform float4		_ClipRect;	// bottom left(x,y) : top right(z,w)
//#endif

//#if defined(EMBOSS_ON)
            half4 _EmbossLight, _EmbossDark;
            half _EmbossSoftness;
            half _EmbossWidth;
//#endif
//#if defined(OUTLINE_ON)
            half4 _OutlineColor;
            half _OutlineSoftness;
            half _OutlineWidth;
//#endif
//#if !defined(UNDERLAY_OFF)
            half4 _UnderlayColor;
            half _UnderlayDilate;
            half _UnderlaySoftness;
//#endif
//#if defined(EMBOSS_ON) || !defined(UNDERLAY_OFF)
            half _UnderlayOffsetX;
            half _UnderlayOffsetY;
//#endif
//#if defined(LOOP_ON)
            half _LoopSpeed;
            half _Margin;
            half _AspectRatio;
            int _Samples;
//#endif
CBUFFER_END

#if defined(EMBOSS_ON) || !defined(UNDERLAY_OFF)
            half ComputeLit(half d) {
                bool flipY = false;
#   if defined(UNITY_UV_STARTS_AT_TOP)
                flipY = UNITY_UV_STARTS_AT_TOP < 0.5;
#   endif
                half3 normal = half3(normalize(half2(ddx(d), ddy(d))) * 0.8, 0.6);
                half3 lightDir = normalize(half3(_UnderlayOffsetX, _UnderlayOffsetY * (flipY ? -1 : 1), 10));
                //half bevelL = (dot(normal, offset) + fringe * 0.001) / dot(offset, offset);
                return saturate(dot(lightDir, normal));
            }
#endif

            v2f vert (appdata v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.suv = v.uv;
#if UNITY_UI_CLIP_RECT
                o.worldPosition = v.vertex.xy;
#endif
                return o;
            }

            half4 looped(sampler2D tex, float2 uv)
            {
#if defined(LOOP_ON)
                float f = min(uv.y, _AspectRatio - uv.y);
                f = 1 - saturate(f * _Margin + 0.5 + (1 - _Margin) / 4);
                uv.y -= _Time.y * _LoopSpeed;
                half4 c = 0;
                for (int j = 0; j < _Samples; j++) {
                    c = max(c, tex2D(tex, uv));
                    // c *= 0.8;
                    uv.y += 1. / _Samples;
                }
                c -= f;
                return c;
#else
                return tex2D(tex, uv);
#endif
            }

            half4 frag(v2f i) : SV_Target{
                half bias = 0.5 - _FaceDilate / 2;

                // Compute density value
                half d = looped(_MainTex, i.uv).a;

                half fringe = max(fwidth(d), 0.0001);
#if defined(UNIFORM_GRADIENT)
                half TargetFringe = 0.15;
                d = (d - 0.5) * TargetFringe / fringe + 0.5;
                fringe = TargetFringe;
#endif

                half4 faceColor = _FaceColor;
                faceColor.rgb *= i.color.rgb;


#if defined(TEXTURING_FACE) || defined(TEXTURING_ALL)
                half4 tex = looped(_BackgroundTex, i.uv);
                faceColor *= tex;
#endif

#if defined(EMBOSS_ON)
                {
                    half lit = ComputeLit(d);
                    half eb_from = max(0, bias + _EmbossWidth - _EmbossSoftness / 2);
                    half eb_to = min(1, bias + _EmbossWidth + _EmbossSoftness / 2);
                    half amnt = 1 - saturate((d - eb_from) / max(0.0001, eb_to - eb_from));
                    faceColor.rgb = lerp(faceColor.rgb, lerp(_EmbossDark, _EmbossLight, lit), amnt);
                }
#endif

#if defined(OUTLINE_ON)
                half4 outlineColor = _OutlineColor;
                outlineColor.rgb *= outlineColor.a;
                //outlineColor.rgb *= outlineColor.a;
#endif

                // Compute result color
                half4 c = faceColor * saturate((d - bias) / fringe + 0.5);

                // Append outline
#ifdef OUTLINE_ON
                if (_OutlineWidth > 0) {
                    half outlineFade = max(_OutlineSoftness, fringe * 2);
                    half halfWidth = (_OutlineWidth / 2);
                    half ol_from = min(1, bias + halfWidth + outlineFade / 2);
                    half ol_to = max(0, bias - halfWidth - outlineFade / 2);
                    c = lerp(faceColor, outlineColor, saturate((ol_from - d) / outlineFade));
                    c *= saturate((d - ol_to) / outlineFade);
                }
#endif

                // Append underlay (drop shadow)
#if !defined(UNDERLAY_OFF)
                {
                    half ul_from = max(0, bias - _UnderlayDilate - _UnderlaySoftness / 2);
                    half ul_to = min(1, bias - _UnderlayDilate + _UnderlaySoftness / 2);
                    bool flipY = false;
#if defined(UNITY_UV_STARTS_AT_TOP)
                    flipY = UNITY_UV_STARTS_AT_TOP < 0.5;
#endif
                    if (UNITY_MATRIX_P._m11 < 0) flipY = !flipY;
#if defined(UNDERLAY_ON)
                    half2 offset =
                        ddx(i.uv) * _UnderlayOffsetX
                        + ddy(i.uv) * ((flipY ? -1 : 1) * _UnderlayOffsetY);

                    float2 underlayUV = i.uv - offset;
                    half old = looped(_MainTex, underlayUV).a;
                    c += _UnderlayColor * _UnderlayColor.a * (1 - c.a) *
                        saturate((old - ul_from) / max(0.0001, ul_to - ul_from));
#elif defined(UNDERLAY_BEVEL)
                    half4 underlayColor = _UnderlayColor;
                    half bevelL = ComputeLit(d);
                    underlayColor.rgb *= bevelL;
                    underlayColor.a += (1 - underlayColor.a) * (1 - bevelL);
                    c += underlayColor * (1 - c.a) *
                        saturate((d - ul_from) / max(0.0001, ul_to - ul_from));
#endif
                }
#endif
                c.a *= i.color.a;
                // Un pre-multiply alpha to avoid color issues on borders
                if (c.a > 0.0001)
                  c.rgb /= c.a;

#if UNITY_UI_CLIP_RECT
                c *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
#endif

#ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
#endif

                return c;
            }
            ENDCG
        }
    }
}