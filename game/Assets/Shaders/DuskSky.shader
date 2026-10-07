Shader "WrongDate/DuskSky" {
 Properties {
  _Top ("Zenith", Color) = (0.14,0.19,0.34,1)
  _Middle ("Horizon", Color) = (0.42,0.37,0.53,1)
  _Bottom ("Dusk", Color) = (0.76,0.49,0.37,1)
 }
 SubShader {
  Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
  Cull Off ZWrite Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   float4 _Top, _Middle, _Bottom;
   struct appdata { float4 vertex:POSITION; };
   struct v2f { float4 pos:SV_POSITION; float3 dir:TEXCOORD0; };
   v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.dir=v.vertex.xyz; return o; }
   float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
   float noise(float2 p) { float2 i=floor(p), f=frac(p); f=f*f*(3-2*f); return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y); }
   fixed4 frag(v2f i):SV_Target {
    float3 d=normalize(i.dir); float h=saturate(d.y);
    float3 c=lerp(_Bottom.rgb,_Middle.rgb,smoothstep(-0.02,0.18,h));
    c=lerp(c,_Top.rgb,pow(saturate(h*1.45),0.6));
    float2 p=d.xz/max(0.12,d.y+0.18)*2.3;
    float cloud=noise(p*1.8)*0.55+noise(p*3.5)*0.3+noise(p*7)*0.15;
    float band=smoothstep(0.52,0.65,cloud)*smoothstep(0.03,0.2,h)*(1-smoothstep(0.52,0.8,h));
    c=lerp(c,float3(0.43,0.39,0.52),band*0.14);
    return float4(c,1);
   }
   ENDCG
  }
 }
}
