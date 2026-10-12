"""Game content and art poster pages. Coordinates use distance from the page top."""


def draw_all(api):
    names = ['page_set', 'cv', 'C', 'W', 'H', 'M', 'CW', 'FONT', 'BOLD', 'SERIF',
             'INK', 'DARK', 'PAPER', 'LIGHT', 'MUTED', 'GOLD', 'RULE', 'text',
             'para', 'rect', 'image', 'line', 'pill', 'panel', 'page_header',
             'footer', 'arrow', 'QrCodeWidget', 'Drawing', 'renderPDF']
    globals().update({name: api[name] for name in names})
    for page in [cover, gameplay, narrative, evidence, spaces, art, atmosphere,
                 invitation, spoiler]:
        page()


def cover():
    page_set(1)
    rect(0, 0, W, H, DARK)
    text('第一人称悬疑游戏  /  兴趣协作', M, 40, 14, BOLD, '#e4c68b')
    text(C['title'], M, 103, 92, BOLD, LIGHT)
    text(C['english_title'], M, 217, 18, BOLD, '#d5c2a0')
    line(M, 264, M+257, 264, '#c9a15d', 2)
    image('cover', M, 293, CW, 414, region=[.51, .132, .485, .808])
    text(C['pitch'], M, 753, 31, BOLD, LIGHT, max_width=CW)
    para(C['premise'], M, 812, CW, 19, 31, color='#e1e4d8', bottom=940)
    x = M
    for value in ['1931年', '自由探索', '证词推理', '身份悬疑']:
        x += pill(value, x, 968, '#efe6ce', '#354b40', 13)+11
    text('走过一座小镇，重新理解一段人生。', M, 1024, 24, SERIF,
         '#e6c78a', max_width=CW)
    para('故事与人物 · 调查玩法 · 空间与道具 · 美术与声光\n'
         '完整版最后一页含核心剧透；转发版保留故事悬念。',
         M, 1075, CW, 14, 23, color='#c9d0c2', bottom=1122)
    cv.bookmarkPage('p1')
    cv.addOutlineEntry('错日证词', 'p1', level=0, closed=False)
    footer(1, True)


def gameplay():
    page_header(2, '游玩体验', '观察、核验，再提出自己的判断',
                '沿着好奇心走访小镇。从一次提问或一件物品出发，逐渐建立自己的解释。')
    image('street', M, 205, CW, 286, region=[0, .055, 1, .875])
    rect(M, 453, CW, 38, INK)
    text('阴雾主街  /  生活建筑、花境与可步行道路', M+15, 464, 13, BOLD, LIGHT)
    text('一次调查，如何展开', M, 521, 24, BOLD)
    top = 567
    gap = 14
    bw = (CW-gap*4)/5
    for i, (name, body) in enumerate(C['loop']):
        x = M+i*(bw+gap)
        rect(x, top, bw, 176, LIGHT)
        text(f'{i+1:02d}', x+14, top+13, 20, BOLD, GOLD)
        text(name, x+14, top+49, 18, BOLD, max_width=bw-28)
        para(body, x+14, top+83, bw-28, 14, 21, bottom=top+167)
        if i < 4:
            arrow(x+bw+2, top+31, x+bw+gap-2, top+31)
    para('每一次重新到访，都可能带着新的问题。调查簿保存材料的来源与人物原话，'
         '让一张票、一段陈述和一个钟面，在玩家手中产生联系。',
         M, 771, CW, 17, 26, color=MUTED, bottom=827)
    gap = 18
    bw = (CW-gap*2)/3
    for i, (title, body) in enumerate(C['pillars']):
        panel(title, body, M+i*(bw+gap), 855, bw, 167, size=15)
    para('你会亲手拿起怀表、靠近纸页、抬头读钟、回到柜台追问。'
         '对世界的理解，在这些具体动作之间慢慢变化。',
         M, 1052, CW, 16, 25, bottom=1122)
    footer(2)


def narrative():
    page_header(3, '故事与人物', '每个人，都握着一部分往事',
                '你需要听见他们的记忆，也需要理解他们说话时所处的位置。')
    gap = 17
    bw = (CW-gap*2)/3
    shots = [('milk', [.49, .40, .32, .55]),
             ('watchmaker', [.37, .33, .34, .46]), ('clerkface', None)]
    for i, (name, role, body) in enumerate(C['characters']):
        x = M+i*(bw+gap)
        key, crop = shots[i]
        image(key, x, 207, bw, 227, region=crop)
        text(role, x, 446, 13, BOLD, GOLD, max_width=bw)
        text(name, x, 477, 24, BOLD)
        para(body, x, 518, bw, 16, 25, bottom=662)
    line(M, 680, W-M, 680)
    text('身份、关系与顺序', M, 708, 25, BOLD)
    for i, (title, body) in enumerate(C['narrative_layers']):
        top = 759+i*80
        text(title, M, top, 20, BOLD, GOLD)
        para(body, M+110, top, CW-110, 17, 26, bottom=top+64)
    rect(M, 1030, CW, 78, INK)
    para('怨气不等于杀意，传言也不等于事实。故事保留人物复杂的生活：'
         '妻子有令人痛苦的选择，也有照顾孩子的日常。',
         M+18, 1047, CW-36, 16, 24, color=LIGHT, bottom=1103)
    footer(3)


