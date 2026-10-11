"""Original painted botanical atlas and tangent normals; no photo/source-image reuse."""
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw,ImageFilter
import json,math
R=Path(__file__).resolve().parents[1];out=R/'game/Assets/Art/Greenery';out.mkdir(exist_ok=True,parents=True)
COLORS=['77634F','948373','C1BBA4','726044','52663E','687B48','768850','8C995E','4C6848','657C53','819364','A0AC73','63785E','7C926E','99A87B','557266','E5DDC7','C589A3','866399','A590BA','D4B56C','91A3C8','AE647E','D5C3A3','B69A49','68784A','527440','937A57','919C6B','708559','D5D1B2','B5BA83']
n=256;rng=np.random.default_rng(19311012);base=Image.new('RGB',(n*8,n*4));normal=Image.new('RGB',base.size,(128,128,255))
y,x=np.mgrid[0:n,0:n];u=x/(n-1);v=y/(n-1)
for k,col in enumerate(COLORS):
 color=np.array([int(col[j:j+2],16) for j in (0,2,4)]);noise=rng.normal(0,1.2,(n,n));h=np.zeros((n,n));tone=np.zeros((n,n))
 if k<4:
  if k==2:
   tone=4*np.sin(y*.14)+noise
   im=Image.fromarray(np.clip(140+tone,0,255).astype('uint8'));d=ImageDraw.Draw(im)
   for i in range(110):
    px=int(rng.uniform(0,n));py=int(rng.uniform(0,n));w=int(rng.uniform(4,42));d.line((px,py,px+w,py+int(rng.uniform(-2,3))),fill=int(rng.uniform(45,100)),width=int(rng.integers(1,4)))
   tone=(np.array(im).astype(float)-140)*.6;h=tone*.8
  else:
   wave=x+3*np.sin(y*.028)+1.5*np.sin(y*.08+x*.023)
   h=7*np.sin(wave*.22)+3*np.sin(wave*.55)+1.6*np.sin(wave*1.6)+noise
   cracks=np.maximum(0,-np.sin(wave*.22)-.57)*31
   tone=h*.6-cracks+3*np.sin(y*.09+x*.07);h-=cracks
 elif 4<=k<16 or k in [25,26,28,29,31]:
  # A central vein and gently arcing side veins, matching geometric leaf UVs.
  center=np.exp(-((u-.5)/.011)**2);edge=np.abs(u-.5)*2
  veins=np.exp(-(np.sin((v+edge*.30)*math.pi*8)/.12)**2)*(1-center)
  h=center*7+veins*2.2+np.sin(v*math.pi)*2+noise*.3
  tone=7*np.sin(v*math.pi)-edge*6+center*8+veins*3+noise
 elif 16<=k<24:
  h=2*np.cos((u-.5)*math.pi*18)*(1-v)+noise*.2
  tone=10*v-6+3*np.cos((u-.5)*math.pi*18)*(1-v)+noise*.5
 else:
  h=noise*2;tone=noise*2
 rgb=np.clip(color[None,None,:]+tone[:,:,None],0,255).astype('uint8');base.paste(Image.fromarray(rgb),(k%8*n,k//8*n))
 dy,dx=np.gradient(h);norm=np.stack([-dx*.14,dy*.14,np.ones_like(dx)],axis=-1);norm/=np.linalg.norm(norm,axis=-1,keepdims=True)
 normal.paste(Image.fromarray(np.clip((norm*.5+.5)*255,0,255).astype('uint8')),(k%8*n,k//8*n))
base.save(out/'GreeneryPalette.png');normal.save(out/'GreeneryNormal.png')
(R/'assets/source/greenery/palette.json').write_text(json.dumps(COLORS))
print('BOTANICAL_TEXTURES_PASS',base.size)
