"""Author the A inn's text resources (documents, porter and landlady dialogue) and the typeset
paper faces of its records.

Run from the repository root:  python -X utf8 tools/hotel/write_hotel_texts.py
Writes game/Assets/Resources/HotelDocuments.json, HotelPorterDialogue.json,
HotelKeeperDialogue.json and game/Assets/Art/Hotel/Papers/{I01,I02,I02Plain,I03}.png
(deterministic TownSans typography; run tools/clocktower/expand_ui_font.py first when new
characters are added).

Story basis: 错日证词_修订稿 section 5 地点I (the lobby register's cross-day pages, the
husband's registration under another name that seems to place him out of the house, the
pressed writing on the next page readable by raking light, the dining ledger without a
dinner order, the porter who remembers a short stay after 18:35), the timeline row 1月20日
18:35 (short stay at the inn, checking later arrivals) and fragment 7. Every record states
whose watch its times follow; nothing says which clock is wrong, who killed anyone, or where
the husband was before or after. The alias, other guests' names and the ledger lines are
production proposals awaiting the user's review.
"""
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[2]
RES = ROOT / 'game/Assets/Resources'
PAPERS = ROOT / 'game/Assets/Art/Hotel/Papers'
FONT = ROOT / 'game/Assets/Art/Fonts/TownSans.ttf'


def doc(i, title, source, body, image=''):
    return dict(id=i, title=title, source=source, body=body, image=image)


REG_HEAD = '青川旅馆　旅客登记簿'
REG_RULE = '到店钟点由门房照自己的怀表填写；门房每天早上到车站对表。姓名与来处由旅客自填。'
REG_ROWS = [
    ('上午十时二十分', '赫伯特·莱恩', '邻市', '二', '住两晚'),
    ('下午二时五分', '玛莎·科尔太太', '南边乡下', '三', '探亲'),
    ('下午四时四十分', '弗兰克、贝克（推销员二位）', '邻市', '一', '一晚'),
    ('晚　八时十五分', 'H·华德', '途经', '六', '候夜车'),
]
I01_BODY = (REG_HEAD + '\n一九三一年一月二十日　星期二\n' + REG_RULE + '\n\n'
            + '\n'.join(f'{t}　{n}　来自{f}　{r}号房　{e}' for t, n, f, r, e in REG_ROWS[:3])
            + '\n晚上一行：H·华德　途经　六号房　候夜车。到店钟点一栏的墨迹晕开了，像是压过吸墨纸，能认出的是“八时十五分”。'
            + '\n\n这一页的页角向下折起。')
I02_BODY = ('登记簿的下一页（一月二十一日）。\n\n台灯压低以后，光贴着纸面斜照过来。这一页上半部分还没写字，却有一行压进纸里的笔画，'
            '位置正对着上一页最末一行：\n\n钟点栏：六时三十五分\n房号栏：六\n\n笔画压得很深，像是写字的人用了力。纸面别处是平的。')
LEDGER_HEAD = '餐厅账桌　一月二十日　星期二　晚'
LEDGER_RULE = '收款钟点照门房的表记；老板娘每天傍晚向门房对一次表。'
LEDGER_ROWS = [
    ('六时五分', '二号房　莱恩先生', '晚餐一客（炖牛肉）', '记房账'),
    ('六时三十分', '一号房　推销员二位', '晚餐二客', '付现'),
    ('六时五十分', '六号房　华德先生', '房费一晚（预付）；晚餐：不用', '付现'),
    ('七时十分', '三号房　科尔太太', '茶点', '记房账'),
    ('七时四十分', '门客（非住客）', '晚餐一客', '付现'),
]
I03_BODY = (LEDGER_HEAD + '\n' + LEDGER_RULE + '\n\n' + '\n'.join(f'{t}　{w}　{x}　{p}' for t, w, x, p in LEDGER_ROWS)
            + '\n\n末行是老板娘的字：“晚饭八时收，厨房八时半熄火。”')

