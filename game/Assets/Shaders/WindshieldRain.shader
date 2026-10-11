Shader "WrongDate/WindshieldRain" {
 Properties {
  _Drops ("Droplet sheet", 2D) = "black" {}
  _Amount ("Wetness", Range(0,1)) = 0
  _Clear ("Wiped fraction", Range(0,1)) = 0
  _Pivot ("Wiper pivot in UV", Vector) = (0.23,1.09,0,0)
  _Size ("Glass size metres", Vector) = (1.12,0.38,0,0)
  _Radii ("Wiper radius min/max, half arc", Vector) = (0.05,0.33,0.73,0)
 }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+1" "RenderType"="Transparent" }
  Blend SrcAlpha OneMinusSrcAlpha
  ZWrite Off Cull Off
  Pass {
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_Drops); SAMPLER(sampler_Drops);
   CBUFFER_START(UnityPerMaterial)
    float4 _Drops_ST; half _Amount; half _Clear; float4 _Pivot; float4 _Size; float4 _Radii;
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
   struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
   Varyings vert(Attributes v) { Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.uv=v.uv; return o; }
   half4 frag(Varyings i):SV_Target {
    half4 drop=SAMPLE_TEXTURE2D(_Drops,sampler_Drops,i.uv);
    // Metres from the wiper pivot; angle zero points straight down the glass.
    float2 d=(i.uv-_Pivot.xy)*_Size.xy;
    float r=length(d), a=abs(atan2(d.x,-d.y));
    half inArc=step(_Radii.x,r)*step(r,_Radii.y)*step(a,_Radii.z);
    half wet=_Amount*(1-inArc*_Clear*.88h);
    return half4(drop.rgb,drop.a*wet*.85h);
   }
   ENDHLSL
  }
 }
}
