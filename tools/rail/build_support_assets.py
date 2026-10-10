"""Original deterministic evidence sheets and simple synthetic railway audio, no external media."""
from pathlib import Path
import math, random, struct, wave
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'game/Assets/Art/Rail';OUT.mkdir(parents=True,exist_ok=True)
font_path=ROOT/'game/Assets/Art/Fonts/TownSans.ttf'
def sheet(name,title,lines,ticket=False):
    image=Image.new('RGB',(1200,850),'#d9c9a6');draw=ImageDraw.Draw(image)
    heading=ImageFont.truetype(str(font_path),58);body=ImageFont.truetype(str(font_path),39)
    draw.rectangle((50,50,1150,800),outline='#544337',width=3)
    draw.text((100,90),title,font=heading,fill='#342e2a')
    if ticket:
        draw.rounded_rectangle((100,245,1090,530),radius=10,outline='#574f40',width=3)
        draw.text((145,275),'青川车站  /  同线路废票',font=body,fill='#342e2a')
        draw.text((145,360),'供查看验票齿孔',font=body,fill='#342e2a')
        for y in (310,346,382,418):draw.ellipse((1067,y,1113,y+26),fill='#242b32')
        start=600
    else:start=235
    for i,line in enumerate(lines):draw.text((100,start+i*68),line,font=body,fill='#342e2a')
    image.save(OUT/name)
from ticket_art import build as build_tickets
from rail_surfaces import build as build_surfaces
build_tickets(OUT,font_path,ROOT/'assets/source/rail')
build_surfaces(OUT)
sheet('E08.png','巡警原簿副本',['一九三一年一月十九日','下午五时二十分','南区巡查记录：有人询问东街。','此处没有该街。','','获准查阅的原簿副本'])
random.seed(31);rate=22050
def write(name,seconds,fn):
    data=bytearray()
    for n in range(int(rate*seconds)):
        t=n/rate;v=max(-1,min(1,fn(t,seconds)))
        data.extend(struct.pack('<h',int(v*22000)))
    with wave.open(str(OUT/name),'wb') as sound:
        sound.setnchannels(1);sound.setsampwidth(2);sound.setframerate(rate);sound.writeframes(data)
def whistle(t,d):
    envelope=min(1,t/.18,(d-t)/.55)
    return envelope*.27*sum(math.sin(math.tau*f*t+math.sin(t*18)*.035) for f in (392,494,587))
def running(t,d):
    pulse=(max(0,math.sin(math.tau*t*4))**12)
    return (random.uniform(-1,1)*pulse*.5+math.sin(t*math.tau*48)*.075)
write('whistle.wav',2.3,whistle);write('running.wav',2,running)
soft=Image.new('RGBA',(64,64));pixels=soft.load()
for x in range(64):
    for y in range(64):
        r=((x-31.5)**2+(y-31.5)**2)**.5/31.5
        pixels[x,y]=(230,235,238,int(255*max(0,1-r)**2))
soft.save(OUT/'Steam.png')
print('RAIL_SUPPORT_ASSETS_PASS')
