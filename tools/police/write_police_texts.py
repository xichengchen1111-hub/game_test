"""Author the police station's text resources (documents, report rules, duty-officer dialogue)
and the typeset paper faces of its papers.

Run from the repository root:  python -X utf8 tools/police/write_police_texts.py
Writes game/Assets/Resources/PoliceDocuments.json, PoliceReport.json and
PoliceOfficerDialogue.json, and game/Assets/Art/Police/Papers/*.png. Texts only typeset facts
already in the revised script (section 1, police view) and docs/mvp-evidence.json; the E08 copy
repeats the station desk's text exactly. No new case facts, dates or culprit statements.

Each paper is printed with exactly its document's title and body (the blank interview form
carries only the heading its description quotes). Objects described in prose (telephones, stove,
cage, bed, rain capes, bound volumes, the photo board) get no paper. Without an image the evidence
close-up and the briefcase draw a blank sheet (2026-10-15).
"""
import json
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
R = ROOT / 'game/Assets/Resources'
PAPERS = ROOT / 'game/Assets/Art/Police/Papers'
FONT = ROOT / 'game/Assets/Art/Fonts/TownSans.ttf'

E01_BODY = ("一九三一年一月二十日夜，一名妇人死于自家会客室，由丈夫发现，邻居夫妇随同进屋。死者遭钝器袭击，死亡时间只能估为傍晚一段较宽的时段。"
            "前后门无强行进入痕迹；家具局部翻乱，收款抽屉被打开；现场有丈夫的旧外套、修车匠工具牌、机油手套和一封被裁去上下文的信（照片附卷）。\n"
            "丈夫陈述（摘要）：二十日约五时三十五分离家，到车站乘车，赴另一镇南区“东街”之约，到了才知那里没有东街；出示一月二十日车票一张；"
            "八时四十分前后回家，与邻居夫妇一同进屋发现妻子。\n"
            "送奶少年证言（摘要）：二十日傍晚送奶到该户，妻子亲手接过牛奶，时为六点二十五分。\n"
            "丈夫经短暂羁押后获准保释，配合调查。")
E08_BODY = "一九三一年一月十九日\n下午五时二十分\n\n南区巡查记录：有人询问东街。此处没有该街。\n\n本页为获准查阅的原簿副本。"


def doc(i, title, source, body, image=''):
    return dict(id=i, title=title, source=source, body=body, image=image)