DOCS = [
    doc('i01', '旅馆登记簿（一月二十日那页）', '青川旅馆登记大堂 · 查阅桌上摊开的登记簿', I01_BODY, 'Hotel/Papers/I01.png'),
    doc('register-next', '登记簿的下一页', '青川旅馆登记大堂 · 查阅桌',
        '登记簿的下一页，一月二十一日。上半页还空着。手指摸上去，纸面有些不平。\n\n桌上那盏台灯的灯罩可以压低。',
        'Hotel/Papers/I02Plain.png'),
    doc('i02', '登记簿下一页的压痕', '青川旅馆登记大堂 · 查阅桌 · 压低台灯斜照', I02_BODY, 'Hotel/Papers/I02.png'),
    doc('i03', '餐厅账本（一月二十日晚）', '青川旅馆餐厅 · 账桌', I03_BODY, 'Hotel/Papers/I03.png'),
    doc('key-rack', '钥匙板', '青川旅馆登记大堂 · 柜台后',
        '钥匙板上挂着一到六号房的钥匙，黄铜牌上刻着房号。六号房的钥匙挂在最末一格。'),
    doc('counter-book', '柜台日用账', '青川旅馆登记大堂 · 柜台',
        '柜台上的日用账，只记煤、灯油和杂项。\n一月十九日　灯油一桶。\n一月二十日　牛奶三瓶，已送。煤半车。\n一月二十一日　警署来人，问二十日的登记页。'),
    doc('house-rules', '本店规则', '青川旅馆登记大堂 · 墙上',
        '本店规则\n一、到店钟点由门房填写，旅客自填姓名与来处。\n二、晚饭六时起，八时收。\n三、房费预付，过夜不退。\n四、深夜到站的旅客，请摇门铃。'),
    doc('lobby-phone', '大堂电话', '青川旅馆登记大堂',
        '挂在墙上的电话。旁边贴着车站、医院和警署的号码。'),
    doc('lobby-luggage', '待送的行李', '青川旅馆登记大堂',
        '两只等着送上楼的皮箱，箱带上挂着旅馆的行李牌。'),
    doc('lodge-pigeonholes', '门房的信格', '青川旅馆门房',
        '按房号分格的信件和报纸。六号房那一格是空的。'),
    doc('lodge-log', '门房值班簿', '青川旅馆门房 · 桌上',
        '门房的值班簿，只记煤和灯油。\n一月二十日：添煤两次；六号房生炉一次。\n一月二十一日：添煤一次。'),
    doc('police-letter', '警署来函', '青川旅馆账房 · 书桌',
        '青川警署来函（一月二十一日）：请贵店保存一月二十日旅客登记簿原页，以备查核。\n\n信封背面是老板娘的字：“已折角，簿子摊在查阅桌上，别合上。”'),
    doc('old-registers', '旧登记簿', '青川旅馆账房 · 书架',
        '按年份排好的旧登记簿，最早的一本是一九一九年的。'),
    doc('office-safe', '保险柜', '青川旅馆账房',
        '铁保险柜，锁着。柜门上贴着一张纸：“现款每晚入柜。”'),
    doc('fireplace', '餐厅壁炉', '青川旅馆餐厅',
        '石砌壁炉，炉膛里的灰是今早新扫的。炉台上摆着两只铜烛台。'),
    doc('sideboard', '餐具柜', '青川旅馆餐厅', '柜里叠着白瓷盘和擦亮的银餐具。'),
    doc('order-spike', '厨房的点菜单', '青川旅馆厨房 · 墙上',
        '钉在墙上的点菜单，最上面一张写着今天的菜：炖牛肉、烤土豆、苹果派。'),
    doc('dresser', '厨房碗架', '青川旅馆厨房', '碗架上一排排白瓷盘和铜锅，擦得发亮。'),
    doc('left-luggage', '寄存的行李', '青川旅馆储藏洗衣间',
        '客人寄存的行李，每件都挂着牌子，写着取件的日子。'),
    doc('room6-bed', '六号房', '青川旅馆二楼 · 六号房',
        '一张单人床，床铺平整，枕头上没有压痕。窗外能看见楼下的车棚。'),
    doc('room6-stove', '六号房的小铁炉', '青川旅馆二楼 · 六号房',
        '炉子里还剩半炉没烧透的煤块，炉灰没有掏。'),
    doc('room6-tray', '洗脸架上的托盘', '青川旅馆二楼 · 六号房',
        '托盘里一只茶杯倒扣着，没有用过。'),
    doc('guest-room', '客房', '青川旅馆二楼',
        '客房收拾得很干净。床头放着一本旅行指南，书页里夹着一张车站的明信片。'),
    doc('bath', '盥洗室', '青川旅馆二楼', '铸铁浴缸和洗脸盆。墙上写着：要热水请提前告诉门房。'),
]


def node(i, speaker, text, nxt=None, choices=None, grant=None):
    n = dict(id=i, speaker=speaker, text=text)
    if nxt:
        n['next'] = nxt
    if choices:
        n['choices'] = [dict(text=c[0], target=c[1], **({'topic': c[2]} if len(c) > 2 and c[2] else {}),
                             **({'requiresEvidence': c[3]} if len(c) > 3 else {})) for c in choices]
    if grant:
        n['grantEvidence'] = grant
    return n


