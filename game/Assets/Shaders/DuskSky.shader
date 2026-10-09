Shader "WrongDate/DuskSky" {
 Properties {
  _Top ("Zenith", Color) = (0.14,0.19,0.34,1)
  _Middle ("Upper horizon", Color) = (0.42,0.37,0.53,1)
  _Bottom ("Horizon haze", Color) = (0.76,0.49,0.37,1)
  _CloudColor ("Lit cloud", Color) = (0.89,0.91,0.93,1)
  _CloudShade ("Cloud underside", Color) = (0.48,0.56,0.66,1)
  _Noise ("Seamless cloud fields", 2D) = "gray" {}
  _CloudOffset ("Low cloud drift", Vector) = (0,0,0,0)
  _HighCloudOffset ("High cloud drift", Vector) = (0,0,0,0)
  _Coverage ("Cloud cover", Range(0,1)) = .46
  _Darkness ("Cloud depth", Range(0,1)) = .12
  _Haze ("Weather haze", Range(0,1)) = 0
  _Twilight ("Warm horizon", Range(0,1)) = 1
  _SunDirection ("Sun direction", Vector) = (1,0,0,0)
  _MoonDirection ("Moon direction", Vector) = (-1,0,0,0)
  _SunVisibility ("Sun visibility", Float) = 0
  _MoonVisibility ("Moon visibility", Float) = 0
  _Night ("Stars", Float) = 0
 }
 SubShader {
  Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
  Cull Off ZWrite Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _Noise;
   float4 _Top, _Middle, _Bottom, _CloudColor, _CloudShade, _SunDirection, _MoonDirection, _CloudOffset, _HighCloudOffset;
   float _SunVisibility, _MoonVisibility, _Night, _Coverage, _Darkness, _Haze, _Twilight;
   struct appdata { float4 vertex:POSITION; };
   struct v2f { float4 pos:SV_POSITION; float3 dir:TEXCOORD0; };
   v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.dir=v.vertex.xyz; return o; }
   float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
   float cloudField(float2 p) {
    float3 n=tex2D(_Noise,p).rgb;
    return n.r+(tex2D(_Noise,p*2+7).b-.5)*.055;
   }
   float4 frag(v2f i):SV_Target {
    float3 d=normalize(i.dir); float h=max(0,d.y);
    float sunDot=dot(d,normalize(_SunDirection.xyz));
    float moonDot=dot(d,normalize(_MoonDirection.xyz));
    float3 c=lerp(_Bottom.rgb,_Middle.rgb,smoothstep(0,.24,h));
    c=lerp(c,_Top.rgb,pow(saturate(h),.65));
    float towardSun=pow(saturate(dot(normalize(float3(d.x,.04,d.z)),normalize(float3(_SunDirection.x,.04,_SunDirection.z)))),6);
    float sunset=exp(-h*7)*towardSun*_Twilight;
    c+=float3(.38,.13,.025)*sunset*(1-_Darkness*.6);
    // Two projected layers, mipmapped seamless fields. No volume raymarch or temporal noise.
    float2 p=d.xz/max(.25,d.y+.30)*.38+_CloudOffset.xy;
    float2 warp=tex2D(_Noise,p*.5).gg-.5;
    p+=warp*float2(.14,.08);
    float n=cloudField(p);
    float threshold=lerp(.71,.33,_Coverage);
    float alpha=smoothstep(threshold-.045,threshold+.055,n);
    // A continuous stratus floor closes the sky during persistent rain.
    float stratus=smoothstep(.76,1,_Coverage)*.98;
    alpha=lerp(alpha,1,stratus);
    float above=smoothstep(-.035,.075,d.y);
    alpha*=above;
    float2 lightStep=normalize(_SunDirection.xz+float2(.001,0))*.018;
    float lightEdge=cloudField(p+lightStep);
    float rim=saturate((n-lightEdge)*13+.12)*pow(saturate(sunDot),3);
    float depth=smoothstep(threshold-.02,threshold+.20,n);
    float bodyShade=saturate(depth*.67+(lightEdge-n)*8+.10);
    float3 cloud=lerp(_CloudColor.rgb,_CloudShade.rgb,bodyShade);
    cloud=lerp(cloud,_CloudColor.rgb*.55+_CloudShade.rgb*.45,stratus*.72);
    float3 warmLight=lerp(float3(.91,.94,1),float3(1,.48,.17),_Twilight);
    cloud+=warmLight*rim*_SunVisibility*(.13+_Twilight*.42)*(1-_Darkness);
    cloud+=float3(.20,.055,.015)*sunset*(1-_Darkness*.8);
    // High thin wisps move independently and remain behind the lower cumulus.
    float2 high=d.xz/max(.3,d.y+.4)*.34*float2(.55,1.6)+_HighCloudOffset.xy;
    float w=tex2D(_Noise,high).r;
    float veil=smoothstep(.50,.70,w)*above*(.12+_Coverage*.12);
    c=lerp(c,_CloudColor.rgb,veil);
    float transmission=1-alpha*.96;
    c+=float3(1,.79,.50)*_SunVisibility*transmission*(smoothstep(.99968,.99986,sunDot)*2.1+pow(saturate(sunDot),64)*.22+pow(saturate(sunDot),8)*.07);
    float moonSurface=1;
    if(moonDot>.9996) {
     float3 moonRight=normalize(cross(_MoonDirection.xyz,float3(0,1,0)));
     float3 moonUp=normalize(cross(moonRight,_MoonDirection.xyz));
     float2 moonUV=float2(dot(d,moonRight),dot(d,moonUp))*32+.5;
     moonSurface=.65+tex2D(_Noise,moonUV).b*.45;
    }
    c+=float3(.68,.79,1)*_MoonVisibility*transmission*(smoothstep(.99963,.99984,moonDot)*moonSurface+pow(saturate(moonDot),100)*.035);
    float2 starUV=float2(atan2(d.z,d.x),asin(clamp(d.y,-1,1)))*90;
    float2 cell=floor(starUV); float2 starPos=frac(starUV)-.5;
    float star=step(.992,hash(cell))*exp(-dot(starPos,starPos)*65);
    c+=star*_Night*smoothstep(.06,.22,d.y)*.40*transmission*(1-veil);
    c=lerp(c,cloud,alpha);
    c=lerp(c,_Bottom.rgb,exp(-h*10)*(.15+_Haze*.30));
    return float4(c,1);
   }
   ENDCG
  }
 }
}