DOCS = [
    doc('e01-copy', '案卷摘要（副本）', '青川警署案卷阅览室 · 公开材料桌 · 已获许可的警方材料副本', E01_BODY, 'Police/Papers/E01.png'),
    doc('e01-compare', '案卷摘要（副本）', '青川警署对照桌 · 与公开材料桌同一份摘要副本', E01_BODY, 'Police/Papers/E01.png'),
    doc('e08-copy', '巡警原簿副本', '青川警署对照桌 · 另一镇巡警原簿的获准调取副本（与车站资料桌为同一抄本）', E08_BODY, 'Rail/E08.png'),
    doc('husband-statement', '丈夫陈述 · 誊录页', '警方誊录的丈夫陈述（副本）· 公开材料桌',
        "我二十号傍晚出的门，大概五点三十五分。去车站坐车，到另一镇南区赴约，对方约在东街。到了南区一问，才知道那里根本没有东街。\n\n"
        "车票我留着，是二十号的。我在那边耽搁了一阵，回到家已经八点四十分左右，正碰上邻居夫妇要出门散步，我请他们陪我进屋……\n\n（页末有陈述人签名。）",
        'Police/Papers/HusbandStatement.png'),
    doc('letter-photo', '残信照片（翻拍）', '现场照片副本 · 公开材料桌（原件封存于保管室）',
        "现场发现的信件照片。信纸上下两端被整齐地裁去，只剩中间几行字，抬头和落款都不在。\n\n照片背面注：现场物证，原件封存。",
        'Police/Textures/PoliceLetterPhoto.png'),
    doc('hall-notice', '查阅须知', '青川警署接待门厅',
        "一、公开材料可在阅览室查阅，阅后放回原处。\n二、物证原件封存，不对外开放；照片副本见东翼保管室。\n"
        "三、阶段报告可交门口小桌，或交给值班警员。\n四、请勿将材料带出警署。", 'Police/Papers/HallNotice.png'),
    doc('patrol-notice', '巡逻告示', '青川警署贯通横厅', "夜间巡逻按车站路、中街、北后巷的顺序进行。\n雨天外出请带雨披。",
        'Police/Papers/PatrolNotice.png'),
    doc('telephone', '墙上的电话', '青川警署贯通横厅', "墙上的摇柄电话，线路接到镇上的总机。"),
    doc('office-phone', '值班室电话', '青川警署值班接待室', "台式电话，听筒挂在叉簧上。旁边压着一张总机号码的小卡片。"),
    doc('catalog', '案卷目录（公开部分）', '青川警署档案誊录室 · 誊录桌',
        "案卷摘要：阅览室公开材料桌、对照桌。\n丈夫陈述誊录页：公开材料桌。\n现场照片：公开材料桌（残信）、保管室照片板（其余物件）。\n"
        "另一镇巡警原簿副本：对照桌；车站资料桌另有一份。\n物证原件：封存于保管室，不对外开放。\n\n（誊录员注：目录只登记存放位置。）",
        'Police/Papers/Catalog.png'),
    doc('interview-pad', '问询记录纸', '青川警署问询室', "空白问询记录纸。抬头印着“青川警署 问询记录”，日期一栏空着。",
        'Police/Papers/InterviewPad.png'),
    doc('duty-book', '巡逻值班簿', '青川警署巡警值班室', "只记班次、巡线与交接时间，没有案件内容。"),
    doc('rain-capes', '油布雨披', '青川警署巡警值班室', "挂着两件油布雨披，衣襟上有干了的泥点。"),
    doc('photo-board', '获准照片副本', '青川警署保管室 · 照片板',
        "一、会客室里的旧外套\n二、修车匠工具牌\n三、机油手套\n四、被裁去上下文的信（照片另见公开材料桌）\n\n原件封存在柜中。"),
    doc('sealed-cage', '封存物证柜', '青川警署保管室', "铁丝网柜门上了锁，封条上写着保管编号。柜里的纸箱贴着标签，原件不对外开放。"),
    doc('holding-bed', '旧羁押室', '青川警署东翼', "铁架床上铺着一张薄褥，叠得很整齐。墙角放着搪瓷盆。高窗很小，只看得见一点天色。\n\n这里空着，门没有上锁。"),
    doc('holding-basin', '搪瓷盆', '青川警署旧羁押室', "一只白色搪瓷盆，盆沿磕掉了一小块漆。"),
    doc('gazette', '镇报合订本', '青川警署资料分馆', "镇报按月装订成册，书脊上写着年份和月份。"),
    doc('archive-boxes', '装订卷宗', '青川警署装订档案库', "卷宗按年份排列，多是与本案无关的旧档。"),
    doc('permit-stub', '查阅许可（存根）', '青川警署负责人办公室', "准许当事人在调查期间查阅本案公开材料副本，阅后放回原处，不得带走原件。\n\n——青川警署",
        'Police/Papers/PermitStub.png'),
    doc('archive-index', '档案索引柜', '青川警署二层档案储存', "按证人、日期、物证三类建立的卡片，和楼下阅览室的分类柜同一套编号。"),
    doc('stove', '小铁炉', '青川警署候问侧席', "铸铁小炉，炉膛里还有一点余温。"),
    doc('index-witness', '分类索引 · 证人', '青川警署阅览室分类柜 · 证人抽屉',
        "丈夫：陈述誊录页、案卷摘要。\n送奶少年：案卷摘要（证言摘要）。\n邻居夫妇：案卷摘要（随同进屋）。\n\n卡片背面：原始笔录存档。",
        'Police/Papers/IndexWitness.png'),
    doc('index-date', '分类索引 · 日期', '青川警署阅览室分类柜 · 日期抽屉',
        "一九三一年一月十九日：另一镇巡警原簿副本。\n一九三一年一月二十日：案卷摘要、丈夫陈述、送奶证言摘要、现场照片。",
        'Police/Papers/IndexDate.png'),
    doc('index-evidence', '分类索引 · 物证', '青川警署阅览室分类柜 · 物证抽屉',
        "旧外套：封存，见照片板。\n工具牌：封存，见照片板。\n机油手套：封存，见照片板。\n被裁的信：封存，照片在公开材料桌。\n车票（一月二十日）：当事人自行保管。",
        'Police/Papers/IndexEvidence.png'),
]

