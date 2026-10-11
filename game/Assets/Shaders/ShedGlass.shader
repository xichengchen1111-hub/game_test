// Lit, lightly frosted train-shed glazing: readable panes by day, dim at night, never self-lit.
Shader "WrongDate/ShedGlass" {
 Properties {
  _Tint ("Glass tint (alpha = base opacity)", Color) = (0.62,0.71,0.70,0.20)
  _Grime ("Soot and rain streaks", Range(0,1)) = 0.4
 }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Blend SrcAlpha OneMinusSrcAlpha
  ZWrite Off Cull Off
  Pass {
   Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
    half4 _Tint;
    half _Grime;
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
   struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; half fog:TEXCOORD2; };
   Varyings vert(Attributes v) {
    Varyings o;
    o.positionWS=TransformObjectToWorld(v.positionOS.xyz);
    o.positionCS=TransformWorldToHClip(o.positionWS);
    o.normalWS=TransformObjectToWorldNormal(v.normalOS);
    o.fog=ComputeFogFactor(o.positionCS.z);
    return o;
   }
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float vnoise(float2 p){
    float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
   }
   half4 frag(Varyings i):SV_Target {
    half3 n=normalize(i.normalWS);
    half3 view=GetWorldSpaceNormalizeViewDir(i.positionWS);
    if(dot(n,view)<0)n=-n;
    half fres=pow(1-saturate(dot(n,view)),4);
    Light light=GetMainLight();
    half3 ambient=SampleSH(n);
    half3 lit=_Tint.rgb*(ambient+light.color*saturate(dot(n,light.direction))*.45);
    half3 sky=SampleSH(reflect(-view,n));
    // World-anchored soot blooms and streaks running down the vault (x along the shed).
    half grime=vnoise(i.positionWS.xz*.55)*.6+vnoise(float2(i.positionWS.x*4.1,i.positionWS.z*.35)+7)*.4;
    half3 colour=lerp(lit,sky*1.05,fres*.65)*(1-_Grime*grime*.35);
    half alpha=saturate(_Tint.a*(.75+grime*_Grime*.8)+fres*.30);
    colour=MixFog(colour,i.fog);
    return half4(colour,alpha);
   }
   ENDHLSL
  }
 }
}