PT, KP, Y = '门房', '老板娘', '你'
I04 = dict(id='I04', title='旅馆门房的证言', source='青川旅馆门房，问起一月二十日晚上时的回答',
           body='一月二十日晚上六点半刚过（照门房自己的表），一位没带行李的先生进门，要了六号房。门房写好钟点，'
                '上楼去给六号房生炉子，回来时客人已经签好了名。客人没吃晚饭，问了晚上还有哪几班车进站，'
                '在楼上待了不到半个钟头就下楼走了，钥匙留在柜台上，再没回来。\n\n门房说，那位客人就是你。他只凭记忆认人。')
PORTER = dict(first='greeting', revisit='again', evidence=I04, nodes=[
    node('greeting', PT, '晚上好，先生……您，您来过。', 'topics'),
    node('again', PT, '先生还有什么要问？', 'topics'),
    node('topics', PT, '您问吧，我记得的都说。', choices=[
        ('二十日晚上，你见过我？', 'ask-seen', 'hotel.porter.seen'),
        ('你怎么认得是我？', 'ask-sure', 'hotel.porter.sure'),
        ('登记簿上的钟点是谁写的？', 'ask-time', 'hotel.porter.time'),
        ('那晚我问过你什么？', 'ask-asked', 'hotel.porter.asked'),
        ('你那块表准吗？', 'ask-watch', 'hotel.porter.watch'),
        ('先这样。', 'ask-bye')]),
    node('ask-seen', Y, '二十日晚上，你见过我？', 'seen'),
    node('seen', PT, '见过。六点半刚过一点，照我这块表。您没带行李，进门就要一间房，我给了六号。', 'seen-2', grant='I04'),
    node('seen-2', PT, '我写好钟点就上楼去生炉子了。回来的时候您已经签好名，房钱也去账桌付了。', 'seen-3'),
    node('seen-3', PT, '您没吃晚饭。上去不到半个钟头就下来了，钥匙放在柜台上，人就走了。后来没再回来。', 'topics'),
    node('ask-sure', Y, '你怎么认得是我？', 'sure'),
    node('sure', PT, '您的帽子，说话的声音。我干这行二十年了，见过的脸记得住。', 'sure-2'),
    node('sure-2', PT, '簿子上写的名字不是您的名字，是“华德”。客人写什么，我们就认什么，不多问。', 'topics'),
    node('ask-time', Y, '登记簿上的钟点是谁写的？', 'time'),
    node('time', PT, '钟点一向是我写，名字和来处客人自己填。我写完就上楼了，后面的我没看着。', 'topics'),
    node('ask-asked', Y, '那晚我问过你什么？', 'asked'),
    node('asked', PT, '您问晚上还有哪几班车进站。我照墙上的时刻跟您说了，您点点头，就上楼了。', 'topics'),
    node('ask-watch', Y, '你那块表准吗？', 'watch'),
    node('watch', PT, '每天早上我去车站接头班车，顺便跟站上的钟对一回。店里的钟点都照它。', 'topics'),
    node('ask-bye', Y, '先这样。', 'bye'),
    node('bye', PT, '您慢走。夜里路滑。'),
])
KEEPER = dict(first='greeting', revisit='again', nodes=[
    node('greeting', KP, '……是您啊。店里出了这种事，您还肯来坐坐。', 'topics'),
    node('again', KP, '您还想问什么？', 'topics'),
    node('topics', KP, '我在账桌这儿，有话就问吧。', choices=[
        ('二十日那一页登记簿在哪里？', 'ask-page', 'hotel.keeper.page'),
        ('六号房那位客人，你见过吗？', 'ask-six', 'hotel.keeper.six'),
        ('账本上的钟点按哪只表？', 'ask-clock', 'hotel.keeper.clock'),
        ('警察来问过什么？', 'ask-police', 'hotel.keeper.police'),
        ('这家旅馆开了多久？', 'ask-inn', 'hotel.keeper.inn'),
        ('打扰了。', 'ask-bye')]),
    node('ask-page', Y, '二十日那一页登记簿在哪里？', 'page'),
    node('page', KP, '就在大堂查阅桌上摊着。警察来问过以后，我在那一页折了个角，一直没合上。您要看就去看，台灯可以拧低一点。', 'topics'),
    node('ask-six', Y, '六号房那位客人，你见过吗？', 'six'),
    node('six', KP, '那晚我一直在账桌。六号房的房钱是预付的，晚饭说不用。脸我没看清，门房跟他说过话，您问门房吧。', 'topics'),
    node('ask-clock', Y, '账本上的钟点按哪只表？', 'clock'),
    node('clock', KP, '照门房的表。店里来的多是赶车的客人，钟点都照他那块，傍晚我跟他对一回。', 'topics'),
    node('ask-police', Y, '警察来问过什么？', 'police'),
    node('police', KP, '二十一号来的，要我们把二十日的登记页留着别动，问了几句六号房，抄了账本那一页就走了。', 'topics'),
    node('ask-inn', Y, '这家旅馆开了多久？', 'inn'),
    node('inn', KP, '我丈夫的父亲那辈就开着了。铁路通到镇上以后，客人多半是等车、换车的。', 'topics'),
    node('ask-bye', Y, '打扰了。', 'bye'),
    node('bye', KP, '您保重。'),
])


