"""Editable A3 portrait game and art poster, using story sources and real captures."""
import hashlib
import json
import math
import re
from pathlib import Path

from PIL import Image
from pypdf import PdfReader, PdfWriter
from reportlab.lib.colors import HexColor, Color
from reportlab.lib.pagesizes import A3
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfgen import canvas
from reportlab.graphics import renderPDF
from reportlab.graphics.barcode.qr import QrCodeWidget
from reportlab.graphics.shapes import Drawing

SOURCE = Path(__file__).resolve().parent
REPO = SOURCE.parents[3]
OUT = REPO / 'output/pdf/developer-poster-20261012'
OUT.mkdir(parents=True, exist_ok=True)
C = json.loads((SOURCE / 'poster-content.json').read_text(encoding='utf-8-sig'))
W, H = A3
M = 54
CW = W-2*M
FONT = 'ProjectText'
BOLD = 'ProjectBold'
SERIF = 'ProjectSerif'
pdfmetrics.registerFont(TTFont(FONT, 'C:/Windows/Fonts/Deng.ttf'))
pdfmetrics.registerFont(TTFont(BOLD, 'C:/Windows/Fonts/msyhbd.ttc', subfontIndex=0))
pdfmetrics.registerFont(TTFont(SERIF, 'C:/Windows/Fonts/simsun.ttc', subfontIndex=0))
INK = '#253933'
DARK = '#142723'
PAPER = '#eee8dc'
LIGHT = '#fbf7ed'
MUTED = '#59675f'
GOLD = '#a3814b'
RULE = '#b5b8a6'
BLUE = '#435e68'
RUST = '#8d5748'

ASSETS = {
    'cover': ('output/demo/tower-night-daylight/night-corner.jpg', '实机截图，钟楼夜景；取右侧改后画面'),
    'street': ('output/demo/greenery-refinement/main-street.jpg', '实机截图，最新阴雾主街与花境'),
    'overview': ('output/demo/greenery-refinement/full-town-overview.jpg', '实机高机位截图，主镇布局'),
    'milk': ('output/demo/milkboy-tripo/entrance-view.png', '实机截图，送奶少年与牛奶屋'),
    'watchmaker': ('output/demo/watchmaker-tripo/player-approach.jpg', '实机截图，Tripo钟表匠与维修台'),
    'clerk': ('output/demo/station-clerk-dialogue/clerk-1080/clerk-six-options.png', '实机截图，售票员六选项对话'),
    'clerkface': ('output/demo/station-clerk-dialogue/clerk-1080/clerk-face-clear-noon.png', '实机受控近景，售票员'),
    'station': ('output/demo/rail-beautify/gallery/front-wide.jpg', '实机截图，B车站'),
    'coach': ('output/demo/rail-beautify/gallery/coach-aisle.jpg', '实机截图，列车客厢'),
    'ticket': ('output/demo/ticket-office/1080/ticket-bin-filled-punched.png', '实机截图，已验票实体孔洞'),
    'hands': ('output/demo/watchmaker-tripo/hands-watch-detail.jpg', '实机截图，钟表匠双手与维修道具'),
    'rainstreet': ('output/demo/greenery-refinement/main-street-night-rain.jpg', '实机截图，雨夜主街'),
    'police': ('docs/concepts/police-station/round-01/d-courtyard-loggia.png', '警署环境概念图，内院回廊方案，非游戏实机')
}
USAGE = []
TEXT_LOG = []
PAGE = 0
PDF = OUT / 'wrong-date-developer-poster-full.pdf'
cv = canvas.Canvas(str(PDF), pagesize=A3, pageCompression=1)
cv.setTitle('错日证词 游戏内容与美术海报 含剧情剧透附录')
cv.setAuthor('错日证词项目')
cv.setSubject('游戏故事 调查玩法 人物 环境与道具美术 声光氛围 兴趣协作')


def fill(color):
    cv.setFillColor(HexColor(color))


def rect(x, top, w, h, color, stroke=None, radius=0):
    fill(color)
    if stroke:
        cv.setStrokeColor(HexColor(stroke))
    if radius:
        cv.roundRect(x, H-top-h, w, h, radius, fill=1, stroke=int(bool(stroke)))
    else:
        cv.rect(x, H-top-h, w, h, fill=1, stroke=int(bool(stroke)))