REPORT = dict(
    statements=[dict(id='S1', text='送奶少年：二十日傍晚六点二十五分，他把牛奶交给了妻子。'),
                dict(id='S2', text='丈夫：二十日傍晚，他乘车到另一镇南区问东街。'),
                dict(id='S3', text='丈夫：二十日的车票证明他当天乘车离镇。'),
                dict(id='S4', text='现场：被裁去上下文的信，指向修车匠到过住宅。'),
                dict(id='S5', text='丈夫：会客室收款抽屉里约一百五十元现金被拿走。'),
                dict(id='S6', text='现场：工具牌和机油手套说明修车匠二十日傍晚到过住宅。')],
    kinds=[dict(id='time', text='时刻需要校正'), dict(id='date', text='日期需要校正'),
           dict(id='trip', text='行程无法由此证明'), dict(id='scene', text='现场布置存疑')],
    rules=[dict(id='D01', statement='S3', kind='trip', required=['E01', 'E02', 'E03'], supporting=[], core=False,
                boundary='无孔的车票只说明这张票没有检过；它不能证明丈夫一定没有离镇。'),
           dict(id='D02', statement='S1', kind='time', required=['E01', 'E04', 'E05'], supporting=['E06'], core=True,
                boundary='这能说明证言里的钟点需要按独立记录重新核对；当下街钟的快慢本身，不能单独证明当天的钟差。'),
           # Second route to D02 through the hospital records (docs/research/mystery-evidence-design.md):
           # the porter's station-set watch and the office clock set by the tower chime record the same short walk.
           dict(id='D02', statement='S1', kind='time', required=['E01', 'E04', 'Y01', 'Y02'], supporting=['E06', 'Y04'], core=True,
                boundary='门房按车站对过的表、事务室按钟楼校的钟，记下同一段几十步的路，却差了约半个钟头。这只说明二十日下午镇钟已经快了；证言里的钟点需要校正，但不说明此后谁进过门。'),
           dict(id='D03', statement='S2', kind='date', required=['E01', 'E08'], supporting=['E07'], core=True,
                boundary='原簿把问路记在十九日；陈述里的日期需要重新核对。往事片段不能作为提交依据。'),
           dict(id='D04', statement='S4', kind='scene', required=['E01', 'E09'], supporting=[], core=False,
                boundary='这只是布置现场的疑问，不能单独证明谁杀了人。'),
           dict(id='D05', statement='S5', kind='scene', required=['H05', 'H13', 'Y02'], supporting=['Y04', 'H23'], core=False,
                boundary='十九日取出的一百元里，有四十元二十日下午已交到医院；申报的数额没有凭证支持。这只说明申报与家里的账对不上，不能证明钱被谁拿走。'),
           # E garage (2026-10-14): the neighbouring-city work order and the two customers' copies.
           dict(id='D06', statement='S6', kind='trip', required=['E01', 'W01', 'W02'], supporting=[], core=False,
                boundary='两份独立记录把修车匠二十日傍晚放在邻市车间；现场物件不能证明他到过住宅。这本身不能说明是谁把物件带进屋里。')],
    unsupported='警署收下了你的判断，但目前附上的材料还不足以核对这一点。',
    unresolved='尚未解决：需要能独立核对这句陈述的记录。',
    closing='警署收下了两组可以核对的材料：送奶证言的钟点需要校正，问路的日期被放进了二十日。原陈述将被重新审查，案件进入重新调查。\n\n你仍然可以继续在镇上查看。')


def node(i, speaker, text, nxt=None, choices=None):
    n = dict(id=i, speaker=speaker, text=text)
    if nxt:
        n['next'] = nxt
    if choices:
        n['choices'] = [dict(text=t, target=g, topic=p) for t, g, p in choices]
    return n


