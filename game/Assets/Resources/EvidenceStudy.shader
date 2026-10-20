Shader "WrongDate/EvidenceStudy" {
 Properties {
  _BaseMap("Surface",2D)="white" {}
  _BaseColor("Colour",Color)=(1,1,1,1)
  _Paper("Paper",Float)=0
  _Metallic("Metal",Range(0,1))=0
  _Rough("Roughness",Range(.05,1))=.85
  _Fibre("Paper fibre",Float)=0
  _Laid("Laid lines",Float)=0
  _Weave("Cloth weave",Float)=0
  _Stripes("Page-edge stripes",Float)=0
  _Leather("Leather grain",Float)=0
  _Brushed("Brushed metal",Float)=0
  _Newsprint("Newsprint",Float)=0
  _Translucency("Back light through thin paper",Range(0,1))=0
  _Gloss("Photographic gloss",Range(0,1))=0
  _BumpScale("Relief",Float)=.00115
  _ShowMap("Print seen through the sheet",2D)="white" {}
  _ShowThrough("Show-through",Range(0,1))=0
  _Age("Age and wear",Range(0,1))=0
  _Glass("Glass",Float)=0
  [HideInInspector]_SrcBlend("",Float)=1
  [HideInInspector]_DstBlend("",Float)=0
  [HideInInspector]_ZWriteOn("",Float)=1
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
  Pass {
   Tags {"LightMode"="SRPDefaultUnlit"}
   Cull Off ZWrite [_ZWriteOn] Blend [_SrcBlend] [_DstBlend]
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   TEXTURE2D(_ShowMap); SAMPLER(sampler_ShowMap);
   CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST,_BaseColor;
    float _Paper,_Metallic,_Rough,_Fibre,_Laid,_Weave,_Stripes,_Leather,_Brushed,_Newsprint,_Translucency,_Gloss,_BumpScale,_Age,_ShowThrough,_Glass,_SrcBlend,_DstBlend,_ZWriteOn;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
   struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;};
   V Vert(A a){V o;o.positionWS=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(a.normalOS);o.uv=TRANSFORM_TEX(a.uv,_BaseMap);return o;}
   float Hash(float2 p){return frac(sin(dot(p,float2(12.9898,78.233)))*43758.5453);}
   float Noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+float2(1,1)),f.x),f.y);}
   // Detail fades out before it would alias at the current pixel size.
   float Fade(float frequency,float footprint){return saturate(1.6-frequency*footprint*2.2);}
   float Relief(float2 uv,float fw){
    float h=0;
    // Fibre clumps and formation (the cloudy "look-through" of real paper) read at normal viewing distance.
    if(_Fibre>0){h+=_Fibre*(Noise(uv*38)*.16+Noise(uv*75)*.26*Fade(75,fw)+Noise(uv*150)*.3*Fade(150,fw));h+=_Fibre*(Noise(uv*260)*.45*Fade(260,fw)+Noise(uv*820)*.35*Fade(820,fw)+Noise(float2(uv.x*1900,uv.y*380))*.3*Fade(1900,fw));}
    if(_Laid>0)h+=_Laid*(sin(uv.y*1150)*.5+.5)*.2*Fade(1150,fw);
    if(_Newsprint>0)h+=_Newsprint*Noise(uv*1500)*.5*Fade(1500,fw);
    if(_Weave>0)h+=_Weave*(sin(uv.x*620)*sin(uv.y*620)*.5+Noise(uv*150)*.3)*Fade(100,fw);
    if(_Stripes>0)h+=_Stripes*(sin(uv.y*2600+Noise(uv*40)*3)*.5+.5)*Fade(420,fw);
    if(_Leather>0){float c=Noise(uv*95);h+=_Leather*(c*c*.8+Noise(uv*340)*.35*Fade(340,fw));}
    if(_Brushed>0)h+=_Brushed*Noise(float2(uv.x*2200,uv.y*24))*.6*Fade(2200,fw);
    return h;
   }
   half4 Frag(V i, FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target {
    half4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;clip(tex.a-.35);
    float3 n=normalize(i.normalWS)*IS_FRONT_VFACE(face,1,-1);
    // Surface relief as a true normal perturbation (surface-gradient bump, no tangents needed).
    float fw=max(fwidth(i.uv.x),fwidth(i.uv.y));
    float relief=Relief(i.uv,fw);float h=relief*_BumpScale;
    float3 dpdx=ddx(i.positionWS),dpdy=ddy(i.positionWS);float dhdx=ddx(h),dhdy=ddy(h);
    float3 n0=n;
    float3 r1=cross(dpdy,n),r2=cross(n,dpdx);float det=dot(dpdx,r1);
    float3 grad=sign(det)*(dhdx*r1+dhdy*r2);
    if(abs(det)>1e-12)n=normalize(abs(det)*n-grad);
    float3 view=normalize(_WorldSpaceCameraPos-i.positionWS);
    // Fixed inspection-only key, fill and rim; no town lights or shadows added.
    float3 key=normalize(float3(-.6,.85,-1));float3 fill=normalize(float3(.8,-.1,-.6));float3 rim=normalize(float3(.3,.6,.8));
    float nk=dot(n,key);
    float diffuse=saturate((nk+.08)/1.08)*.9+.16*saturate(dot(n,fill));
    float ambient=lerp(.08,.17,n.y*.5+.5);
    // Thin paper glows a little where the key light comes through from behind.
    float3 through=tex.rgb*float3(1,.93,.8)*saturate(-nk)*_Translucency*.42;
    float rough=lerp(_Rough,.22,_Metallic);
    float exponent=clamp(2/max(.0025,rough*rough*rough*rough)-2,2,2048);
    float3 hk=normalize(key+view);float nh=saturate(dot(n,hk));
    float3 f0=lerp(float3(.04,.04,.04),tex.rgb,_Metallic);
    float3 spec=f0*pow(nh,exponent)*(exponent+8)/25*saturate(nk);
    float lum=dot(tex.rgb,float3(.3,.59,.11));
    // Ink and graphite catch a soft sheen the paper around them does not.
    float ink=saturate((.42-lum)*3.5)*_Paper;spec+=ink*pow(nh,48)*.07*saturate(nk);
    // Photographs: a sharp clear-coat highlight and a faint sky reflection at grazing angles.
    float fres=pow(1-saturate(dot(n,view)),5);
    float3 refl=reflect(-view,n);
    spec+=_Gloss*(pow(nh,600)*.9*saturate(nk)+lerp(.02,.22,saturate(refl.y*.5+.5))*(.04+.96*fres));
    // Brass and steel: a coloured environment term so metal never reads as flat paint.
    float3 env=lerp(float3(.09,.085,.08),float3(.6,.58,.55),saturate(refl.y*.5+.5));
    float3 metal=_Metallic*tex.rgb*env*(.55+.45*fres);
    float edge=pow(1-saturate(dot(n,view)),3)*saturate(dot(n,rim))*.17;
    float grain=lerp(1,.975+Noise(i.uv*1400)*.05*Fade(1400,fw),_Paper);
    // Age: low-frequency mottling toward tea-brown, foxing spots, and edges darkened by handling.
    float3 albedo=tex.rgb;
    // Ink does not lie evenly: it thins where it crossed raised fibres.
    float inkCover=saturate((.6-dot(tex.rgb,float3(.3,.59,.11)))*2.6)*_Paper;
    albedo+=inkCover*(Noise(i.uv*310)*Noise(i.uv*95))*.08*Fade(310,fw);
    // Writing on the other side shows faintly through thin sheets (mirrored, as seen from behind).
    if(_ShowThrough>0){float3 other=SAMPLE_TEXTURE2D(_ShowMap,sampler_ShowMap,i.uv).rgb;albedo*=1-saturate((.75-dot(other,float3(.3,.59,.11)))*1.6)*_ShowThrough;}
    if(_Age>0){
     float m=Noise(i.uv*5.5)*.6+Noise(i.uv*17)*.4;
     albedo*=lerp(float3(1,1,1),float3(1,.93,.79),_Age*(.25+.55*m));
     albedo*=1-_Age*.1*(Noise(i.uv*48)*Noise(i.uv*9));
     float fox=smoothstep(.8,.93,Noise(i.uv*34+7.3))*smoothstep(.45,.8,Noise(i.uv*6+2.1));
     albedo=lerp(albedo,albedo*float3(.78,.6,.42),fox*_Age*.7);
     float border=min(min(i.uv.x,1-i.uv.x),min(i.uv.y,1-i.uv.y));
     float worn=(1-smoothstep(0,.045+.03*Noise(i.uv*30),border))*_Age;
     albedo=lerp(albedo,albedo*float3(.7,.58,.44),worn*.55);
    }
    // Grain valleys hold a little shadow, so stock reads as stock and not as a print on glass.
    albedo*=1-saturate(relief)*lerp(.05,.16,saturate(_Paper+_Leather*.6+_Weave*.6));
    // A low raking light only adds the relief's contrast, not overall brightness.
    float3 rake=normalize(float3(-1,.18,-.42));float rakeDetail=(dot(n,rake)-dot(n0,rake))*2.4;
    albedo*=1+clamp(rakeDetail,-.25,.25)*saturate(_Paper+_Leather+_Weave+_Fibre*.5);
    // Calendered paper keeps a faint broad sheen.
    spec+=_Paper*.022*pow(nh,7)*saturate(nk);
    float3 color=albedo*(diffuse*(1-_Metallic*.7)+ambient)*grain+through*(albedo/max(tex.rgb,.001))+spec+metal+edge;
    if(_Glass>0){
     // Thin glass: mostly clear, a faint film of dust (patchy, heavier toward the edge), and reflections.
     float r=length(i.uv-.5)*2;float film=saturate(.35+Noise(i.uv*14)*.4+Noise(i.uv*55)*.25)*(.55+.45*saturate(r));
     float cover=saturate(.025+film*.065*_Glass+fres*.5);
     float3 glassColor=_BaseColor.rgb*film*.08*(diffuse+ambient)+spec*1.6+float3(.5,.52,.55)*pow(nh,900)*2*saturate(nk)+lerp(.015,.18,saturate(refl.y*.5+.5))*(.05+.95*fres);
     return half4(glassColor,cover);
    }
    return half4(color,1);
   }
   ENDHLSL
  }
 }
}