def font(size):
    return ImageFont.truetype(str(FONT), size)


def paper_base(size, seed, tone=(222, 211, 184)):
    rng = np.random.default_rng(seed)
    n = rng.normal(0, 5, (size[1], size[0], 1))
    arr = np.clip(np.array(tone, np.float32)[None, None, :] + n, 0, 255)
    im = Image.fromarray(arr.astype(np.uint8), 'RGB').filter(ImageFilter.GaussianBlur(.6))
    return im


def ledger_rules(d, size, top, rows, cols, colour=(150, 128, 104)):
    for k in range(rows + 1):
        y = top + k * 70
        d.line((48, y, size[0] - 48, y), fill=colour, width=2 if k else 3)
    for x in cols:
        d.line((x, top, x, top + rows * 70), fill=colour, width=2)


def register_page():
    size = (1024, 1365)
    im = paper_base(size, 1920); d = ImageDraw.Draw(im)
    d.text((60, 46), REG_HEAD, font=font(46), fill=(48, 40, 34))
    d.text((60, 112), '一九三一年一月二十日　星期二', font=font(32), fill=(60, 50, 40))
    d.text((60, 160), '到店钟点由门房照自己的怀表填写；', font=font(24), fill=(90, 74, 58))
    d.text((60, 192), '门房每天早上到车站对表。姓名与来处由旅客自填。', font=font(24), fill=(90, 74, 58))
    top = 250; cols = [258, 640, 800, 880]
    ledger_rules(d, size, top, 14, cols)
    for x, h in ((60, '到店钟点'), (270, '姓名'), (650, '来处'), (810, '房'), (890, '备注')):
        d.text((x, top + 18), h, font=font(26), fill=(70, 56, 44))
    ink = (40, 48, 70)
    for k, (t, n, f, r, e) in enumerate(REG_ROWS):
        y = top + 70 * (k + 1) + 18
        if k < 3:
            d.text((60, y), t, font=font(25), fill=ink)
        d.text((270, y), n, font=font(27 if len(n) < 9 else 22), fill=ink)
        d.text((650, y), f, font=font(25), fill=ink); d.text((822, y), r, font=font(27), fill=ink)
        d.text((890, y), e, font=font(23), fill=ink)
    # The evening line's time cell: scraped, blotted, rewritten; only "八时十五分" reads clearly.
    y = top + 70 * 4
    blot = Image.new('L', size, 0); bd = ImageDraw.Draw(blot)
    rng = np.random.default_rng(31)
    for _ in range(26):
        cx, cy = 60 + rng.uniform(10, 180), y + rng.uniform(14, 56)
        r = rng.uniform(8, 26)
        bd.ellipse((cx - r * 1.4, cy - r, cx + r * 1.4, cy + r), fill=int(rng.uniform(40, 95)))
    blot = blot.filter(ImageFilter.GaussianBlur(6))
    stain = Image.new('RGB', size, (118, 120, 150))
    im = Image.composite(stain, im, blot); d = ImageDraw.Draw(im)
    d.text((62, y + 18), '晚　八时十五分', font=font(25), fill=(30, 36, 62))
    # Folded corner (bottom right) and a faint ring from a cup.
    d.polygon([(size[0], size[1] - 170), (size[0] - 170, size[1]), (size[0], size[1])], fill=(176, 162, 136))
    d.polygon([(size[0] - 6, size[1] - 176), (size[0] - 176, size[1] - 6), (size[0] - 176, size[1] - 176)], fill=(206, 194, 166))
    d.line([(size[0] - 6, size[1] - 176), (size[0] - 176, size[1] - 6)], fill=(150, 132, 108), width=2)
    return im