O, Y = '值班警员', '你'
DIALOGUE = dict(first='greeting', revisit='greeting', nodes=[
    node('greeting', O, '您好，先生。是来查阅材料的吗？', 'topics'),
    node('topics', O, '您想问什么？', choices=[
        ('这里能查些什么？', 'ask-what', 'police-officer.what'),
        ('巡警记录的副本是从哪儿来的？', 'ask-copy', 'police-officer.copy'),
        ('原件能看吗？', 'ask-originals', 'police-officer.originals'),
        ('后面的旧羁押室……', 'ask-holding', 'police-officer.holding'),
        ('我想提交阶段报告。', 'ask-report', 'police-officer.report'),
        ('先这样，谢谢。', 'ask-goodbye', 'police-officer.goodbye')]),
    node('ask-what', Y, '这里能查些什么？', 'what'),
    node('what', O, '公开案卷在左手边的阅览室：案卷摘要、陈述誊录页和残信照片都在桌上，对照桌上放着摘要和巡警记录的副本。分类柜按证人、日期、物证排。\n'
                    '后院两翼还有资料分馆和档案库。您有查阅许可，看完放回原处就行。', 'topics'),
    node('ask-copy', Y, '巡警记录的副本是从哪儿来的？', 'copy'),
    node('copy', O, '那是另一镇巡警原簿的调取副本。原簿在那边，我们这里只留获准抄录的一份，车站资料桌也放着一份。', 'topics'),
    node('ask-originals', Y, '原件能看吗？', 'originals'),
    node('originals', O, '物证原件封存在东翼的保管室，不对外开放。照片副本挂在那边的照片板上，您可以去看。', 'topics'),
    node('ask-holding', Y, '后面的旧羁押室……', 'holding'),
    node('holding', O, '您出来以后那间就空着了。门没锁，想看就推开看看。', 'topics'),
    node('ask-report', Y, '我想提交阶段报告。', 'report'),
    node('report', O, '报告纸在这儿。写清楚您质疑哪句陈述，附上哪些材料。我们只核对材料对不对得上，不替您下结论。'),
    node('ask-goodbye', Y, '先这样，谢谢。', 'goodbye'),
    node('goodbye', O, '好的。有事再来窗口找我。'),
])

# ---- Paper faces ------------------------------------------------------------------------------
# House paper palette (tools/garage/write_garage_texts.py, tools/hospital/evidence.py). A typewriter
# strikes each character on its own, so the ink weight and baseline vary a little from one to the next.
PAPER, BORDER, HEAD, RULE, INK, STAMP = (218, 207, 178), (140, 120, 92), (42, 44, 40), (96, 92, 76), (52, 50, 42), (122, 64, 56)
CARD_RED, CARD_BLUE, FORM_LINE = (170, 78, 66), (150, 166, 178), (180, 168, 140)
CLOSING, OPENING = set('。，、；：）”’！？…—》」』'), set('（“‘《「『')
PAGE_SPACING, NOTICE_SPACING = (1.45, .55, .3), (1.45, .55, .12)   # line, blank line, between paragraphs (x type size)


def face(size):
    return ImageFont.truetype(str(FONT), size)


def tokens(text):
    i = 0
    while i < len(text):
        n = 2 if text[i] in '…—' and text[i + 1:i + 2] == text[i] else 1    # ……and —— never split
        yield text[i:i + n]
        i += n


def wrap(text, font, width, indent=0.0):
    """Measured lines: closing marks hang on the line before, opening marks never end a line."""
    lines, line = [], ''
    for t in tokens(text):
        if line and font.getlength(line + t) > width - (indent if lines else 0) and t[0] not in CLOSING:
            carry = ''
            while line and line[-1] in OPENING:
                carry, line = line[-1] + carry, line[:-1]
            lines.append(line)
            line = carry
        line += t
    lines.append(line)
    if ''.join(lines) != text:
        raise SystemExit('Paper lines lost text: ' + text)
    return lines


def rows_of(body, font, width):
    """(indent, text, opens a paragraph) for every line, None for a blank line; numbered items hang."""
    rows = []
    for para in body.split('\n'):
        if not para:
            rows.append(None)
            continue
        hang = font.getlength(para[:2]) if para[1:2] == '、' and para[0] in '一二三四五六七八九十' else 0.0
        rows += [(hang if k else 0.0, line, k == 0) for k, line in enumerate(wrap(para, font, width, hang))]
    return rows


def extent(rows, size, spacing):
    lead, gap, space = (size * k for k in spacing)
    h, prev = 0.0, None
    for r in rows:
        h += gap if r is None else lead + (space if r[2] and prev is not None else 0)
        prev = r
    return h


def orphans(rows):
    """Paragraphs whose wrapped last line holds only one or two characters."""
    return sum(1 for r, n in zip(rows, rows[1:] + [None])
               if r and not r[2] and (n is None or n[2]) and len(r[1].rstrip(''.join(CLOSING))) <= 2)


def fit(body, width, room, sizes, spacing=PAGE_SPACING, one_line=None):
    """Largest type that fits the room; within 2 px of it, the size leaving the fewest orphans.
    A notice takes one line per item if that keeps the type at least `one_line` px."""
    fits = [(font, rows) for font in map(face, sizes) for rows in [rows_of(body, font, width)]
            if extent(rows, font.size, spacing) <= room]
    if not fits:
        raise SystemExit('Paper text does not fit: ' + body[:20])
    if one_line:
        for font, rows in fits:
            if font.size >= one_line and all(r is None or r[2] for r in rows):
                return font, rows
    near = [c for c in fits if c[0].size >= fits[0][0].size - 2]
    return min(near, key=lambda c: (orphans(c[1]), -c[0].size))


