from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageFilter
import numpy as np
import json,random
from data import ROOT,ART,SRC
rng=np.random.default_rng(1931);N=1024
y,x=np.mgrid[0:N,0:N];noise=rng.normal(0,1.8,(N,N))
def save(name,a):
 im=Image.fromarray(np.uint8(np.clip(a,0,255)));im.save(ART/(name+'.png'));return im
wood=6*np.sin(y*.07+3*np.sin(x*.004))+2*np.sin(y*.5+np.sin(x*.01))+noise
save('Walnut',np.stack([100+wood,70+wood*.7,43+wood*.5],2))
plank=wood.copy();plank[(y%128)<3]-=23
for row in range(8):plank[row*128:(row+1)*128,((row*223)%N):((row*223)%N)+3]-=18
save('Floor',np.stack([115+plank,89+plank*.75,60+plank*.5],2))
stucco=noise+3*np.sin(x*.043)*np.sin(y*.037)
save('Plaster',np.stack([182+stucco,173+stucco,149+stucco],2))
fabric=noise*.5+((x%4)<2)*3+((y%4)<2)*3
save('Fabric',np.stack([112+fabric,119+fabric,88+fabric],2))
wall=Image.new('RGB',(N,N),(181,170,140));d=ImageDraw.Draw(wall)
for u in range(-128,N+128,128):
 for v in range(-128,N+128,128):
  xx=u+(64 if (v//128)%2 else 0);yy=v
  d.line([(xx,yy-32),(xx-20,yy),(xx,yy+32),(xx+20,yy),(xx,yy-32)],fill=(143,148,116),width=2)
  for dx,dy in [(0,-12),(9,0),(0,12),(-9,0)]:d.ellipse((xx+dx-4,yy+dy-6,xx+dx+4,yy+dy+6),fill=(146,122,96))
wall.save(ART/'Wallpaper.png')
rug=Image.new('RGB',(N,N),(99,49,42));d=ImageDraw.Draw(rug)
for k,c in [(8,(48,61,59)),(45,(170,140,95)),(60,(62,74,64)),(110,(184,143,98)),(126,(99,49,42))]:d.rectangle((k,k,N-k,N-k),outline=c,width=12)
for u in range(180,900,128):
 for v in range(180,900,128):d.polygon([(u,v-37),(u+30,v),(u,v+37),(u-30,v)],fill=(149,109,67));d.ellipse((u-8,v-8,u+8,v+8),fill=(48,65,61))
rug.save(ART/'Rug.png')
tile=np.where(((x//128+y//128)%2)==0,1,0);a=np.stack([np.where(tile,153,90),np.where(tile,153,107),np.where(tile,135,99)],2).astype(float)+noise[:,:,None];a[(x%128<3)|(y%128<3)]=58;save('Tile',a)
slate=noise+np.sin(x*.017)*3;slate[(y%96)<4]-=15
save('Slate',np.stack([69+slate,76+slate,79+slate],2))
# Normal maps derived from grayscale surface variation, never from a flat constant.
for name in ['Walnut','Floor','Plaster','Wallpaper','Fabric','Tile','Slate','Rug']:
 im=Image.open(ART/(name+'.png')).convert('L');a=np.asarray(im).astype(float)/255;dy,dx=np.gradient(a);normal=np.stack([-dx*2,-dy*2,np.ones_like(a)],2);normal/=np.linalg.norm(normal,axis=2)[:,:,None];save(name+'Normal',(normal*.5+.5)*255)
# Authored paper sheets use deterministic typography, not generated image lettering.
fontpath=ROOT/'game/Assets/Art/Fonts/TownSans.ttf'
f=ImageFont.truetype(str(fontpath),36);small=ImageFont.truetype(str(fontpath),25)
for e in json.loads((ROOT/'game/Assets/Resources/HouseEvidence.json').read_text())['entries']:
 im=Image.new('RGB',(1024,768),(211,199,167));d=ImageDraw.Draw(im);d.rectangle((35,35,989,733),outline=(133,113,85),width=2);d.text((70,60),e['title'],font=f,fill=(44,47,41));d.line((70,120,955,120),fill=(95,96,79),width=2)
 yy=154
 for para in e['body'].split('\n'):
  while para:
   line=para[:32];para=para[32:];d.text((70,yy),line,font=small,fill=(54,53,44));yy+=36
  yy+=14
 im.save(ART/(e['id']+'.png'))
print('HOUSE_TEXTURES',len(list(ART.glob('*.png'))))
