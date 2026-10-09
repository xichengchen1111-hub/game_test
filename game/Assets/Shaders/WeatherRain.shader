Shader "WrongDate/WeatherRain" {
 Properties { _Tint ("Rain light", Color) = (.68,.74,.79,1) }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent" }
  Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
  Pass {
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
    half4 _Tint;
   CBUFFER_END
   struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
   struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;half fog:TEXCOORD1;};
   Varyings vert(Attributes i) {Varyings o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;o.color=i.color;o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   half4 frag(Varyings i):SV_Target {
    half width=saturate(1-abs(i.uv.x*2-1));
    half ends=saturate(min(i.uv.y,1-i.uv.y)*5);
    return half4(MixFog(_Tint.rgb,i.fog),i.color.a*width*ends);
   }
   ENDHLSL
  }
 }
}