def strike(d, x, y, text, font, rnd):
    for ch in text:
        w = rnd.uniform(.8, 1.0)
        d.text((round(x), y + rnd.choice((-1, 0, 0, 0, 1))), ch, font=font, fill=tuple(round(p + (i - p) * w) for p, i in zip(PAPER, INK)))
        x += font.getlength(ch)


def typeset(d, x, y, rows, font, rnd, spacing=PAGE_SPACING):
    lead, gap, space = (font.size * k for k in spacing)
    prev = None
    for r in rows:
        if r is None:
            y += gap
        else:
            y += space if r[2] and prev is not None else 0
            strike(d, x + r[0], y, r[1], font, rnd)
            y += lead
        prev = r
    return y


def blank(size, seed, inset=30, line=2):
    """Aged paper with faint fibre (as on the hospital sheets) and a ruled border."""
    im = Image.new('RGB', size, PAPER)
    d = ImageDraw.Draw(im)
    rnd = random.Random(seed)
    for _ in range(size[0] * size[1] // 870):
        c = rnd.randrange(196, 224)
        d.point((rnd.randrange(size[0]), rnd.randrange(size[1])), fill=(c, c - 10, c - 36))
    d.rectangle((inset, inset, size[0] - inset - 1, size[1] - inset - 1), outline=BORDER, width=line)
    return im, d


def seal(d, cx, cy, r):
    """A pale ink stamp without lettering, as on the garage copies."""
    d.ellipse((cx - r, cy - r, cx + r, cy + r), outline=STAMP, width=5)
    d.ellipse((cx - r + 18, cy - r + 18, cx + r - 18, cy + r - 18), outline=STAMP, width=2)


def page(doc, seed, stamped=False, short=False, W=1024, H=1320):
    """A typed page: title, rule and body. A short note is cut to its text (a half sheet)."""
    width = W - 2 * 76 - 20
    if short:
        font, rows = fit(doc['body'], width, 10 ** 5, range(44, 27, -1))
        H = max(int(W * .72), int(186 + extent(rows, font.size, PAGE_SPACING) + 110))
    else:
        font, rows = fit(doc['body'], width, H - (230 if stamped else 90) - 186, range(44, 27, -1))
    im, d = blank((W, H), seed)
    d.text((76, 74), doc['title'], font=face(48), fill=HEAD)
    d.line((76, 150, W - 76, 150), fill=RULE, width=2)
    typeset(d, 76, 186, rows, font, random.Random(seed + '/ink'))
    if stamped:
        seal(d, W - 172, H - 168, 84)
    return im


def notice(doc, seed, W=1024, H=768):
    """A posted notice: centred heading, double rule, numbered items with a hanging indent."""
    font, rows = fit(doc['body'], W - 200, H - 286, range(46, 27, -1), NOTICE_SPACING, one_line=36)
    H = max(W // 2, int(196 + extent(rows, font.size, NOTICE_SPACING) + 100))
    im, d = blank((W, H), seed, 28, 4)
    d.rectangle((42, 42, W - 43, H - 43), outline=BORDER, width=1)
    head = face(60)
    w = head.getlength(doc['title'])
    d.text(((W - w) / 2, 74), doc['title'], font=head, fill=HEAD)
    d.line(((W - w) / 2 - 40, 160, (W + w) / 2 + 40, 160), fill=RULE, width=2)
    typeset(d, 90, 196, rows, font, random.Random(seed + '/ink'), NOTICE_SPACING)
    return im


def blank_form(doc, seed, W=1024, H=900):
    """A sheet off the unused interview pad: only the printed heading its description quotes and the
    empty date field. Large print, since it is only ever seen small in the inspection panel."""
    body = doc['body']
    heading, label = body[body.index('“') + 1:body.index('”')], '日期'
    if label + '一栏' not in body:
        raise SystemExit('Interview form description no longer names its date field')
    im, d = blank((W, H), seed)
    head = face(58)
    d.text(((W - head.getlength(heading)) / 2, 66), heading, font=head, fill=HEAD)
    d.line((76, 160, W - 76, 160), fill=RULE, width=3)
    d.line((76, 168, W - 76, 168), fill=RULE, width=1)
    small = face(46)
    d.text((W - 440, 196), label, font=small, fill=HEAD)
    d.line((W - 426 + small.getlength(label), 250, W - 76, 250), fill=RULE, width=2)
    for y in range(320, H - 60, 56):
        d.line((76, y, W - 76, y), fill=FORM_LINE, width=1)
    return im


def counterfoil(doc, seed, W=1024, H=600):
    """The permit's stub: perforated edge, the grant, then the station's signature beside its stamp."""
    *grant, sign = doc['body'].split('\n')
    if not sign.startswith('——'):
        raise SystemExit('Permit stub must end with its signature line')
    im, d = blank((W, H), seed)
    for y in range(40, H - 40, 18):
        d.ellipse((93, y - 3, 99, y + 3), fill=(176, 162, 132))
    d.text((140, 66), doc['title'], font=face(46), fill=HEAD)
    d.line((140, 136, W - 70, 136), fill=RULE, width=2)
    font, rows = fit('\n'.join(grant).strip('\n'), W - 230, H - 320, range(42, 27, -1))
    rnd = random.Random(seed + '/ink')
    typeset(d, 140, 166, rows, font, rnd)
    strike(d, W - 250 - font.getlength(sign), H - 140, sign, font, rnd)
    seal(d, W - 150, H - 128, 64)
    return im


def index_card(doc, seed, W=1000, H=600):
    """A ruled index card: heading over the red rule, typed lines sitting on the blue rules."""
    im, d = blank((W, H), seed, 24)
    d.text((64, 46), doc['title'], font=face(42), fill=HEAD)
    top, pitch = 116, 70
    d.line((40, top, W - 40, top), fill=CARD_RED, width=3)
    rules = list(range(top + pitch, H - 40, pitch))
    for y in rules:
        d.line((40, y, W - 40, y), fill=CARD_BLUE, width=1)
    for size in range(40, 27, -1):
        font = face(size)
        rows = rows_of(doc['body'], font, W - 120)
        if len(rows) <= len(rules):
            break
    else:
        raise SystemExit('Index card overflows: ' + doc['id'])
    rnd = random.Random(seed + '/ink')
    for r, y in zip(rows, rules):
        if r:
            strike(d, 64 + r[0], y - round(size * 1.22), r[1], font, rnd)
    return im


STYLES = {'E01': (page, dict(stamped=True)), 'HusbandStatement': (page, dict(short=True)), 'Catalog': (page, dict(short=True)),
          'HallNotice': (notice, {}), 'PatrolNotice': (notice, {}), 'InterviewPad': (blank_form, {}), 'PermitStub': (counterfoil, {}),
          'IndexWitness': (index_card, {}), 'IndexDate': (index_card, {}), 'IndexEvidence': (index_card, {})}


def missing_glyphs(text):
    """Characters TownSans lacks (they would print as the empty .notdef glyph)."""
    font = face(40)

    def ink(ch):
        im = Image.new('L', (64, 64))
        ImageDraw.Draw(im).text((8, 4), ch, font=font, fill=255)
        return im.tobytes()
    notdef = ink('')
    return sorted({c for c in text if not c.isspace() and ink(c) == notdef})


def write_papers():
    PAPERS.mkdir(parents=True, exist_ok=True)
    made = {}
    for d in DOCS:
        if not d['image'].startswith('Police/Papers/'):
            continue
        stem = Path(d['image']).stem
        if stem in made:
            if (made[stem]['title'], made[stem]['body']) != (d['title'], d['body']):
                raise SystemExit('Documents sharing paper ' + stem + ' differ in text')
            continue
        lacking = missing_glyphs(d['title'] + d['body'])
        if lacking:
            raise SystemExit('TownSans lacks ' + ''.join(lacking) + ' (extend it with tools/clocktower/expand_ui_font.py)')
        style, options = STYLES[stem]
        style(d, stem, **options).save(PAPERS / (stem + '.png'), optimize=True)
        made[stem] = d
    return sorted(made)


for name, data in (('PoliceDocuments.json', dict(documents=DOCS)), ('PoliceReport.json', REPORT), ('PoliceOfficerDialogue.json', DIALOGUE)):
    (R / name).write_bytes((json.dumps(data, ensure_ascii=False, indent=1) + '\n').encode('utf-8'))
papers = write_papers()
print('POLICE_TEXTS_PASS', len(DOCS), 'documents', len(papers), 'papers', ' '.join(papers))