def evidence():
    page_header(4, '调查与线索', '把原话、纸面和现场放在一起',
                '信息有各自的来源与边界。推理的乐趣，在于发现它们怎样互相支持或产生矛盾。')
    image('clerk', M, 206, CW, 330, region=[0, .19, 1, .78])
    text('柜台前的主动提问  /  从行程、人物与票务规则中寻找可以核对的细节',
         M, 551, 13, BOLD, MUTED, max_width=CW)
    for i, (title, body) in enumerate(C['evidence_types']):
        top = 599+i*71
        text(title, M, top, 20, BOLD, GOLD)
        para(body, M+110, top, CW-110, 17, 26, bottom=top+62)
        if i < 3:
            line(M+110, top+60, W-M, top+60)
    image('ticket', M, 927, 218, 158, region=[.38, .36, .25, .42])
    text('让线索成为可观察的物件', M+242, 929, 22, BOLD, max_width=CW-242)
    para('废票箱里叠放的票纸、票边上的孔洞、簿页的日期与钟面上的指针，'
         '把抽象的陈述变成玩家能够再次检查的细节。',
         M+242, 974, CW-242, 17, 26, bottom=1089)
    text('检票处  /  纸张与验票痕迹', M, 1096, 12, BOLD, MUTED)
    footer(4)


