"""One deterministic stock/three-hole definition for physical tickets and their viewers."""
import json, math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from fontTools.ttLib import TTFont

WIDTH, HEIGHT = 110., 65.  # millimetres; original fictional ticket stock
HOLES = [(104., y, 3.6) for y in (16., 32.5, 49.)]


def ticket_image(out, font_path, punched):
    rng = np.random.default_rng(1931)
    h, w = 650, 1100
    yy, xx = np.mgrid[:h, :w]
    grain = rng.normal(0, 1.5, (h, w))
    age = 4*np.cos(xx*.009)*np.sin(yy*.008)
    edge = np.maximum(0, 13-np.minimum.reduce([xx, yy, w-1-xx, h-1-yy]))*.38
    rgb = np.stack([237+grain+age-edge, 223+grain+age-edge, 186+grain+age-edge], -1)
    a = np.full((h,w,1),255)
    im = Image.fromarray(np.concatenate([np.clip(rgb,0,255),a],-1).astype('uint8'),'RGBA')
    d = ImageDraw.Draw(im)
    ink = '#443b2d'
    def font(n): return ImageFont.truetype(str(font_path),n)
    d.rectangle((45,40,1055,610), outline=ink, width=4)
    d.rectangle((56,51,1044,599), outline='#8d7956', width=2)
    for x,y in [(64,61),(1036,61),(64,589),(1036,589)]:
        d.line((x-8,y,x+8,y),fill=ink,width=2);d.line((x,y-8,x,y+8),fill=ink,width=2)
    d.text((95,87),'青 川 车 站',font=font(74),fill=ink)
    d.text((99,190),'单 程 乘 车 券',font=font(39),fill=ink)
    d.line((96,268,975,268),fill=ink,width=3)
    d.text((98,300),'青川   →   邻镇',font=font(62),fill=ink)
    d.text((100,408),'日期',font=font(32),fill=ink)
    if punched:
        # The retained sample's historical date is unknown; do not invent it.
        d.line((206,456,867,456),fill='#8d7956',width=2)
    else:
        d.text((202,398),'一九三一年一月二十日',font=font(39),fill=ink)
    d.text((100,520),'请妥善保存车票',font=font(31),fill=ink)
    # Paper itself has no verdict, mock serial number, or printed hole symbol.
    if punched:
        for x,y,r in HOLES:
            d.ellipse(((x-r)*10,(y-r)*10,(x+r)*10,(y+r)*10),fill=(0,0,0,0))
    im.save(out / ('E03.png' if punched else 'E02.png'))


def geometry(punched):
    """Extruded mesh with actual circular voids, independent of the texture alpha."""
    holes = HOLES if punched else []
    cuts={0.,HEIGHT}
    for x,y,r in holes:
        cuts.update(round(y+r*math.sin(i*math.tau/64),9) for i in range(64))
    levels=sorted(cuts)
    points=[];tris=[];uvs=[]
    def point(x,y,up):
        points.extend([(x-WIDTH/2)/1000,up,(HEIGHT/2-y)/1000])
        uvs.extend([x/WIDTH,1-y/HEIGHT])
        return len(points)//3-1
    def quad(a,b,c,d,flip=False):
        indices=[a,b,c,a,c,d]
        if flip: indices=[a,c,b,a,d,c]
        tris.extend(indices)
    for lo,hi in zip(levels,levels[1:]):
        cy=(lo+hi)/2
        hole=next((p for p in holes if abs(cy-p[1])<p[2]),None)
        if hole:
            x,y,r=hole
            dx0=math.sqrt(max(0,r*r-(lo-y)**2));dx1=math.sqrt(max(0,r*r-(hi-y)**2))
            spans=[(0,x-dx0,0,x-dx1),(x+dx0,WIDTH,x+dx1,WIDTH)]
        else:spans=[(0,WIDTH,0,WIDTH)]
        for l0,r0,l1,r1 in spans:
            coords=[(l0,lo),(r0,lo),(r1,hi),(l1,hi)]
            quad(*(point(x,y,.0003) for x,y in coords))
            quad(*(point(x,y,-.0003) for x,y in coords),flip=True)
    loops=[[(0,0),(WIDTH,0),(WIDTH,HEIGHT),(0,HEIGHT)]]
    loops += [[(x+r*math.cos(i*math.tau/64),y+r*math.sin(i*math.tau/64)) for i in range(64)] for x,y,r in holes]
    for n,loop in enumerate(loops):
        for a,b in zip(loop,loop[1:]+loop[:1]):
            quad(point(*a,.0003),point(*b,.0003),point(*b,-.0003),point(*a,-.0003),flip=n==0)
    return dict(vertices=points,triangles=tris,uv=uvs)


def build(out, font_path, source):
    source.mkdir(parents=True,exist_ok=True)
    cmap=TTFont(font_path).getBestCmap()
    required='青川车站单程乘车券邻镇日期一九三一年一月二十日请妥善保存车票'
    assert all(ord(c) in cmap for c in required),'Extend TownSans before generating ticket labels'
    ticket_image(out,font_path,False);ticket_image(out,font_path,True)
    spec={'widthMm':WIDTH,'heightMm':HEIGHT,'holes':[dict(xMm=x,yMm=y,radiusMm=r) for x,y,r in HOLES],
          'intact':geometry(False),'punched':geometry(True)}
    text=json.dumps(spec,ensure_ascii=False,separators=(',',':'))+'\n'
    (out/'TicketGeometry.json').write_text(text,encoding='utf-8')
    (source/'ticket-geometry.json').write_text(text,encoding='utf-8')
    # Assertions distinguish clear voids from black paint, and preserve all intact edges.
    for punched in (False,True):
        p=out/('E03.png' if punched else 'E02.png')
        alpha=np.array(Image.open(p))[:,:,3]
        for x,y,r in HOLES:assert alpha[round(y*10),round(x*10)]==(0 if punched else 255)
        if not punched:assert alpha.min()==255
    print('TICKET_ART_PASS: intact opaque stock; three transparent punch holes; shared physical geometry')
