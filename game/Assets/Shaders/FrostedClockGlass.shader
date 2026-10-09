Shader "WrongDate/FrostedClockGlass" {
 Properties {
  _MilkColor ("Opal glass", Color) = (0.94,0.93,0.87,1)
  _Milkiness ("Inside diffusion", Range(0,1)) = .64
  _OutsideMilkiness ("Outside diffusion", Range(0,1)) = .9
  _BlurPixels ("Diffusion radius", Float) = 6
 }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Cull Off ZWrite Off ZTest LEqual
  Pass {
   Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   CBUFFER_START(UnityPerMaterial)
    half4 _MilkColor;
    float _Milkiness, _OutsideMilkiness, _BlurPixels;
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
   struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; };
   Varyings vert(Attributes v) {
    Varyings o;o.positionWS=TransformObjectToWorld(v.positionOS.xyz);
    o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(v.normalOS);return o;
   }
   half4 frag(Varyings i):SV_Target {
    float2 uv=GetNormalizedScreenSpaceUV(i.positionCS);
    float2 d=_BlurPixels/_ScaledScreenParams.xy;
    half3 scene=SampleSceneColor(uv)*.2h;
    scene+=(SampleSceneColor(uv+float2(d.x,0))+SampleSceneColor(uv-float2(d.x,0))+
            SampleSceneColor(uv+float2(0,d.y))+SampleSceneColor(uv-float2(0,d.y)))*.1h;
    scene+=(SampleSceneColor(uv+d*.707)+SampleSceneColor(uv-d*.707)+
            SampleSceneColor(uv+float2(d.x,-d.y)*.707)+SampleSceneColor(uv+float2(-d.x,d.y)*.707))*.1h;
    bool inside=dot(normalize(i.normalWS),GetWorldSpaceNormalizeViewDir(i.positionWS))<0;
    float diffusion=inside?_Milkiness:_OutsideMilkiness;
    // Nearby real hands cast a legible soft silhouette through the opal pane.
    // Distant street geometry remains diffuse. Both samples are live URP buffers.
    if(inside) {
     float behind=LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams)+TransformWorldToView(i.positionWS).z;
     float nearby=saturate(1-max(0,behind)/.18);
     diffusion=lerp(diffusion,.25,nearby);
    }
    float grain=frac(sin(dot(floor(i.positionWS.xy*350)+floor(i.positionWS.zy*350),float2(12.9898,78.233)))*43758.5453)-.5;
    return half4(lerp(scene,_MilkColor.rgb,diffusion)+grain*.012,1);
   }
   ENDHLSL
  }
 }
}