def line(x1, t1, x2, t2, color=RULE, width=1, dash=None):
    cv.setStrokeColor(HexColor(color))
    cv.setLineWidth(width)
    cv.setDash(dash or [])
    cv.line(x1, H-t1, x2, H-t2)
    cv.setDash([])


def text(value, x, top, size=16, font=FONT, color=INK, max_width=None):
    if max_width is not None:
        assert pdfmetrics.stringWidth(value, font, size)<=max_width+.01, (PAGE, value, 'too wide')
    assert 0<=top and top+size<=H-10, (PAGE, value, 'outside page')
    fill(color)
    cv.setFont(font,size)
    cv.drawString(x,H-top-size*.85,value)
    TEXT_LOG.append({'page':PAGE,'text':value,'x':x,'top':top,'size':size})


def split_lines(value, width, size, font=FONT):
    lines=[]
    for chunk in value.split('\n'):
        current=[]
        for token in re.findall(r'[A-Za-z0-9]+(?:[._/-][A-Za-z0-9]+)*|.',chunk):
            candidate=''.join(current)+token
            if current and pdfmetrics.stringWidth(candidate,font,size)>width:
                if token in '，。！？；：、）】》”：' or current[-1] in '（【《“':
                    carry=current.pop()
                    lines.append(''.join(current).rstrip())
                    current=[carry,token]
                else:
                    lines.append(''.join(current).rstrip())
                    current=[] if token.isspace() else [token]
            else:
                current.append(token)
        lines.append(''.join(current).rstrip())
    return lines


def para(value, x, top, width, size=17, leading=None, font=FONT, color=INK, bottom=None):
    leading=leading or size*1.55
    lines=split_lines(value,width,size,font)
    if bottom is not None:
        assert top+(len(lines)-1)*leading+size<=bottom, (PAGE,value,'text block overflow',top,len(lines),bottom)
    for i,s in enumerate(lines):
        text(s,x,top+i*leading,size,font,color,max_width=width+.5)
    return top+len(lines)*leading


def pill(value,x,top,color=INK,bg='#dce2d5',size=12.5):
    ww=pdfmetrics.stringWidth(value,BOLD,size)+20
    rect(x,top,ww,26,bg,radius=3)
    text(value,x+10,top+6,size,BOLD,color)
    return ww


def image(key,x,top,w,h,region=None,anchor=(.5,.5)):
    rel,desc=ASSETS[key]
    path=REPO/rel
    assert path.exists(), path
    with Image.open(path) as im:
        iw,ih=im.size
    sx,sy,sw,sh=(region or [0,0,1,1])
    sx*=iw;sy*=ih;sw*=iw;sh*=ih
    scale=max(w/sw,h/sh)
    shownw,shownh=sw*scale,sh*scale
    dx=x-(shownw-w)*anchor[0]-sx*scale
    dy=H-top-h-(shownh-h)*(1-anchor[1])-(ih-sy-sh)*scale
    cv.saveState()
    clip=cv.beginPath();clip.rect(x,H-top-h,w,h)
    cv.clipPath(clip,stroke=0,fill=0)
    cv.drawImage(str(path),dx,dy,iw*scale,ih*scale,mask='auto')
    cv.restoreState()
    USAGE.append({'page':PAGE,'key':key,'source':rel,'label':desc,'crop_region':region,'box':[x,top,w,h]})


def panel(title,body,x,top,w,h,color=LIGHT,accent=GOLD,size=16):
    rect(x,top,w,h,color)
    rect(x,top,3,h,accent)
    text(title,x+18,top+17,20,BOLD,max_width=w-36)
    para(body,x+18,top+54,w-36,size,bottom=top+h-13)


def page_set(no):
    global PAGE
    PAGE=no


