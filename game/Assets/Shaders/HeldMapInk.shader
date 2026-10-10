Shader "WrongDate/HeldMapInk" {
    Properties {
        _MainTex ("Paper or ink", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Shade ("Form shading", Range(0,1)) = 0
        _ZWrite ("Depth write", Float) = 1
        _SrcBlend ("Source blend", Float) = 1
        _DstBlend ("Destination blend", Float) = 0
    }
    SubShader {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass {
            Tags { "LightMode"="UniversalForward" }
            Cull Off ZWrite [_ZWrite] Blend [_SrcBlend] [_DstBlend]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST, _Color; float _Shade;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;float3 normalOS:NORMAL;};
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 normal:TEXCOORD1;};
            Varyings Vert(Attributes input) {Varyings o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);o.uv=TRANSFORM_TEX(input.uv,_MainTex);o.normal=TransformObjectToWorldNormal(input.normalOS);return o;}
            half4 Frag(Varyings input):SV_Target {half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv)*_Color;half shape=.56+.44*saturate(dot(normalize(input.normal),normalize(float3(-.4,.8,-.45))));c.rgb*=lerp(1,shape,_Shade);return c;}
            ENDHLSL
        }
    }
}
