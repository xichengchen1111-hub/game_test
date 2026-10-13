Shader "WrongDate/EvidenceStudy" {
 Properties {
  _BaseMap("Surface",2D)="white" {}
  _BaseColor("Colour",Color)=(1,1,1,1)
  _Paper("Paper",Float)=0
  _Metallic("Metal",Range(0,1))=0
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
  Pass {
   Tags {"LightMode"="SRPDefaultUnlit"}
   Cull Off ZWrite On
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST,_BaseColor;
    float _Paper,_Metallic;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
   struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;};
   V Vert(A a){V o;o.positionWS=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(a.normalOS);o.uv=TRANSFORM_TEX(a.uv,_BaseMap);return o;}
   float Noise(float2 p){return frac(sin(dot(p,float2(12.9898,78.233)))*43758.5453);}
   half4 Frag(V i, FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target {
    half4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;clip(tex.a-.35);
    float3 n=normalize(i.normalWS)*IS_FRONT_VFACE(face,1,-1);
    float3 view=normalize(_WorldSpaceCameraPos-i.positionWS);
    // Fixed inspection-only soft key, fill and edge response; no town lights or shadows added.
    float3 key=normalize(float3(-.6,.85,-1));float3 fill=normalize(float3(.8,-.1,-.6));float3 rim=normalize(float3(.3,.6,.8));
    float diffuse=.22+.72*saturate(dot(n,key))+.21*saturate(dot(n,fill));
    float spec=pow(saturate(dot(n,normalize(key+view))),lerp(24,70,_Metallic));
    float edge=pow(1-saturate(dot(n,view)),3)*saturate(dot(n,rim))*.19;
    float grain=lerp(1, .975+Noise(floor(i.uv*1400))*.05,_Paper);
    float3 color=tex.rgb*diffuse*grain+lerp(float3(.7,.73,.77),tex.rgb,_Metallic)*spec*lerp(.035,.34,_Metallic)*(1-_Paper*.85)+edge;
    return half4(color,1);
   }
   ENDHLSL
  }
 }
}
