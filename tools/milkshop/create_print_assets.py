"""Deterministic prop lettering; approved background orders, no evidence imagegen text."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import random, math, wave, struct
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'game/Assets/Art/MilkShopInteriorA';ART.mkdir(parents=True,exist_ok=True)
random.seed(1931)
im=Image.new('RGB',(1024,1024),(223,208,173));d=ImageDraw.Draw(im)
fontpath=Path('C:/Windows/Fonts/msyh.ttc')
def f(n):return ImageFont.truetype(str(fontpath),n)
def center(region,text,n,color):
 x,y,w,h=region;d.text((x+w/2,y+h/2),text,font=f(n),fill=color,anchor='mm',align='center')
# Cloth cover title plate.
d.rectangle((0,0,511,149),fill=(145,111,56),outline=(85,64,30),width=10);center((0,0,512,150),'送奶记录',66,(45,40,28))
# Spare slips and ledger print.
for rect in [(0,150,512,520),(512,0,1024,520),(0,520,256,860)]:d.rectangle(rect,fill=(227,214,183),outline=(147,130,96),width=6)
center((24,162,464,70),'送 奶 单',43,(57,46,32))
center((532,16,472,65),'一 月',42,(57,46,32))
for y in range(110,495,64):d.line((530,y,1004,y),fill=(150,137,113),width=3)
for text,y in [('19日 钟表铺',146),('2瓶 · 已送',210),('20日 旅馆',274),('3瓶 · 已送',338)]:center((537,y,458,55),text,36,(68,50,32))
for y in [263,327,391,455]:d.line((40,y,475,y),fill=(152,137,110),width=3)
center((0,530,256,300),'鲜\n奶',72,(58,49,35))
# Side-wall chalkboard.
d.rectangle((256,520,639,1023),fill=(35,48,41));center((273,545,350,440),'鲜奶\n每日送达\n空瓶回收',46,(225,218,187))
# Modest woven runner, deliberately a flat four-vertex surface.
d.rectangle((640,520,1023,1023),fill=(73,89,67))
for inset in [14,20,35,42]:d.rectangle((640+inset,520+inset,1023-inset,1023-inset),outline=(177,167,133),width=4)
for y in range(578,970,10):d.line((688,y,975,y),fill=(83,95,73),width=2)
for x in range(698,965,15):d.line((x,569,x,974),fill=(76,88,65),width=1)
im.save(ART/'InkAtlas.png')
# Original short mechanical bell sound, generated here; spatial audio, no loop.
rate=22050;duration=.85
with wave.open(str(ART/'ServiceBell.wav'),'w') as out:
 out.setnchannels(1);out.setsampwidth(2);out.setframerate(rate)
 out.writeframes(b''.join(struct.pack('<h',int(9000*math.exp(-7*i/rate)*(math.sin(math.tau*1450*i/rate)+.4*math.sin(math.tau*2241*i/rate))*(min(i/70,1)))) for i in range(int(rate*duration))))
print('MILK_PRINT_ASSETS_PASS')