def next_page(raking):
    size = (1024, 1365)
    im = paper_base(size, 1921); d = ImageDraw.Draw(im)
    d.text((60, 46), REG_HEAD, font=font(46), fill=(48, 40, 34))
    d.text((60, 112), '一九三一年一月二十一日　星期三', font=font(32), fill=(60, 50, 40))
    top = 250; cols = [258, 640, 800, 880]
    ledger_rules(d, size, top, 14, cols)
    for x, h in ((60, '到店钟点'), (270, '姓名'), (650, '来处'), (810, '房'), (890, '备注')):
        d.text((x, top + 18), h, font=font(26), fill=(70, 56, 44))
    # The morning's two entries further down the page.
    ink = (40, 48, 70)
    for k, (t, n, f, r, e) in enumerate((('上午九时五十分', '约翰·格雷', '邻市', '四', '一晚'), ('上午十一时', '露丝·班纳特', '本镇', '五', '两晚'))):
        y = top + 70 * (k + 6) + 18
        for x, s, z in ((60, t, 25), (270, n, 27), (650, f, 25), (822, r, 27), (890, e, 23)):
            d.text((x, y), s, font=font(z), fill=ink)
    # Pressed writing in the first rows: invisible flat-lit, raised by raking light (a lit edge and
    # a shadow edge on each stroke, as from a lamp low on the left).
    y = top + 70 + 18
    mask = Image.new('L', size, 0); md = ImageDraw.Draw(mask)
    md.text((60, y), '六时三十五分', font=font(25), fill=255)
    md.text((822, y), '六', font=font(27), fill=255)
    md.text((270, y), '华德', font=font(27), fill=255)
    arr = np.asarray(im).astype(np.float32)
    mask = mask.filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.GaussianBlur(.8))
    m = np.asarray(mask).astype(np.float32) / 255
    if raking:
        lit = np.roll(np.roll(m, -3, 1), -2, 0); shade = np.roll(np.roll(m, 3, 1), 2, 0)
        arr += (lit * 95)[..., None] - (shade * 105)[..., None]
        # Raking light also brings out the paper's tooth across the sheet.
        tooth = np.random.default_rng(7).normal(0, 7, m.shape)
        arr += tooth[..., None]
        arr *= .93
    else:
        arr -= (m * 1.2)[..., None]
    return Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), 'RGB')


def dining_ledger():
    size = (1024, 1365)
    im = paper_base(size, 1922, tone=(226, 218, 196)); d = ImageDraw.Draw(im)
    d.text((60, 46), LEDGER_HEAD, font=font(42), fill=(52, 42, 34))
    d.text((60, 112), '收款钟点照门房的表记；', font=font(25), fill=(90, 74, 58))
    d.text((60, 146), '老板娘每天傍晚向门房对一次表。', font=font(25), fill=(90, 74, 58))
    top = 210; cols = [210, 500, 880]
    ledger_rules(d, size, top, 12, cols, colour=(156, 120, 110))
    for x, h in ((60, '钟点'), (220, '房号　客人'), (510, '所点'), (890, '付款')):
        d.text((x, top + 18), h, font=font(26), fill=(70, 56, 44))
    ink = (54, 36, 30)
    for k, (t, w, x_, p) in enumerate(LEDGER_ROWS):
        y = top + 70 * (k + 1) + 18
        d.text((60, y), t, font=font(25), fill=ink); d.text((220, y), w, font=font(22), fill=ink)
        d.text((510, y), x_, font=font(22 if len(x_) > 12 else 25), fill=ink); d.text((890, y), p, font=font(24), fill=ink)
    d.text((60, top + 70 * 12 + 40), '晚饭八时收，厨房八时半熄火。', font=font(28), fill=(70, 46, 40))
    return im


for name, data in (('HotelDocuments.json', dict(documents=DOCS)), ('HotelPorterDialogue.json', PORTER),
                   ('HotelKeeperDialogue.json', KEEPER)):
    (RES / name).write_bytes((json.dumps(data, ensure_ascii=False, indent=1) + '\n').encode('utf-8'))
PAPERS.mkdir(parents=True, exist_ok=True)
register_page().save(PAPERS / 'I01.png', optimize=True)
next_page(True).save(PAPERS / 'I02.png', optimize=True)
next_page(False).save(PAPERS / 'I02Plain.png', optimize=True)
dining_ledger().save(PAPERS / 'I03.png', optimize=True)
print('HOTEL_TEXTS_PASS', len(DOCS), 'documents', len(PORTER['nodes']) + len(KEEPER['nodes']), 'dialogue nodes')
