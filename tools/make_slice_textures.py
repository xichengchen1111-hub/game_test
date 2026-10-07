"""Precisely authored signs, clocks and paper textures for the 3D scene."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import math

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'game/Assets/Art/Textures'
FONT=ROOT/'game/Assets/Art/Fonts/TownSans.ttf'
def font(size): return ImageFont.truetype(str(FONT),size)
def centered(draw,xy,text,size,color): draw.text(xy,text,font=font(size),fill=color,anchor='mm')
def sign(file,title,subtitle):
    im=Image.new('RGB',(1536,384),'#233C3B');d=ImageDraw.Draw(im)
    d.rectangle((20,20,1515,363),outline='#B59362',width=4)
    centered(d,(768,155),title,126,'#D9C9A9');centered(d,(768,294),subtitle,33,'#B9AC91')
    im.save(OUT/file)
def clock(file):
    im=Image.new('RGBA' if file=='WatchFace.png' else 'RGB',(768,768),(0,0,0,0) if file=='WatchFace.png' else '#B59560');d=ImageDraw.Draw(im)
    d.ellipse((8,8,760,760),fill='#AD8650',outline='#755B3B',width=12)
    d.ellipse((35,35,733,733),fill='#DDCAA2',outline='#A99571',width=8)
    d.ellipse((61,61,707,707),outline='#A59271',width=2)
    for i in range(60):
        a=math.radians(i*6);r=310;inner=286 if i%5==0 else 300
        d.line((384+math.sin(a)*r,384-math.cos(a)*r,384+math.sin(a)*inner,384-math.cos(a)*inner),fill='#3A3431',width=5 if i%5==0 else 2)
    roman=['XII','I','II','III','IV','V','VI','VII','VIII','IX','X','XI']
    for i,s in enumerate(roman):
        a=math.radians(i*30);centered(d,(384+math.sin(a)*245,384-math.cos(a)*245),s,49,'#39342F')
    centered(d,(384,486),'青川',26,'#7C6B52');centered(d,(384,523),'1931',20,'#7C6B52')
    im.save(OUT/file)
def main():
    OUT.mkdir(parents=True,exist_ok=True)
    sign('SignClock.png','钟 表 铺','钟表修理   ·   老街一九〇八')
    sign('SignStation.png','青 川 车 站','QINGCHUAN RAILWAY   ·   1931')
    sign('SignInn.png','青 川 客 栈','茶点   ·   客房')
    clock('ClockFace.png');clock('WatchFace.png')
    im=Image.new('RGB',(900,1200),'#D9C6A0');d=ImageDraw.Draw(im)
    d.rectangle((40,40,860,1160),outline='#8C7657',width=3)
    centered(d,(450,150),'镇务维修通知',68,'#403A31')
    d.line((100,237,800,237),fill='#7A6950',width=2)
    for i,line in enumerate(['1931 年 1 月 20 日','中午十二时','南区街灯已修复。','灯柱上的白布，','于修复后拆下。','修灯工签记：已完工。']):
        d.text((98,302+i*112),line,font=font(45 if i<2 else 42),fill='#453E34')
    im.save(OUT/'Notice.png')
    im=Image.new('RGB',(768,900),'#AE8963');d=ImageDraw.Draw(im)
    d.rectangle((48,48,720,852),fill='#BCA17A');d.rectangle((88,100,680,650),fill='#CFAB7E')
    d.line((385,100,385,650),fill='#524536',width=21);d.line((88,390,680,390),fill='#524536',width=18)
    im.save(OUT/'DoorWindow.png')
    im=Image.new('RGB',(1200,800),'#536681');d=ImageDraw.Draw(im)
    for i,(x,h,c) in enumerate([(0,380,'#48536D'),(280,450,'#4A5672'),(630,340,'#424E66'),(970,420,'#485870')]):
        d.rectangle((x,h,x+250,800),fill=c);d.polygon([(x-15,h),(x+125,h-90),(x+265,h)],fill='#3E4960')
        for a in range(2):
            for b in range(2):d.rectangle((x+50+a*100,h+50+b*110,x+90+a*100,h+117+b*110),fill='#B99672' if (a+b+i)%3 else '#647590')
    im.save(OUT/'WindowTown.png')
    print('Created 8 authored texture assets:',OUT)
if __name__=='__main__': main()
