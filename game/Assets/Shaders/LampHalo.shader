Shader "WrongDate/LampHalo" {
 // Mist scattering around a lit lamp or opal clock face. Camera-facing quad,
 // additive, softened where it meets geometry; strength follows the scene fog.
 // DayNightLamp scales _EmissionColor per renderer with its lamp output.
 Properties {
  [HDR] _EmissionColor ("Halo", Color) = (1,.8,.55,1)
  _NearFade ("Fade in from/to camera distance", Vector) = (5,14,0,0)
  _Softness ("Depth softness (m)", Float) = .9
  _Pull ("Pull toward camera (m)", Float) = .6
  [HideInInspector] _BaseColor ("Unused; read by DayNightLamp", Color) = (1,1,1,1)
 }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+20" "RenderType"="Transparent" "IgnoreProjector"="True" }
  Blend One One ZWrite Off ZTest LEqual Cull Off
  Pass {
   Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #pragma shader_feature_local_fragment _EMISSION
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   CBUFFER_START(UnityPerMaterial)
    half4 _EmissionColor;
    float4 _NearFade;
    float _Softness, _Pull;
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
   struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float4 data:TEXCOORD1; };
   Varyings vert(Attributes v) {
    Varyings o;
    float3 centre=TransformObjectToWorld(float3(0,0,0));
    float size=length(float3(UNITY_MATRIX_M._m00,UNITY_MATRIX_M._m10,UNITY_MATRIX_M._m20));
    float3 toCamera=GetCameraPositionWS()-centre;float distance=length(toCamera);
    centre+=toCamera/max(distance,1e-3)*min(_Pull,distance*.5);
    float3 right=UNITY_MATRIX_V[0].xyz,up=UNITY_MATRIX_V[1].xyz;
    float2 corner=v.uv*2-1;
    float3 positionWS=centre+(right*corner.x+up*corner.y)*size*.5;
    o.positionCS=TransformWorldToHClip(positionWS);o.uv=corner;
    float fade=smoothstep(_NearFade.x,_NearFade.y,distance);
    o.data=float4(fade,ComputeFogFactor(o.positionCS.z),0,0);
    return o;
   }
   half4 frag(Varyings i):SV_Target {
    #if !defined(_EMISSION)
     return 0;
    #else
     float r2=dot(i.uv,i.uv);
     if(r2>=1)discard;
     half falloff=exp(-r2*5.0)*.65+exp(-r2*1.6)*.35;
     falloff*=1-smoothstep(.55,1,r2);
     float2 screen=GetNormalizedScreenSpaceUV(i.positionCS);
     float scene=LinearEyeDepth(SampleSceneDepth(screen),_ZBufferParams);
     float own=LinearEyeDepth(i.positionCS.z,_ZBufferParams);
     half soft=saturate((scene-own)/_Softness);
     // More mist between viewer and lamp gives a wider glow; very far away it thins.
     half visibility=ComputeFogIntensity(i.data.y);
     half mist=saturate(1-visibility)*.85+.15;
     return half4(_EmissionColor.rgb*falloff*soft*i.data.x*mist*lerp(.35,1,visibility),0);
    #endif
   }
   ENDHLSL
  }
 }
}
