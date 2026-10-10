Shader "WrongDate/RailWorldText" {
 Properties { _MainTex("Font atlas",2D)="white" {} }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  ZWrite Off ZTest LEqual Cull Back Blend SrcAlpha OneMinusSrcAlpha
  Pass {
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
   struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
   struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
   V vert(A input){V o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);o.uv=input.uv;o.color=input.color;return o;}
   half4 frag(V input):SV_Target {half alpha=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv).a;return half4(input.color.rgb,input.color.a*alpha);}
   ENDHLSL
  }
 }
}