def spaces():
    page_header(5, '场景与环境美术', '同一座小镇，留下不同的空间记忆',
                '建筑有鲜明的轮廓，房间有具体的生活用途；玩家靠走访与停留熟悉它们。')
    entries = [
        ('cover', [.51, .132, .485, .808], '灰石与背光钟盘', '古钟楼',
         '灰石塔身与乳白钟盘构成地标。走进机芯与积灰，让时间拥有可触碰的内部。'),
        ('watchmaker', [0, .10, 1, .80], '旧铜、工具与暖灯', '胡桃木钟表铺',
         '深绿木作与暖灯围住维修台。旧铜、展柜和工具，保留一门手艺与店主的生活。'),
        ('milk', [0, .10, 1, .78], '木板、奶瓶与米白墙面', '牛奶屋',
         '小屋里的柜台、奶瓶与生活陈设，让一次日常送奶和少年拘谨的谈话，成为案件的一部分。'),
        ('station', [0, .08, 1, .82], '拱石、铁玻璃与木柜台', '车站',
         '拱石站房、铁玻璃雨棚与木制柜台延续出发的秩序。票据与行程，让熟悉的日常也带上疑问。')]
    gap = 22
    bw = (CW-gap)/2
    for i, (key, crop, caption, title, body) in enumerate(entries):
        x = M+(i % 2)*(bw+gap)
        top = 207+(i//2)*362
        image(key, x, top, bw, 214, region=crop)
        text(caption, x, top+227, 12, BOLD, MUTED, max_width=bw)
        text(title, x, top+253, 23, BOLD)
        para(body, x, top+291, bw, 16, 24, bottom=top+357)
    image('coach', M, 949, 229, 140, region=[0, .08, 1, .80])
    text('从站台走进车厢', M+255, 950, 22, BOLD)
    para('胡桃木板、织物座椅、铜色五金与连续的车窗，'
         '让旅行空间既适合移动，也适合停下来回想一次行程。',
         M+255, 995, CW-255, 16, 25, bottom=1103)
    footer(5)


def art():
    page_header(6, '角色与道具美术', '细节围绕生活动作，慢慢变得可信',
                '清晰的体块与克制的色彩，配合磨损材质、暖光和可辨认的生活用品。')
    image('hands', M, 205, CW, 314, region=[0, .16, 1, .75])
    text('维修台近景  /  人物的手、怀表、工具与旧木桌面', M, 533, 13, BOLD, MUTED)
    gap = 22
    bw = (CW-gap)/2
    for i, (title, body) in enumerate(C['art_direction']):
        panel(title, body, M+(i % 2)*(bw+gap), 578+(i//2)*163,
              bw, 148, size=15)
    text('色彩关系', M, 934, 22, BOLD)
    colors = [('雾灰', '#8a959c'), ('紫灰', '#747786'), ('苔绿', '#455448'),
              ('奶白', '#d9d1ba'), ('胡桃木', '#78543b'), ('旧铜', '#a78450')]
    gap = 12
    bw = (CW-gap*5)/6
    for i, (label, color) in enumerate(colors):
        x = M+i*(bw+gap)
        rect(x, 975, bw, 31, color)
        text(label, x, 1022, 14, BOLD, MUTED)
    para('美术把关注点留给可以读懂的物件：纸上的字、钟上的针、'
         '修理时的手与柜台后的脸。细节让调查动作自然发生。',
         M, 1070, CW, 16, 24, bottom=1122)
    footer(6)


def atmosphere():
    page_header(7, '声光与氛围', '雾、灯、雨与时间，陪伴调查发生',
                '小镇的压抑感藏在天气、距离与房间的声音里，也藏在重返熟悉地点的体验中。')
    image('rainstreet', M, 205, CW, 287, region=[0, .06, 1, .86])
    text('雨夜主街  /  雾中的灯光、湿地面与近处的植物', M, 507, 13, BOLD, MUTED)
    for i, (title, body) in enumerate(C['atmosphere']):
        panel(title, body, M, 561+i*167, CW, 148, size=17)
    para('纸页翻动、门扇合拢、脚步、机芯与列车声，让玩家从街道进入小屋，'
         '从一段谈话转向一件物品时，感到自己仍在同一个世界里。',
         M, 1080, CW, 15, 23, bottom=1123)
    footer(7)


def invitation():
    page_header(8, '游戏吸引力', '想一起创造的，是这样的悬疑体验',
                '从证词与物件出发，让玩家在一座有生活气息的小镇里，亲手重建一段过去。', True)
    gap = 22
    bw = (CW-gap)/2
    image('police', M, 207, bw, 243, region=[.005, .035, .49, .365])
    image('police', M+bw+gap, 207, bw, 243, region=[.502, .035, .494, .365])
    text('警署环境概念图  /  内院回廊、借景窗与案卷陈设', M, 461, 12, BOLD, '#c7d2c7')
    for i, (title, body) in enumerate(C['appeal']):
        x = M+(i % 2)*(bw+gap)
        top = 500+(i//2)*148
        rect(x, top, bw, 128, '#263e34')
        text(title, x+18, top+17, 21, BOLD, '#d8ba80')
        para(body, x+18, top+57, bw-36, 16, 25, color='#d4ddcf', bottom=top+119)
    text('兴趣协作', M, 977, 24, BOLD, '#d8ba80')
    para(C['invitation'], M, 1020, CW-136, 17, 26, color=LIGHT, bottom=1101)
    qr = QrCodeWidget(C['repo_url'])
    bounds = qr.getBounds()
    size = 95
    drawing = Drawing(size, size, transform=[size/(bounds[2]-bounds[0]), 0, 0,
                      size/(bounds[3]-bounds[1]), 0, 0])
    drawing.add(qr)
    rect(W-M-106, 978, 106, 106, LIGHT)
    renderPDF.draw(drawing, cv, W-M-100, H-984-size)
    text('扫码了解项目', W-M-106, 1095, 11, BOLD, '#c7d2c7')
    text('github.com/xichengchen1111-hub/game_test', M, 1110, 12, BOLD, '#d8ba80')
    cv.linkURL(C['repo_url'], (M, H-1124, W-M-135, H-1107), relative=0, thickness=0)
    footer(8, True)


def spoiler():
    page_header(9, '独立附录 / 核心剧透', '原主的真相，以及理解真相后的选择',
                '本页包含故事核心答案。希望保留悬念时，可使用不附本页的8页转发版。', True)
    rect(M, 209, CW, 182, '#263e34')
    text('核心真相', M+20, 228, 20, BOLD, '#d8ba80')
    para(C['spoiler']['truth'], M+20, 269, CW-40, 21, 33,
         font=SERIF, color=LIGHT, bottom=371)
    para(C['spoiler']['motive'], M, 417, CW, 16, 25, color='#d4ddcf', bottom=523)
    text('两组核心查证', M, 555, 23, BOLD, LIGHT)
    for i, (title, key) in enumerate([('时刻矛盾', 'time'), ('日期矛盾', 'date')]):
        top = 601+i*123
        text(title, M, top, 18, BOLD, '#d8ba80')
        para(C['spoiler'][key], M+111, top, CW-111, 16, 25,
             color='#d4ddcf', bottom=top+104)
    line(M, 846, W-M, 846, '#526358')
    text('理解真相后，怎样使用它', M, 870, 21, BOLD, LIGHT)
    para(C['spoiler']['endings'], M, 908, CW, 16, 25, color='#d4ddcf', bottom=1018)
    para(C['spoiler']['montage'], M, 1023, CW, 14, 22, color='#bfcfbe', bottom=1116)
    footer(9, True)
