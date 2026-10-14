"""Author the E garage's text resources (documents, mechanic dialogue) and the typeset paper
faces of its two notebook records.

Run from the repository root:  python -X utf8 tools/garage/write_garage_texts.py
Writes game/Assets/Resources/GarageDocuments.json, MechanicDialogue.json and
game/Assets/Art/Garage/Papers/{W01,W02,Envelope}.png (deterministic TownSans typography).

Story basis: 错日证词_修订稿 section 5 地点H (front counter and customer register, the
mechanic's work vehicle, the neighbouring-city workshop's work orders and two customers'
independent testimony that place him there on the 20th) and section 7 (修车匠 and the two
customers' own lines, used verbatim). Day-to-day register lines, prices and fittings are
ordinary shop life and add no case fact; the register's 20 January margin note is the
mechanic's own word and is only corroborated by the independent W01/W02 copies. No new
time, route or culprit statement is introduced. Wording awaits the user's review.
"""
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
RES = ROOT / 'game/Assets/Resources'
PAPERS = ROOT / 'game/Assets/Art/Garage/Papers'


def doc(i, title, source, body, image=''):
    return dict(id=i, title=title, source=source, body=body, image=image)


W01_BODY = ("凯恩车间　工单（抄件）\n一九三一年一月二十日　星期二\n\n"
            "一、摩托车一辆　更换传动皮带\n　　下午二时接车。傍晚六时车主来取，皮带尚在更换；换好后当场交车。\n　　经手：修车匠（签名）\n\n"
            "二、轿车一辆　检修刹车\n　　下午三时接车。傍晚七时前车主付账取车，收据加盖车间邮戳。\n　　经手：修车匠（签名）\n\n"
            "原件存邻市凯恩车间。")
W02_BODY = ("两位顾客的证词（抄件）\n\n"
            "顾客甲（取摩托车）：\n“见过。我六点去取摩托，他还在给我的车换皮带。我拿走车时，他没离开工位。”\n\n"
            "顾客乙（付修车账）：\n“七点前我去付账。他还在。我后来把收据寄给警署了，收据上有车间的邮戳。”\n"
            "“收据日期、我住的旅店登记和那班末班车都对得上。镇上警察来问过，我也把原件给他们看了。”\n\n"
            "问话记录原件存青川警署。")

