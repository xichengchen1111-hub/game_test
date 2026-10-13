from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
r=Path(__file__).resolve().parents[2]
font=Path('/System/Library/Fonts/STHeiti Medium.ttc')
if not font.exists():font=r/'game/Assets/Art/Fonts/TownSans.ttf'
im=Image.new('RGB',(1400,1800),(211,201,173));d=ImageDraw.Draw(im)
def text(x,y,t,size):d.text((x,y),t,font=ImageFont.truetype(str(font),size),fill=(44,39,30))
text(440,70,'青川日报',110);text(95,218,'社会版　　一九三一年一月　　案件报道',31)
d.line((80,280,1320,280),fill=(44,39,30),width=5)
text(105,322,'本镇妇人家中遇害',69);text(110,419,'丈夫接受问讯　警方继续调查',48)
lines=['本镇一名妇人于一月二十日夜间，','被发现死于自家会客室。据悉，发现','死者的是其丈夫，邻居夫妇亦在场。','警方已问讯丈夫，并核查其当晚行程。','','住宅内有局部翻乱痕迹，前后门未','发现明显强行进入痕迹。死亡时刻','尚未确定，警方仍在收集有关材料。','','镇上有人谈及死者的家庭纷争，亦','有人将此案与一名修车匠联系起来。','这些传闻尚不能说明谁实施了袭击。','','本报提醒：案情仍在调查，传言与','已核实的事实应加以区别。']
for i,t in enumerate(lines):text(108,550+i*65,t,42)
d.line((95,1610,1300,1610),fill=(44,39,30),width=3);text(108,1645,'案情未结　请勿以传闻代替事实',38)
im.save(r/'game/Assets/Art/Opening/Newspaper.png')