def page_header(no,label,title,strap,dark=False):
    global PAGE
    PAGE=no
    rect(0,0,W,H,DARK if dark else PAPER)
    if not dark:
        rect(0,0,W,9,INK)
    tone=LIGHT if dark else INK
    text(f'{no:02d} / {label}',M,35,13,BOLD,GOLD if not dark else '#d4b879')
    text(title,M,77,34,BOLD,tone,max_width=CW)
    para(strap,M,130,CW,16,24,color='#c7d2c7' if dark else MUTED)
    line(M,181,W-M,181,color='#526357' if dark else RULE)
    cv.bookmarkPage('p'+str(no))
    cv.addOutlineEntry(title,'p'+str(no),level=0,closed=False)


def footer(page,dark=False):
    tone='#c3cabd' if dark else MUTED
    line(M,H-60,W-M,H-60,color='#516156' if dark else RULE,width=.7)
    text('错日证词  /  游戏介绍  /  兴趣协作',M,H-45,11,BOLD,tone)
    text('2026.10.12  ·  '+f'{page:02d}',W-M-123,H-45,11,FONT,tone)
    cv.showPage()


def arrow(x1,t1,x2,t2,color=GOLD):
    line(x1,t1,x2,t2,color,1.5)
    a=math.atan2(t2-t1,x2-x1)
    for delta in [-.45,.45]:
        line(x2,t2,x2-8*math.cos(a+delta),t2-8*math.sin(a+delta),color,1.5)


def source_check():
    # Verify that every intended character exists in the embedded text fonts.
    missing={}
    for font in [FONT,BOLD,SERIF]:
        glyphs=pdfmetrics.getFont(font).face.charToGlyph
        chars={ch for item in TEXT_LOG for ch in item['text'] if ord(ch)>127}
        absent=''.join(sorted(ch for ch in chars if ord(ch) not in glyphs))
        if absent:missing[font]=absent
    assert not missing, missing


def deliver():
    from poster_pages import draw_all
    draw_all(globals())
    source_check()
    cv.save()
    reader=PdfReader(PDF)
    assert len(reader.pages)==9
    share=PdfWriter()
    for page in reader.pages[:8]:share.add_page(page)
    share.add_metadata({'/Title':'错日证词 游戏内容与美术海报 转发版','/Author':'错日证词项目','/Subject':'游戏故事 调查玩法 美术氛围 兴趣协作'})
    share.write(str(OUT/'wrong-date-developer-poster-share.pdf'))
    extracted=[p.extract_text() for p in reader.pages]
    assert '原主丈夫是真正的凶手' in extracted[8]
    assert all('原主丈夫是真正的凶手' not in p for p in extracted[:8])
    forbidden=['已有切片','局部实现','待完成','已选暂缓','开发缺口','工期','低配','Windows','Unity','MVP','E05','7618050']
    assert all(word not in '\n'.join(extracted) for word in forbidden)
    checks={'pages_full':9,'pages_share':8,'fonts_embedded':True,'glyphs_present':True,
        'core_spoiler_only_in_last_page':True,'source_commit':C['source_commit'],
        'no_progress_or_task_allocation_copy':True,
        'scope':'Document bounds and text/font checks. Visual PDF review must be recorded separately.',
        'page_text_lengths':[len(t) for t in extracted]}
    (OUT/'document-checks.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf-8')
    assets=[]
    for key,(path,label) in ASSETS.items():
        if key not in {use['key'] for use in USAGE}:continue
        p=REPO/path
        assets.append({'key':key,'path':path,'label':label,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
    manifest={'date':C['date'],'source_commit':C['source_commit'],'assets':assets,'image_placements':USAGE,
        'editorial_sources':C['sources'],'main_pages':8,'spoiler_page':9,
        'user_preferences':{'collaboration':'兴趣协作','spoilers':'主体保留悬念，末页单列开发剧透','focus':'游戏内容、美术与氛围，不介绍进度、技术清单或任务分工'},
        'notes':['实机截图来自已记录的不同专项版本，版面裁切不修改原文件。','警署使用已选D的概念图，图下明确标注概念，不冒充实机。','联系方式未提供，项目仓库仅作为了解入口。']}
    (SOURCE/'source-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({'status':'PDF_AUTHORED','full':str(PDF),'share':str(OUT/'wrong-date-developer-poster-share.pdf'),'pages':9},ensure_ascii=False))


if __name__=='__main__':
    deliver()
