"""Render controlled clock-tower history lettering; never use generated image text as evidence."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
root=Path(__file__).resolve().parents[2]
output=root/'game/Assets/Art/Textures/TowerHistory.png'
image=Image.new('RGB',(1024,1100),'#d9ceb2');draw=ImageDraw.Draw(image)
for inset,width in [(28,7),(48,2)]:draw.rectangle((inset,inset,1023-inset,1099-inset),outline='#877044',width=width)
font=root/'game/Assets/Art/Fonts/TownSans.ttf'
lines=[('青川古钟塔',106,228),('建成于 1816 年',64,410),('钟表大师',54,554),('埃利亚斯·霍夫曼',60,643),('主持建造',54,730),('百余年钟声',46,900),('守候小镇晨昏',46,965)]
for text,size,y in lines:draw.text((512,y),text,font=ImageFont.truetype(str(font),size),fill='#4e4433',anchor='mm')
image.save(output)
print(output.relative_to(root))
# Controlled Roman numeral dial, with empty hand layer and transparent corners.
import math
image=Image.new('RGBA',(1024,1024),(0,0,0,0));draw=ImageDraw.Draw(image)
for box,fill in [((6,6,1018,1018),'#827045'),((21,21,1003,1003),'#b8a57e'),((34,34,990,990),'#ece1c8'),((54,54,970,970),'#f2e9d3')]:draw.ellipse(box,fill=fill)
draw.ellipse((61,61,963,963),outline='#766c55',width=3)
for index in range(60):
 a=math.radians(index*6);r1=430 if index%5 else 417;r2=444
 draw.line((512+math.sin(a)*r1,512-math.cos(a)*r1,512+math.sin(a)*r2,512-math.cos(a)*r2),fill='#534f45',width=6 if index%5==0 else 3)
roman=['XII','I','II','III','IV','V','VI','VII','VIII','IX','X','XI']
number_font=ImageFont.truetype(str(font),65)
for index,number in enumerate(roman):
 a=math.radians(index*30)
 draw.text((512+math.sin(a)*359,512-math.cos(a)*359),number,font=number_font,fill='#363a39',anchor='mm')
image.save(output.with_name('TowerDial.png'))
