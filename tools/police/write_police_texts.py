"""Author the police station's text resources (documents, report rules, duty-officer dialogue).

Run from the repository root:  python -X utf8 tools/police/write_police_texts.py
Writes game/Assets/Resources/PoliceDocuments.json, PoliceReport.json and
PoliceOfficerDialogue.json. Texts only typeset facts already in the revised
script (section 1, police view) and docs/mvp-evidence.json; the E08 copy repeats
the station desk's text exactly. No new case facts, dates or culprit statements.
"""
import json
from pathlib import Path

R = Path(__file__).resolve().parents[2] / 'game/Assets/Resources'

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
    doc('e01-copy', '案卷摘要（副本）', '青川警署案卷阅览室 · 公开材料桌 · 已获许可的警方材料副本', E01_BODY),
    doc('e01-compare', '案卷摘要（副本）', '青川警署对照桌 · 与公开材料桌同一份摘要副本', E01_BODY),
    doc('e08-copy', '巡警原簿副本', '青川警署对照桌 · 另一镇巡警原簿的获准调取副本（与车站资料桌为同一抄本）', E08_BODY, 'Rail/E08.png'),
    doc('husband-statement', '丈夫陈述 · 誊录页', '警方誊录的丈夫陈述（副本）· 公开材料桌',
        "我二十号傍晚出的门，大概五点三十五分。去车站坐车，到另一镇南区赴约，对方约在东街。到了南区一问，才知道那里根本没有东街。\n\n"
        "车票我留着，是二十号的。我在那边耽搁了一阵，回到家已经八点四十分左右，正碰上邻居夫妇要出门散步，我请他们陪我进屋……\n\n（页末有陈述人签名。）"),
    doc('letter-photo', '残信照片（翻拍）', '现场照片副本 · 公开材料桌（原件封存于物证室）',
        "现场发现的信件照片。信纸上下两端被整齐地裁去，只剩中间几行字，抬头和落款都不在。\n\n照片背面注：现场物证，原件封存。",
        'Police/Textures/PoliceLetterPhoto.png'),
    doc('hall-notice', '查阅须知', '青川警署接待门厅',
        "一、公开材料可在阅览室查阅，阅后放回原处。\n二、物证原件封存，不对外开放；照片副本见东翼物证室。\n"
        "三、阶段报告可交门口小桌，或交给值班警员。\n四、请勿将材料带出警署。"),
    doc('patrol-notice', '巡逻告示', '青川警署贯通横厅', "夜间巡逻按车站路、中街、北后巷的顺序进行。\n雨天外出请带雨披。"),
    doc('telephone', '墙上的电话', '青川警署贯通横厅', "墙上的摇柄电话，线路接到镇上的总机。"),
    doc('office-phone', '值班室电话', '青川警署值班接待室', "台式电话，听筒挂在叉簧上。旁边压着一张总机号码的小卡片。"),
    doc('catalog', '案卷目录（公开部分）', '青川警署档案誊录室 · 誊录桌',
        "案卷摘要：阅览室公开材料桌、对照桌。\n丈夫陈述誊录页：公开材料桌。\n现场照片：公开材料桌（残信）、物证室照片板（其余物件）。\n"
        "另一镇巡警原簿副本：对照桌；车站资料桌另有一份。\n物证原件：封存于物证室，不对外开放。\n\n（誊录员注：目录只登记存放位置。）"),
    doc('interview-pad', '问询记录纸', '青川警署问询室', "空白问询记录纸。抬头印着“青川警署 问询记录”，日期一栏空着。"),
    doc('duty-book', '巡逻值班簿', '青川警署巡警值班室', "只记班次、巡线与交接时间，没有案件内容。"),
    doc('rain-capes', '油布雨披', '青川警署巡警值班室', "挂着两件油布雨披，衣襟上有干了的泥点。"),
    doc('photo-board', '获准照片副本', '青川警署物证室 · 照片板',
        "一、会客室里的旧外套\n二、修车匠工具牌\n三、机油手套\n四、被裁去上下文的信（照片另见公开材料桌）\n\n原件封存在柜中。"),
    doc('sealed-cage', '封存物证柜', '青川警署物证室', "铁丝网柜门上了锁，封条上写着保管编号。柜里的纸箱贴着标签，原件不对外开放。"),
    doc('holding-bed', '旧羁押室', '青川警署东翼', "铁架床上铺着一张薄褥，叠得很整齐。墙角放着搪瓷盆。高窗很小，只看得见一点天色。\n\n这里空着，门没有上锁。"),
    doc('holding-basin', '搪瓷盆', '青川警署旧羁押室', "一只白色搪瓷盆，盆沿磕掉了一小块漆。"),
    doc('gazette', '镇报合订本', '青川警署资料分馆', "镇报按月装订成册，书脊上写着年份和月份。"),
    doc('archive-boxes', '装订卷宗', '青川警署装订档案库', "卷宗按年份排列，多是与本案无关的旧档。"),
    doc('permit-stub', '查阅许可（存根）', '青川警署负责人办公室', "准许当事人在调查期间查阅本案公开材料副本，阅后放回原处，不得带走原件。\n\n——青川警署"),
    doc('archive-index', '档案索引柜', '青川警署二层档案储存', "按证人、日期、物证三类建立的卡片，和楼下阅览室的分类柜同一套编号。"),
    doc('stove', '小铁炉', '青川警署候问侧席', "铸铁小炉，炉膛里还有一点余温。"),
    doc('index-witness', '分类索引 · 证人', '青川警署阅览室分类柜 · 证人抽屉',
        "丈夫：陈述誊录页、案卷摘要。\n送奶少年：案卷摘要（证言摘要）。\n邻居夫妇：案卷摘要（随同进屋）。\n\n卡片背面：原始笔录存档。"),
    doc('index-date', '分类索引 · 日期', '青川警署阅览室分类柜 · 日期抽屉',
        "一九三一年一月十九日：另一镇巡警原簿副本。\n一九三一年一月二十日：案卷摘要、丈夫陈述、送奶证言摘要、现场照片。"),
    doc('index-evidence', '分类索引 · 物证', '青川警署阅览室分类柜 · 物证抽屉',
        "旧外套：封存，见照片板。\n工具牌：封存，见照片板。\n机油手套：封存，见照片板。\n被裁的信：封存，照片在公开材料桌。\n车票（一月二十日）：当事人自行保管。"),
]

