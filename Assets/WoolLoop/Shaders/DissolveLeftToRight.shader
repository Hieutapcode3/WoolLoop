Shader "WoolLoop/Shader Graph/Burn Dissolve"
{
    Properties
    {
        [MainColor]_BaseColor ("Base Color", Color) = (1,1,1,1)
        [Toggle]_UseDissolve ("Use Dissolve", Float) = 1
        _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0
        _GuideTexture ("Guide Texture", 2D) = "white" {}

        [Toggle]_UseTriplanarUVs ("Use Triplanar UVs", Float) = 0
        _TriplanarSpace ("Triplanar Space", Float) = 0
        _GuideTiling ("Guide Tiling", Vector) = (1,1,1,0)
        _GuideStrength ("Guide Strength", Range(0,2)) = 0
        _ScrollSpeed ("Scroll Speed", Vector) = (0,0,0,0)
        _BackColor ("Back Color", Color) = (0,0,0,1)
        _Axis ("Axis", Float) = 0
        _InvertDirection ("Invert Direction (Min & Max)", Float) = 0
        _MinValue ("Min Value", Range(0,1)) = 0
        _MaxValue ("Max Value", Range(0,1)) = 1

        [HDR]_EmberColor ("Ember Color", Color) = (1,0.6,0.1,1)
        _EmberHardness ("Ember Hardness", Range(0.1,10)) = 2
        _EmberWidth ("Ember Width", Range(0,1)) = 0.2
        [HDR]_BurnColor ("Burn Color", Color) = (1,0.35,0.05,1)
        _BurnHardness ("Burn Hardness", Range(0.1,10)) = 2
        _BurnWidth ("Burn Width", Range(0,1)) = 0.15
        _BurnOffset ("Burn Offset", Range(-1,1)) = 0

        [Toggle]_UseVertexDisplacement ("Use Vertex Displacement", Float) = 0
        _Strength ("Strength", Range(0,1)) = 0.1
        _Smoothness ("Smoothness", Range(0,10)) = 2
        _Offset ("Offset", Range(-1,1)) = 0
        _Width ("Width", Range(0,2)) = 1
        _RotationImpact ("Rotation Impact", Vector) = (0,1,0,0)
        _RotationVector ("Rotation Vector", Vector) = (0,0,0,0)
        _RotationVectorLerp ("Rotation Vector Lerp", Vector) = (0,0,0,0)
        _LerpTexture ("Lerp Texture", 2D) = "white" {}
        _LerpTiling ("Lerp Tiling", Vector) = (1,1,1,0)
        _LerpScrollSpeed ("Lerp Scroll Speed", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Cull Back
        ZWrite On
        AlphaToMask On

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_GuideTexture); SAMPLER(sampler_GuideTexture);
            TEXTURE2D(_LerpTexture); SAMPLER(sampler_LerpTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _UseDissolve;
                float _DissolveAmount;
                float4 _GuideTexture_ST;
                float _UseTriplanarUVs;
                float _TriplanarSpace;
                float4 _GuideTiling;
                float _GuideStrength;
                float4 _ScrollSpeed;
                float4 _BackColor;
                float _Axis;
                float _InvertDirection;
                float _MinValue;
                float _MaxValue;
                float4 _EmberColor;
                float _EmberHardness;
                float _EmberWidth;
                float4 _BurnColor;
                float _BurnHardness;
                float _BurnWidth;
                float _BurnOffset;
                float _UseVertexDisplacement;
                float _Strength;
                float _Smoothness;
                float _Offset;
                float _Width;
                float4 _RotationImpact;
                float4 _RotationVector;
                float4 _RotationVectorLerp;
                float4 _LerpTexture_ST;
                float4 _LerpTiling;
                float4 _LerpScrollSpeed;
            CBUFFER_END

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionHCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; half fog:TEXCOORD3; };

            Varyings vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(input.normalOS);
                o.positionHCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = n.normalWS;
                o.uv = input.uv;
                o.fog = ComputeFogFactor(pos.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                if (_UseDissolve < 0.5)
                {
                    half3 n = normalize(i.normalWS);
                    half ndotl = saturate(dot(n, GetMainLight().direction));
                    half3 lit = _BaseColor.rgb * (0.25h + ndotl);
                    lit = MixFog(lit, i.fog);
                    return half4(lit, 1);
                }

                float axisValue = i.uv.x;
                if (_Axis < 0.5) axisValue = i.uv.x;
                else if (_Axis < 1.5) axisValue = i.uv.y;
                else axisValue = saturate(i.positionWS.y);

                float dissolve = saturate(_DissolveAmount);
                if (_InvertDirection > 0.5)
                    dissolve = 1.0 - dissolve;

                float threshold = lerp(1.0, 0.0, dissolve);
                float guide = SAMPLE_TEXTURE2D(_GuideTexture, sampler_GuideTexture, i.uv * _GuideTiling.xy + _ScrollSpeed.xy * _Time.y).r * _GuideStrength;
                threshold += guide;

                clip(threshold - axisValue);

                half3 n = normalize(i.normalWS);
                half3 l = GetMainLight().direction;
                half ndotl = saturate(dot(n, l));
                half3 lit = _BaseColor.rgb * (0.25h + ndotl);

                float burnCenter = saturate(abs(axisValue - threshold - _BurnOffset));
                float ember = exp(-burnCenter * _EmberHardness * 8.0) * smoothstep(_EmberWidth, 0, burnCenter);
                float burn = exp(-burnCenter * _BurnHardness * 10.0) * smoothstep(_BurnWidth, 0, burnCenter);

                half3 col = lit;
                col += _BurnColor.rgb * burn;
                col += _EmberColor.rgb * ember;
                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor; float _UseDissolve; float _DissolveAmount; float4 _GuideTexture_ST; float _UseTriplanarUVs; float _TriplanarSpace; float4 _GuideTiling; float _GuideStrength; float4 _ScrollSpeed; float4 _BackColor; float _Axis; float _InvertDirection; float _MinValue; float _MaxValue; float4 _EmberColor; float _EmberHardness; float _EmberWidth; float4 _BurnColor; float _BurnHardness; float _BurnWidth; float _BurnOffset; float _UseVertexDisplacement; float _Strength; float _Smoothness; float _Offset; float _Width; float4 _RotationImpact; float4 _RotationVector; float4 _RotationVectorLerp; float4 _LerpTexture_ST; float4 _LerpTiling; float4 _LerpScrollSpeed;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionHCS:SV_POSITION; float2 uv:TEXCOORD0; };
            Varyings vert(Attributes i){ Varyings o; o.positionHCS = TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; return o; }
            half4 frag(Varyings i):SV_Target
            {
                if (_UseDissolve < 0.5) return 0;
                float axisValue = i.uv.x;
                if (_Axis < 0.5) axisValue = i.uv.x;
                else if (_Axis < 1.5) axisValue = i.uv.y;
                else axisValue = 0;
                float dissolve = saturate(_DissolveAmount);
                if (_InvertDirection > 0.5) dissolve = 1.0 - dissolve;
                float threshold = lerp(1.0, 0.0, dissolve);
                clip(threshold - axisValue);
                return 0;
            }
            ENDHLSL
        }
    }
}
