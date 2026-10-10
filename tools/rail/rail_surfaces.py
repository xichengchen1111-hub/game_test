"""Original seamless base/normal/metallic-smoothness maps, authored at metre scale."""
import numpy as np
from PIL import Image


def build(out):
    n=512;yy,xx=np.mgrid[:n,:n];rng=np.random.default_rng(3101)
    noise=rng.normal(0,1,(n,n));px=xx*2*np.pi/n;py=yy*2*np.pi/n
    waves=np.sin(px*5)+np.sin(py*8)+np.cos((px+py)*3)
    def save(name,base,height,metal,smooth):
        rgb=np.clip(base,0,255).astype('uint8')
        if rgb.ndim==2:rgb=np.repeat(rgb[...,None],3,-1)
        Image.fromarray(rgb).save(out/(name+'Base.png'))
        dx=(np.roll(height,-1,1)-np.roll(height,1,1))*2.4
        dy=(np.roll(height,-1,0)-np.roll(height,1,0))*2.4
        normal=np.stack([-dx,-dy,np.ones_like(dx)],-1);normal/=np.linalg.norm(normal,axis=2,keepdims=True)
        Image.fromarray(np.clip((normal*.5+.5)*255,0,255).astype('uint8')).save(out/(name+'Normal.png'))
        mask=np.empty((n,n,4),dtype='uint8');mask[:,:,0]=metal;mask[:,:,1:3]=0
        mask[:,:,3]=np.clip(smooth,0,255);Image.fromarray(mask).save(out/(name+'Mask.png'))
    # Ashlar courses with staggered vertical joints and restrained limestone mottling.
    courses=yy//128;vert=(xx+(courses%2)*128)%256
    joint=(yy%128<3)|(vert<3)
    h=.14+waves*.014+noise*.006;h[joint]=0
    base=np.stack([226+waves*2+noise,222+waves*2+noise,207+waves*2+noise],-1)
    base[joint]=[183,178,165]
    save('Limestone',base,h,0,42+noise*3)
    # Flooring: diagonal dark/cream tesserae, broad checker blocks and fine stone flecks.
    checker=((xx//128+yy//128)%2).astype(bool);tile=(xx%128<2)|(yy%128<2)
    colors=np.where(checker[...,None],[136,148,145],[221,217,198])+noise[...,None]*2
    colors[tile]=[125,122,112];height=np.ones((n,n))*.12;height[tile]=0
    save('Floor',colors,height,0,92+noise*8)
    # Quarter-cut walnut with periodic swirled grain, never a photo or generated text.
    grain=np.sin(px*16+np.sin(py*2)*1.4+np.sin(px*3)*2)
    fine=np.sin(px*64+np.sin(py*3)*.6)*.8
    v=grain*10+fine+noise*.7
    wood=np.stack([177+v,133+v*.8,88+v*.5],-1)
    save('Walnut',wood,grain*.018+noise*.003,0,95+grain*9)
    # Woven moquette: subtle warm olive diamonds within a small herringbone weave.
    diamond=(np.abs((xx+yy)%64-32)<3)|(np.abs((xx-yy)%64-32)<3)
    weave=((xx%4==0)*2+(yy%4==0))
    felt=np.stack([167+noise*2+weave,177+noise*2+weave,139+noise*2+weave],-1)
    felt[diamond]*=.78
    save('Moquette',felt,(xx%4==0)*.035+(yy%4==0)*.035+noise*.003,0,18)
    # Painted steel keeps restrained broad colour, scuffs and a varied satin response.
    paint=225+noise*1.2+waves*1.3
    save('Paint',paint,noise*.003,16,95+waves*7+noise*4)
    iron=181+noise*3+waves*4
    save('Iron',iron,noise*.009,175,67+noise*6)
    brass=np.stack([222+noise,217+noise,194+noise],-1)
    save('Brass',brass,noise*.002+np.sin(px*64)*.003,225,154+waves*10+noise*3)
    leather=184+waves*2+noise*2
    save('Leather',leather,noise*.014,0,64+waves*5)
    slate=202+noise*3+waves*3
    slate[yy%64<2]=154
    save('Slate',slate,(yy%64>=2)*.09+noise*.008,0,40+noise*3)
    print('RAIL_SURFACES_PASS: nine original seamless surface sets')