REPORT = dict(
    statements=[dict(id='S1', text='送奶少年：二十日傍晚六点二十五分，他把牛奶交给了妻子。'),
                dict(id='S2', text='丈夫：二十日傍晚，他乘车到另一镇南区问东街。'),
                dict(id='S3', text='丈夫：二十日的车票证明他当天乘车离镇。'),
                dict(id='S4', text='现场：被裁去上下文的信，指向修车匠到过住宅。'),
                dict(id='S5', text='丈夫：会客室收款抽屉里约一百五十元现金被拿走。')],
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
                boundary='十九日取出的一百元里，有四十元二十日下午已交到医院；申报的数额没有凭证支持。这只说明申报与家里的账对不上，不能证明钱被谁拿走。')],
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
    node('originals', O, '物证原件封存在东翼的物证室，不对外开放。照片副本挂在那边的照片板上，您可以去看。', 'topics'),
    node('ask-holding', Y, '后面的旧羁押室……', 'holding'),
    node('holding', O, '那间空着，好久没用了。门没锁，想看就推开看看。', 'topics'),
    node('ask-report', Y, '我想提交阶段报告。', 'report'),
    node('report', O, '报告纸在这儿。写清楚您质疑哪句陈述，附上哪些材料。我们只核对材料对不对得上，不替您下结论。'),
    node('ask-goodbye', Y, '先这样，谢谢。', 'goodbye'),
    node('goodbye', O, '好的。有事再来窗口找我。'),
])

for name, data in (('PoliceDocuments.json', dict(documents=DOCS)), ('PoliceReport.json', REPORT), ('PoliceOfficerDialogue.json', DIALOGUE)):
    (R / name).write_bytes((json.dumps(data, ensure_ascii=False, indent=1) + '\n').encode('utf-8'))
print('POLICE_TEXTS_PASS', len(DOCS), 'documents')