DOCS = [
    doc('register', '客户登记本', '修车铺接待室 · 柜台',
        "一月十四日　自行车一辆，换内胎。已取。\n"
        "一月十五日　马车铁箍松脱，上门修理。已结。\n"
        "一月十六日　摩托车化油器清洗。已取。\n"
        "一月十七日　手推车换车轴。已取。\n"
        "一月十九日　轿车换刹车片。车主改日来取。\n"
        "一月二十日　（这一行空着。页边用铅笔写着：去邻市凯恩车间帮工，铺面不开。）"),
    doc('office-phone', '柜台电话', '修车铺接待室 · 柜台',
        "台式电话，听筒挂在叉簧上。底座下压着一张卡片，记着零件行和邻市凯恩车间的电话号码。"),
    doc('price-board', '价目牌', '修车铺接待室 · 墙上',
        "补胎　　　　三角\n换机油　　　六角\n调化油器　　一元起\n上门修理　　另计路费\n\n本铺修理汽车、摩托车、自行车与农用车。"),
    doc('office-pigeonholes', '分格架', '修车铺接待室',
        "按车主分格放着的修理单据和零件小包。格子上贴着手写的名字。"),
    doc('kane-envelope', '凯恩车间寄来的信封', '修车铺账房 · 书桌信盘',
        "信封上盖着邻市的邮戳，寄信处写着“凯恩车间”。里面抽出过两份抄件，放在桌上。\n\n"
        "信封里还留着一张便条：“原件存车间，警察已经来问过。顾客的收据原件寄到了青川警署。”",
        'Garage/Papers/Envelope.png'),
    doc('w01', '凯恩车间工单（抄件）', '修车铺账房 · 书桌 · 邻市凯恩车间寄来的抄件', W01_BODY, 'Garage/Papers/W01.png'),
    doc('w02', '两位顾客的证词（抄件）', '修车铺账房 · 书桌 · 邻市凯恩车间寄来的抄件', W02_BODY, 'Garage/Papers/W02.png'),
    doc('old-registers', '旧登记簿', '修车铺账房 · 书架', "按年份排好的客户登记簿，最早的一本是一九二七年的。"),
    doc('work-truck', '修车匠的工作车', '修车铺修理车间',
        "一辆旧式轻便货车，墨绿车身，黑色挡泥板。车厢里放着一只红色工具箱和一个备胎，车厢木板上新补了一块。\n\n"
        "手拉葫芦的吊钩正悬在引擎盖上方。"),
    doc('chain-hoist', '手拉葫芦', '修车铺修理车间 · 屋架横梁',
        "挂在屋架横梁上的手拉葫芦，链条被手磨得发亮。吊钩停在货车引擎盖上方。"),
    doc('motorcycle', '送修的摩托车', '修车铺修理车间',
        "一辆旧摩托车支在架子上，车把上挂着一张修理签：换火花塞。"),
    doc('bench-vise', '台虎钳', '修车铺修理车间 · 工作台',
        "铸铁台虎钳，钳口夹着一截锉了一半的铁件。"),
    doc('tool-wall', '工具墙', '修车铺修理车间',
        "木板上钉着一排钩子，扳手、锤子和锉刀按大小挂好。"),
    doc('tag-board', '工具牌挂板', '修车铺修理车间 · 工具库门边',
        "挂板上一排排钩子挂着打了字号的黄铜工具牌，借工具时拿牌子来换。\n\n"
        "第二排有一个钩子空着，钩下那一小块木板的颜色比四周浅。"),
    doc('oil-rags', '油污抹布与手套', '修车铺修理车间',
        "铁桶里堆着用过的抹布，还有好几副沾满机油的帆布手套，样式都差不多。"),
    doc('tyres', '旧轮胎', '修车铺车间后场', "叠起来的旧轮胎，胎面已经磨平。"),
    doc('tool-store', '工具库货架', '修车铺工具库', "分格放着备用零件：火花塞、皮带、弹簧和成盒的螺丝。"),
    doc('loft-crates', '阁楼上的杂物', '修车铺储物阁楼', "旧木箱、拆下来的车座和两只备用车轮，上面积了一层薄灰。"),
    doc('shed-rack', '零件棚', '修车铺后院', "棚下的木架上立着轮胎和钢圈。"),
    doc('tag-blanks', '抽屉里的牌坯', '修车铺车间后场 · 零件柜抽屉', "几块还没打字号的黄铜牌坯，旁边是一盒钢字码。"),
]


def node(i, speaker, text, nxt=None, choices=None):
    n = dict(id=i, speaker=speaker, text=text)
    if nxt:
        n['next'] = nxt
    if choices:
        n['choices'] = [dict(text=c[0], target=c[1], topic=c[2], **({'requiresEvidence': c[3]} if len(c) > 3 else {})) for c in choices]
    return n


M, Y = '修车匠', '你'
DIALOGUE = dict(first='greeting', revisit='greeting', nodes=[
    node('greeting', M, '……是你。你来这儿做什么？', 'topics'),
    node('topics', M, '要问就问吧。', choices=[
        ('你和她打算离开小镇？', 'ask-leave', 'mechanic.leave'),
        ('星期二晚上，你在哪里？', 'ask-where', 'mechanic.where'),
        ('你有工具留在她家吗？', 'ask-tools', 'mechanic.tools'),
        ('凯恩车间寄来的抄件是怎么回事？', 'ask-copies', 'mechanic.copies', 'W01'),
        ('你的工具牌平时挂在哪里？', 'ask-board', 'mechanic.board'),
        ('先这样。', 'ask-goodbye', 'mechanic.goodbye')]),
    node('ask-leave', Y, '你和她打算离开小镇？', 'leave'),
    node('leave', M, '打算。她说那笔钱是她自己的。她欠那么多人，我不信她每句话，可我确实爱过她。', 'topics'),
    node('ask-where', Y, '星期二晚上，你在哪里？', 'where'),
    node('where', M, '邻市的凯恩车间。那天下午我接了两辆车，两个客人都认得我。车间关门后我住在临城旅店，登记簿上有名字。'
                     '你手里那些东西要是说我在她家，就是有人想让你相信我去过。', 'topics'),
    node('ask-tools', Y, '你有工具留在她家吗？', 'tools'),
    node('tools', M, '那块牌子是我的。工具不在她家。有人拿走牌子，不等于我拿走了她的命。', 'topics'),
    node('ask-copies', Y, '凯恩车间寄来的抄件是怎么回事？', 'copies'),
    node('copies', M, '车间把那天的工单抄了一份寄给我，两个客人的话也抄在后面。原件在车间和警署，警察已经去问过。你不信，就自己去核。', 'topics'),
    node('ask-board', Y, '你的工具牌平时挂在哪里？', 'board'),
    node('board', M, '后面那块挂板上，借工具的人拿牌子换，我自己的也挂在那儿。什么时候少的，我说不上来。', 'topics'),
    node('ask-goodbye', Y, '先这样。', 'goodbye'),
    node('goodbye', M, '……走吧。'),
])


