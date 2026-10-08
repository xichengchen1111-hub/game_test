"""Original repeatable walnut-shop surface detail; no external artwork or source photos."""
from pathlib import Path
import math
import random
from PIL import Image, ImageDraw, ImageFilter


def main():
    root=Path(__file__).resolve().parents[1]
    target=root/'game/Assets/Art/WalnutShop'
    target.mkdir(parents=True,exist_ok=True)
    n=1024
    rng=random.Random(1931)
    phases=[rng.random()*math.tau for _ in range(12)]
    wood=Image.new('RGB',(n,n))
    pixels=wood.load()
    for y in range(n):
        for x in range(n):
            a=x/n*math.tau
            warp=2.4*math.sin(y/n*math.tau)+.8*math.sin(y/n*math.tau*3+a)
            grain=sum(math.sin(a*(j+4)*3+phases[j]+warp*(.5+j*.12))/(j+2) for j in range(12))
            value=max(175,min(252,int(224+grain*13+rng.uniform(-3,3))))
            pixels[x,y]=(value,int(value*.985),int(value*.955))
    wood.save(target/'Wood.png')
    plaster=Image.new('RGB',(n,n));p=plaster.load()
    for y in range(n):
        for x in range(n):
            v=rng.randrange(235,251);p[x,y]=(v,v,v)
    plaster=plaster.filter(ImageFilter.GaussianBlur(.6));plaster.save(target/'Plaster.png')
    rug=Image.new('RGB',(768,1024),(151,120,105));d=ImageDraw.Draw(rug)
    for inset,color,width in [(12,(222,196,149),10),(30,(76,88,79),16),(53,(203,169,117),4),(72,(98,72,65),18),(94,(215,183,136),4)]:
        d.rectangle((inset,inset,767-inset,1023-inset),outline=color,width=width)
    for y in range(140,920,104):
        for x in range(145,670,96):
            d.polygon([(x,y-28),(x+30,y),(x,y+28),(x-30,y)],outline=(215,185,137),width=4)
            d.rectangle((x-4,y-4,x+4,y+4),fill=(74,88,78))
    # Fine woven cross-thread modulation rather than an oversized flat decal.
    p=rug.load()
    for y in range(rug.height):
        for x in range(rug.width):
            v=.96 if (x+y)%3==0 else 1.02
            p[x,y]=tuple(max(0,min(255,int(c*v+rng.uniform(-3,3)))) for c in p[x,y])
    rug.save(target/'Rug.png')
    print('WATCH_SHOP_TEXTURES_PASS: original Wood, Plaster, Rug')


if __name__=='__main__':
    main()
