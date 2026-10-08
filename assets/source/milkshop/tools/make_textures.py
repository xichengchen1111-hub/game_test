"""Author repeatable material maps and trace the generated sign's alpha outline."""
from pathlib import Path
import json, numpy as np
from PIL import Image
from scipy.ndimage import gaussian_filter, label

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'textures'; OUT.mkdir(exist_ok=True)
rng=np.random.default_rng(1931)
N=1024
y,x=np.mgrid[0:N,0:N].astype(np.float32)/N
fine=rng.normal(0,1,(N,N)).astype(np.float32)
grain=gaussian_filter(fine,(.45,16)); grain/=grain.std()
grain+=.30*np.sin(y*2400+2.3*np.sin(x*18)+np.sin(x*37))
wear=gaussian_filter(fine,7);wear/=wear.std()
large=gaussian_filter(rng.normal(0,1,(N,N)),60);large/=large.std()
patch=large+.25*np.sin(x*17+y*7)
black=patch>.43
for name,col in [('timber',(139,99,63)),('pale-timber',(202,211,190)),('cream-timber',(219,211,182))]:
    rgb=np.array(col)[None,None,:]+grain[:,:,None]*5+wear[:,:,None]*2.6
    rgb+=large[:,:,None]*3
    Image.fromarray(np.uint8(np.clip(rgb,0,255))).save(OUT/(name+'-basecolor.png'))
rgb=np.where(black[:,:,None],np.array([37,42,40]),np.array([218,214,197])).astype(float)
rgb+=grain[:,:,None]*4+wear[:,:,None]*2
flecks=(wear>1.5)&(grain>1.5)
rgb[flecks]=rgb[flecks]*.78+np.array([145,109,73])*.22
Image.fromarray(np.uint8(np.clip(rgb,0,255))).save(OUT/'cow-timber-basecolor.png')
height=grain*.007+wear*.005
dy,dx=np.gradient(height)
normal=np.dstack((-dx*9,-dy*9,np.ones_like(dx)))
normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
Image.fromarray(np.uint8(np.clip(normal*.5+.5,0,1)*255)).save(OUT/'wood-normal.png')
Image.fromarray(np.uint8(np.clip(.82+grain*.018,0,1)*255)).save(OUT/'wood-roughness.png')

sign=OUT/'cow-sign.png'
if sign.exists():
    # Image data is not edited: low resolution alpha is used only to construct geometry.
    im=Image.open(sign).convert('RGBA'); w,h=im.size
    sample=im.resize((512,round(512*h/w)))
    a=np.asarray(sample)[:,:,3]>128
    labs,count=label(a); sizes=np.bincount(labs.ravel());sizes[0]=0;a=labs==sizes.argmax()
    edges={}
    hh,ww=a.shape
    for yy,xx in np.argwhere(a):
        if yy==0 or not a[yy-1,xx]:edges[(int(xx),int(yy))]=(int(xx+1),int(yy))
        if xx==ww-1 or not a[yy,xx+1]:edges[(int(xx+1),int(yy))]=(int(xx+1),int(yy+1))
        if yy==hh-1 or not a[yy+1,xx]:edges[(int(xx+1),int(yy+1))]=(int(xx),int(yy+1))
        if xx==0 or not a[yy,xx-1]:edges[(int(xx),int(yy+1))]=(int(xx),int(yy))
    loops=[]
    while edges:
        start=next(iter(edges));cur=start;loop=[]
        while cur in edges:
            loop.append(cur);cur=edges.pop(cur)
            if cur==start:break
        if len(loop)>10:loops.append(loop)
    pts=np.array(max(loops,key=len),float)
    def simplify(v,eps):
        if len(v)<3:return v
        d=v[-1]-v[0];length=np.linalg.norm(d)
        distances=np.linalg.norm(v-v[0],axis=1) if length==0 else np.abs(np.cross(d,v-v[0]))/length
        i=int(distances.argmax())
        if distances[i]>eps:return np.concatenate([simplify(v[:i+1],eps)[:-1],simplify(v[i:],eps)])
        return v[[0,-1]]
    k=len(pts)//2; pts=np.concatenate([simplify(pts[:k+1],.65)[:-1],simplify(np.concatenate([pts[k:],pts[:1]]),.65)[:-1]])
    json.dump({'image_size':[w,h],'points_uv':[[float(px/ww),float(1-py/hh)] for px,py in pts]},open(ROOT/'cow-outline.json','w'),indent=2)
    print('Sign contour:',len(pts),'vertices; source alpha:',im.getextrema()[3])
print('Material maps written:',OUT)