def paper(name, title, body, size=(1024, 1320), lines=26):
    """Typeset a sheet: deterministic font, ruled header, wrapped body."""
    font = ROOT / 'game/Assets/Art/Fonts/TownSans.ttf'
    big = ImageFont.truetype(str(font), 44); small = ImageFont.truetype(str(font), 31)
    im = Image.new('RGB', size, (218, 207, 178)); d = ImageDraw.Draw(im)
    d.rectangle((30, 30, size[0] - 31, size[1] - 31), outline=(140, 120, 92), width=2)
    d.text((72, 70), title, font=big, fill=(42, 44, 40))
    d.line((72, 140, size[0] - 72, 140), fill=(96, 92, 76), width=2)
    y = 172; width = 29
    closing = '。，、；：）”！？'
    for para in body.split('\n'):
        if not para:
            y += 18; continue
        while para:
            # Line-start rule: closing punctuation hangs on the line before instead of opening a new one.
            n = width
            while n < len(para) and para[n] in closing:
                n += 1
            d.text((72, y), para[:n], font=small, fill=(52, 50, 42)); para = para[n:]; y += 42
    # A pale ink stamp in the corner, without lettering.
    d.ellipse((size[0] - 250, size[1] - 250, size[0] - 90, size[1] - 90), outline=(122, 64, 56), width=5)
    d.ellipse((size[0] - 232, size[1] - 232, size[0] - 108, size[1] - 108), outline=(122, 64, 56), width=2)
    PAPERS.mkdir(parents=True, exist_ok=True)
    im.save(PAPERS / (name + '.png'), optimize=True)


def envelope():
    font = ROOT / 'game/Assets/Art/Fonts/TownSans.ttf'
    big = ImageFont.truetype(str(font), 52); small = ImageFont.truetype(str(font), 34)
    im = Image.new('RGB', (1200, 640), (196, 178, 140)); d = ImageDraw.Draw(im)
    d.rectangle((24, 24, 1175, 615), outline=(150, 128, 94), width=3)
    d.text((300, 270), '青川修车铺　收', font=big, fill=(40, 40, 36))
    d.text((80, 520), '邻市　凯恩车间　寄', font=small, fill=(60, 56, 46))
    d.ellipse((930, 60, 1120, 250), outline=(80, 70, 70), width=6)
    for k in range(5):
        d.line((900, 110 + k * 22, 1150, 100 + k * 22), fill=(80, 70, 70), width=3)
    d.rectangle((60, 60, 210, 230), outline=(110, 60, 50), width=4)
    PAPERS.mkdir(parents=True, exist_ok=True)
    im.save(PAPERS / 'Envelope.png', optimize=True)


for name, data in (('GarageDocuments.json', dict(documents=DOCS)), ('MechanicDialogue.json', DIALOGUE)):
    (RES / name).write_bytes((json.dumps(data, ensure_ascii=False, indent=1) + '\n').encode('utf-8'))
paper('W01', '凯恩车间工单（抄件）', W01_BODY.split('\n', 1)[1])
paper('W02', '顾客证词（抄件）', W02_BODY.split('\n', 1)[1])
envelope()
print('GARAGE_TEXTS_PASS', len(DOCS), 'documents', len(DIALOGUE['nodes']), 'dialogue nodes')
