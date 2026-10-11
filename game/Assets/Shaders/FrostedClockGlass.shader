Shader "WrongDate/FrostedClockGlass" {
 Properties {
  _MilkColor ("Opal glass", Color) = (0.94,0.93,0.87,1)
  _Milkiness ("Inside diffusion", Range(0,1)) = .64
  _OutsideMilkiness ("Outside diffusion", Range(0,1)) = .9
  _BlurPixels ("Diffusion radius", Float) = 6
  // Night backlighting from the lamps behind the dial. DayNightLamp scales this
  // colour (per renderer) with the same photocell output as the dial's spill light.
  [HDR] _EmissionColor ("Backlight", Color) = (0,0,0,1)
  _GlowFull ("Full backlight reference", Float) = 1
  _DialRadius ("Dial radius (object units)", Float) = 1.7
  [HideInInspector] _BaseColor ("Unused; read by DayNightLamp", Color) = (1,1,1,1)
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
   #pragma multi_compile_fog
   #pragma shader_feature_local_fragment _EMISSION
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   CBUFFER_START(UnityPerMaterial)
    half4 _MilkColor;
    half4 _EmissionColor;
    float _Milkiness, _OutsideMilkiness, _BlurPixels, _GlowFull, _DialRadius;
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
   struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float3 dial:TEXCOORD2; };
   Varyings vert(Attributes v) {
    Varyings o;o.positionWS=TransformObjectToWorld(v.positionOS.xyz);
    o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(v.normalOS);
    o.dial=float3(v.positionOS.xy/_DialRadius,ComputeFogFactor(o.positionCS.z));return o;
   }
   // Opal glass lit from behind by two lamps low in the chamber: a warm, slightly
   // uneven field, brightest near the centre and dimming toward the bezel.
   half BacklightProfile(float2 p) {
    float r=length(p);
    half field=lerp(1.08,.70,smoothstep(.15,1.,r));
    float2 a=p-float2(-.40,-.30),b=p-float2(.40,-.30);
    field+=.16*exp(-dot(a,a)*5.5)+.16*exp(-dot(b,b)*5.5);
    field*=1-.10*smoothstep(.80,1.,r);
    return field;
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
    float nearby=0;
    if(inside) {
     float behind=LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams)+TransformWorldToView(i.positionWS).z;
     nearby=saturate(1-max(0,behind)/.18);
     diffusion=lerp(diffusion,.25,nearby);
    }
    float grain=frac(sin(dot(floor(i.positionWS.xy*350)+floor(i.positionWS.zy*350),float2(12.9898,78.233)))*43758.5453)-.5;
    half3 milk=_MilkColor.rgb;
    half3 glow=0;
    #if defined(_EMISSION)
     // Unlit opal reads as daylit paper; once the lamps take over it falls to the
     // night ambience and the backlight carries the face.
     half glowAmount=saturate(max(_EmissionColor.r,max(_EmissionColor.g,_EmissionColor.b))/max(_GlowFull,1e-3));
     milk*=lerp(1,inside?.46:.20,glowAmount);
     // From the machine room the hands outside still read as shadows on the lit pane.
     glow=_EmissionColor.rgb*BacklightProfile(i.dial.xy)*(inside?.32*(1-.85*nearby):1)*(1+grain*.10);
    #endif
    half3 colour=lerp(scene,milk,diffusion)+grain*.012;
    // The unlit daytime face keeps its approved legibility. The lamp glow thins in
    // heavy mist, but carries farther than the fogged masonry around it.
    half visibility=ComputeFogIntensity(i.dial.z);
    colour+=glow*lerp(.22,1,pow(visibility,.45));
    return half4(colour,1);
   }
   ENDHLSL
  }
 }
}
